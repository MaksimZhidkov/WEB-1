using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Infrastructure.Data;

public sealed class ImageRowConfig : IEntityTypeConfiguration<ImageRow>
{
    public void Configure(EntityTypeBuilder<ImageRow> b)
    {
        b.ToTable("images");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.RelativePath).HasMaxLength(500).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();

        b.HasIndex(x => x.CreatedAtUtc);
    }
}
