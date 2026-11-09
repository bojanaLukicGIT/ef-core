using System.ComponentModel.DataAnnotations;
using System.Drawing;
using EfCoreMiniApp.Data.Configurations;
using EfCoreMiniApp.Data.Interceptors;
using EfCoreMiniApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Data;

public class PubContext : DbContext
{
    public PubContext(DbContextOptions<PubContext> options) : base(options)
    {
        SavingChanges += OnSavingChanges;
        SavedChanges += OnSavedChanges;
    }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Cover> Covers => Set<Cover>();
    public DbSet<AuthorByArtist> AuthorsByArtist => Set<AuthorByArtist>();

    public SaveChangesSummary? LastSaveSummary { get; private set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(AuditInterceptor.Instance, QueryHintCommandInterceptor.Instance);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(100);
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
        configurationBuilder.Properties<BookGenre>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<Color>().HaveConversion<ColorToStringConverter>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var auditedType in new[] { typeof(Author), typeof(Book), typeof(Publisher) })
        {
            modelBuilder.Entity(auditedType).Property<DateTime>(AuditInterceptor.CreatedAt);
            modelBuilder.Entity(auditedType).Property<DateTime>(AuditInterceptor.LastUpdated);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PubContext).Assembly);

        if (Database.IsSqlServer())
        {
            CoverConfiguration.MapToStoredProcedures(modelBuilder.Entity<Cover>());
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTrackedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateTrackedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTrackedEntities()
    {
        var entities = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified && !e.Metadata.IsOwned())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            Validator.ValidateObject(entity, new ValidationContext(entity), validateAllProperties: true);
        }
    }

    private void OnSavingChanges(object? sender, SavingChangesEventArgs e)
    {
        var entries = ChangeTracker.Entries().ToList();
        LastSaveSummary = new SaveChangesSummary(
            entries.Count(entry => entry.State == EntityState.Added),
            entries.Count(entry => entry.State == EntityState.Modified),
            entries.Count(entry => entry.State == EntityState.Deleted),
            EntitiesSaved: 0);
    }

    private void OnSavedChanges(object? sender, SavedChangesEventArgs e)
    {
        LastSaveSummary = (LastSaveSummary ?? new SaveChangesSummary(0, 0, 0, 0)) with { EntitiesSaved = e.EntitiesSavedCount };
    }
}
