using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreMiniApp.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.BookId);

        builder.Property(b => b.Title).HasMaxLength(200);

        builder.HasOne(b => b.Cover)
            .WithOne(c => c.Book)
            .HasForeignKey<Cover>(c => c.BookId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(SeedData.Books);
    }
}
