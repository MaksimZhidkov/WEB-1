using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Infrastructure.Data;

public sealed class VoteRowConfig : IEntityTypeConfiguration<VoteRow>
{
    public void Configure(EntityTypeBuilder<VoteRow> b)
    {
        b.ToTable("votes");

        b.HasKey(x => new { x.ImageId, x.UserName });

        b.Property(x => x.UserName).HasMaxLength(64).IsRequired();
        b.Property(x => x.IsLike).IsRequired();

        b.HasOne(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.ImageId);
    }
}
