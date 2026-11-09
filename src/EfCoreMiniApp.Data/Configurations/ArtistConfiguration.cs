using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreMiniApp.Data.Configurations;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(a => a.ArtistId);

        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasMany(a => a.Covers)
            .WithMany(c => c.Artists)
            .UsingEntity<CoverAssignment>(
                right => right.HasOne<Cover>().WithMany().HasForeignKey(ca => ca.CoverId).OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Artist>().WithMany().HasForeignKey(ca => ca.ArtistId).OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ArtistCover");
                    join.HasKey(ca => new { ca.ArtistId, ca.CoverId });
                    join.Property(ca => ca.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
                    join.HasData(SeedData.CoverAssignments);
                });

        builder.HasData(SeedData.Artists);
    }
}
