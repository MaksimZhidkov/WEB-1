using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MyApp.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        // 1) Ждём готовности БД + применяем миграции
        const int maxAttempts = 30;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                db.Database.Migrate();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                Thread.Sleep(delay);
            }
        }

        // 2) Проверяем наличие таблиц через обычный запрос.
        // Если миграции не применились/их нет — db.Images.Any() упадёт 42P01, ловим и даём понятную диагностику.
        bool hasAny;
        try
        {
            hasAny = db.Images.Any();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Таблицы нет -> пробуем миграцию ещё раз
            db.Database.Migrate();

            try
            {
                hasAny = db.Images.Any();
            }
            catch (PostgresException ex2) when (ex2.SqlState == "42P01")
            {
                throw new InvalidOperationException(
                    "Таблица 'images' отсутствует. Миграции не применились или не были собраны в контейнер. " +
                    "Проверь, что миграция создана в проекте MyApp.Infrastructure (Data/Migrations) и пересобери контейнер.",
                    ex2);
            }
        }

        if (hasAny) return;

        var img1 = new ImageRow
        {
            Id = Guid.NewGuid(),
            Title = "Тестовое изображение 1",
            RelativePath = "/uploads/seed1.png",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30)
        };

        var img2 = new ImageRow
        {
            Id = Guid.NewGuid(),
            Title = "Тестовое изображение 2",
            RelativePath = "/uploads/seed2.png",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        db.Images.AddRange(img1, img2);

        db.Votes.AddRange(
            new VoteRow { ImageId = img1.Id, UserName = "user1", IsLike = true },
            new VoteRow { ImageId = img1.Id, UserName = "user2", IsLike = false },
            new VoteRow { ImageId = img2.Id, UserName = "user1", IsLike = true }
        );

        db.SaveChanges();
    }

    private static bool IsTransient(Exception ex)
    {
        // типичные “временные” ошибки старта БД/сети
        if (ex is NpgsqlException) return true;
        if (ex is DbUpdateException) return true;
        if (ex is TimeoutException) return true;
        if (ex is PostgresException pex && pex.SqlState is "57P03" or "57P02" or "08001" or "08006") return true;
        return false;
    }
}
