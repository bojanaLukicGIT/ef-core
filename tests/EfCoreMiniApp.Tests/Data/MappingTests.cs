using System.Drawing;
using EfCoreMiniApp.Data.Interceptors;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfCoreMiniApp.Tests.Data;

public class MappingTests
{
    [Fact]
    public void ColumnAttribute_MapsTitleToBookTitleColumn()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var property = context.Model.FindEntityType(typeof(Book))!.FindProperty(nameof(Book.Title))!;

        Assert.Equal("BookTitle", property.GetColumnName());
    }

    [Theory]
    [InlineData(typeof(Author), nameof(Author.FirstName), 100)]
    [InlineData(typeof(Cover), nameof(Cover.DesignIdeas), 100)]
    [InlineData(typeof(Book), nameof(Book.Title), 200)]
    [InlineData(typeof(Artist), nameof(Artist.Notes), 500)]
    [InlineData(typeof(Book), nameof(Book.Genre), 20)]
    [InlineData(typeof(Book), nameof(Book.PrimaryColor), 30)]
    public void MaxLength_ComesFromPreConventionsAndFluentApi(Type entityType, string propertyName, int expectedLength)
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        Assert.Equal(expectedLength, property.GetMaxLength());
    }

    [Theory]
    [InlineData(typeof(Author), nameof(Author.FirstName), false)]
    [InlineData(typeof(Artist), nameof(Artist.Notes), true)]
    [InlineData(typeof(Book), nameof(Book.PublisherId), true)]
    [InlineData(typeof(Book), nameof(Book.AuthorId), false)]
    [InlineData(typeof(Book), nameof(Book.PrimaryColor), true)]
    [InlineData(typeof(Publisher), nameof(Publisher.FoundedYear), true)]
    public void NullableReferenceAndValueTypes_DriveColumnNullability(Type entityType, string propertyName, bool expectedNullable)
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        Assert.Equal(expectedNullable, property.IsNullable);
    }

    [Fact]
    public void PreConventionConfiguration_SetsDecimalPrecision()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var basePrice = context.Model.FindEntityType(typeof(Book))!.FindProperty(nameof(Book.BasePrice))!;

        Assert.Equal(10, basePrice.GetPrecision());
        Assert.Equal(2, basePrice.GetScale());
    }

    [Fact]
    public void EnumConversion_StoresGenreAsString()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var storedGenre = context.Database.SqlQuery<string>($"SELECT Genre AS Value FROM Books WHERE BookId = 3").Single();

        Assert.Equal("ScienceFiction", storedGenre);
        Assert.Equal(BookGenre.ScienceFiction, context.Books.Single(b => b.BookId == 3).Genre);
    }

    [Fact]
    public void CustomValueConverter_StoresColorByName()
    {
        using var database = new SqliteDatabase();
        int bookId;
        using (var context = database.CreateContext())
        {
            var book = new Book { Title = "Teal Book", PublishDate = new DateOnly(2024, 1, 1), AuthorId = 7, PrimaryColor = Color.Teal };
            context.Books.Add(book);
            context.SaveChanges();
            bookId = book.BookId;
        }

        using (var context = database.CreateContext())
        {
            var stored = context.Database.SqlQuery<string>($"SELECT PrimaryColor AS Value FROM Books WHERE BookId = {bookId}").Single();
            var book = context.Books.Single(b => b.BookId == bookId);

            Assert.Equal("Teal", stored);
            Assert.Equal(Color.Teal.Name, book.PrimaryColor!.Value.Name);
            Assert.Null(context.Books.Single(b => b.BookId == 8).PrimaryColor);
        }
    }

    [Fact]
    public void OwnedTypeMappedToJson_IsStoredInOneColumnAndQueryable()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Authors.Add(new Author
            {
                FirstName = "William",
                LastName = "Shakespeare",
                Contact = new ContactDetails { EmailAddress = "will@upstart.crow", Phone = "555-555-1234" }
            });
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var json = context.Database.SqlQuery<string>($"SELECT Contact AS Value FROM Authors WHERE LastName = 'Shakespeare'").Single();
            var author = context.Authors.Single(a => a.Contact!.EmailAddress == "will@upstart.crow");

            Assert.Contains("\"EmailAddress\":\"will@upstart.crow\"", json);
            Assert.Equal("555-555-1234", author.Contact!.Phone);
        }
    }

    [Fact]
    public void PrimitiveCollection_IsStoredAsJsonAndQueryable()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Authors.Add(new Author
            {
                FirstName = "William",
                LastName = "Shakespeare",
                Nicknames = ["The Bard", "The Bard of Avon", "Swan of Avon"]
            });
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var json = context.Database.SqlQuery<string>($"SELECT Nicknames AS Value FROM Authors WHERE LastName = 'Shakespeare'").Single();
            var bards = context.Authors.Where(a => a.Nicknames.Contains("The Bard")).Select(a => a.LastName).ToList();

            Assert.Equal("[\"The Bard\",\"The Bard of Avon\",\"Swan of Avon\"]", json);
            Assert.Equal(["Shakespeare"], bards);
            Assert.Equal(["UKL"], context.Authors.Single(a => a.AuthorId == 4).Nicknames);
        }
    }

    [Fact]
    public void ComplexType_MapsValueObjectToColumnsOfTheOwnerTable()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Publishers.Add(new Publisher
            {
                Name = "Danube Press",
                Address = new Address { Street = "Knez Mihailova 1", City = "Belgrade", PostalCode = "11000" }
            });
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var complexProperty = context.Model.FindEntityType(typeof(Publisher))!.FindComplexProperty(nameof(Publisher.Address))!;
            var cityColumn = complexProperty.ComplexType
                .FindProperty(nameof(Address.City))!
                .GetColumnName(StoreObjectIdentifier.Table("Publishers"));
            var publisher = context.Publishers.Single(p => p.Address.City == "Belgrade");

            Assert.Equal("Address_City", cityColumn);
            Assert.Equal(new Address { Street = "Knez Mihailova 1", City = "Belgrade", PostalCode = "11000" }, publisher.Address);
        }
    }

    [Fact]
    public void ReplacingValueObject_IsDetectedAndSaved()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var publisher = context.Publishers.Find(3)!;
            publisher.Address = publisher.Address with { City = "Novi Sad", PostalCode = "21000" };
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal("Novi Sad", context.Publishers.Find(3)!.Address.City);
        }
    }

    [Fact]
    public void ShadowProperties_ExistInTheModelButNotOnTheClass()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var lastUpdated = context.Model.FindEntityType(typeof(Author))!.FindProperty(AuditInterceptor.LastUpdated)!;

        Assert.True(lastUpdated.IsShadowProperty());
        Assert.Null(typeof(Author).GetProperty(AuditInterceptor.LastUpdated));
    }

    [Fact]
    public void KeylessEntity_IsMappedToAView()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(AuthorByArtist))!;

        Assert.Null(entityType.FindPrimaryKey());
        Assert.Equal("AuthorsByArtist", entityType.GetViewName());
        Assert.Null(entityType.GetTableName());
    }
}
