using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreMiniApp.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(a => a.AuthorId);

        builder.HasIndex(a => new { a.LastName, a.FirstName });

        builder.OwnsOne(a => a.Contact, contact => contact.ToJson());

        builder.PrimitiveCollection(a => a.Nicknames).IsRequired();

        builder.HasMany(a => a.Books)
            .WithOne(b => b.Author)
            .HasForeignKey(b => b.AuthorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(SeedData.Authors);
    }
}
