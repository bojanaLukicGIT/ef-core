using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq.Expressions;
using EfCoreMiniApp.Domain;

namespace EfCoreMiniApp.Api.Dtos;

public record BookDto(
    int BookId,
    string Title,
    DateOnly PublishDate,
    decimal BasePrice,
    BookGenre Genre,
    string? PrimaryColor,
    int AuthorId,
    string AuthorName,
    int? PublisherId,
    string? PublisherName)
{
    public static readonly Expression<Func<Book, BookDto>> Projection = book => new BookDto(
        book.BookId,
        book.Title,
        book.PublishDate,
        book.BasePrice,
        book.Genre,
        book.PrimaryColor.HasValue ? book.PrimaryColor.Value.Name : null,
        book.AuthorId,
        book.Author.FirstName + " " + book.Author.LastName,
        book.PublisherId,
        book.Publisher != null ? book.Publisher.Name : null);

    public static BookDto From(Book book) => new(
        book.BookId,
        book.Title,
        book.PublishDate,
        book.BasePrice,
        book.Genre,
        book.PrimaryColor?.Name,
        book.AuthorId,
        $"{book.Author.FirstName} {book.Author.LastName}",
        book.PublisherId,
        book.Publisher?.Name);
}

public record BookDetailsDto(
    int BookId,
    string Title,
    DateOnly PublishDate,
    decimal BasePrice,
    BookGenre Genre,
    string? PrimaryColor,
    int AuthorId,
    string AuthorName,
    int? PublisherId,
    string? PublisherName,
    CoverDto? Cover,
    byte[]? RowVersion)
{
    public static readonly Expression<Func<Book, BookDetailsDto>> Projection = book => new BookDetailsDto(
        book.BookId,
        book.Title,
        book.PublishDate,
        book.BasePrice,
        book.Genre,
        book.PrimaryColor.HasValue ? book.PrimaryColor.Value.Name : null,
        book.AuthorId,
        book.Author.FirstName + " " + book.Author.LastName,
        book.PublisherId,
        book.Publisher != null ? book.Publisher.Name : null,
        book.Cover == null
            ? null
            : new CoverDto(book.Cover.CoverId, book.Cover.DesignIdeas, book.Cover.DigitalOnly, book.Cover.BookId),
        book.RowVersion);
}

public record BookInput : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    public DateOnly PublishDate { get; init; }

    [Range(typeof(decimal), "0", "10000")]
    public decimal BasePrice { get; init; }

    [EnumDataType(typeof(BookGenre))]
    public BookGenre Genre { get; init; }

    [MaxLength(30)]
    public string? PrimaryColor { get; init; }

    [Range(1, int.MaxValue)]
    public int AuthorId { get; init; }

    [Range(1, int.MaxValue)]
    public int? PublisherId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublishDate == default)
        {
            yield return new ValidationResult("PublishDate is required.", [nameof(PublishDate)]);
        }

        if (PrimaryColor is not null && !Color.FromName(PrimaryColor).IsKnownColor)
        {
            yield return new ValidationResult($"'{PrimaryColor}' is not a known color name.", [nameof(PrimaryColor)]);
        }
    }

    public void ApplyTo(Book book)
    {
        book.Title = Title;
        book.PublishDate = PublishDate;
        book.BasePrice = BasePrice;
        book.Genre = Genre;
        book.PrimaryColor = PrimaryColor is null ? null : Color.FromName(PrimaryColor);
        book.AuthorId = AuthorId;
        book.PublisherId = PublisherId;
    }
}

public record BookUpdateInput : BookInput
{
    public byte[]? RowVersion { get; init; }
}

public record GenreStatisticsDto(BookGenre Genre, int BookCount);

public record BookStatisticsDto(int TotalBooks, int BooksWithCover, int BooksWithoutPublisher, List<GenreStatisticsDto> ByGenre);

public record BookCancellationDto(int BookId, int ArtistsNotified);
