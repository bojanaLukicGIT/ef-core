using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreMiniApp.Data.Configurations;

public class AuthorByArtistConfiguration : IEntityTypeConfiguration<AuthorByArtist>
{
    public void Configure(EntityTypeBuilder<AuthorByArtist> builder)
    {
        builder.HasNoKey().ToView("AuthorsByArtist");
    }
}
