using MyApp.Application.Services;
using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Repositories;
using MyApp.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ЛР1 — Вариант 4 (Изображения)",
        Version = "v1",
        Description = "REST API: загрузка изображений, постраничный просмотр, голосование, рейтинг, контакты."
    });
});

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
builder.Services.AddSingleton<IImageRepository, InMemoryImageRepository>();
builder.Services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(uploadsPath, maxBytes: 5 * 1024 * 1024));
builder.Services.AddSingleton<ImageService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
