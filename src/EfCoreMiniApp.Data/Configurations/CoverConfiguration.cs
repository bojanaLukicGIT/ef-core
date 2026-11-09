using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreMiniApp.Data.Configurations;

public class CoverConfiguration : IEntityTypeConfiguration<Cover>
{
    public void Configure(EntityTypeBuilder<Cover> builder)
    {
        builder.HasKey(c => c.CoverId);

        builder.HasData(SeedData.Covers);
    }

    public static void MapToStoredProcedures(EntityTypeBuilder<Cover> builder)
    {
        builder.InsertUsingStoredProcedure("CoverInsert", procedure => procedure
            .HasParameter(c => c.DesignIdeas)
            .HasParameter(c => c.DigitalOnly)
            .HasParameter(c => c.BookId)
            .HasParameter(c => c.CoverId, parameter => parameter.IsOutput()));

        builder.UpdateUsingStoredProcedure("CoverUpdate", procedure => procedure
            .HasOriginalValueParameter(c => c.CoverId)
            .HasParameter(c => c.DesignIdeas)
            .HasParameter(c => c.DigitalOnly)
            .HasParameter(c => c.BookId)
            .HasRowsAffectedReturnValue());

        builder.DeleteUsingStoredProcedure("DeleteCover", procedure => procedure
            .HasOriginalValueParameter(c => c.CoverId)
            .HasRowsAffectedReturnValue());
    }
}
