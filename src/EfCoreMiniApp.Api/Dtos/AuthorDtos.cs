using System.ComponentModel.DataAnnotations;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Domain;

namespace EfCoreMiniApp.Api.Dtos;

public record AuthorDto(int AuthorId, string FirstName, string LastName, int BookCount);

public record AuthorSummaryDto(int AuthorId, string FirstName, string LastName);

public record AuthorNameDto(string FirstName, string LastName);

public record AuthorDetailsDto(
    int AuthorId,
    string FirstName,
    string LastName,
    ContactDetailsDto? Contact,
    List<string> Nicknames,
    int BookCount)
{
    public static AuthorDetailsDto From(Author author, int bookCount) => new(
        author.AuthorId,
        author.FirstName,
        author.LastName,
        author.Contact is null
            ? null
            : new ContactDetailsDto { EmailAddress = author.Contact.EmailAddress, Phone = author.Contact.Phone },
        author.Nicknames,
        bookCount);
}

public record ContactDetailsDto
{
    [EmailAddress, MaxLength(100)]
    public string? EmailAddress { get; init; }

    [Phone, MaxLength(30)]
    public string? Phone { get; init; }
}

public record AuthorInput : IValidatableObject
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    public ContactDetailsDto? Contact { get; init; }

    public List<string>? Nicknames { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ValidationExtensions.ValidateNested(Contact, nameof(Contact)))
        {
            yield return result;
        }

        if (Nicknames?.Any(nickname => string.IsNullOrWhiteSpace(nickname) || nickname.Length > 50) == true)
        {
            yield return new ValidationResult(
                "Nicknames must not be empty and must be at most 50 characters long.",
                [nameof(Nicknames)]);
        }
    }

    public void ApplyTo(Author author)
    {
        author.FirstName = FirstName;
        author.LastName = LastName;
        author.Nicknames = Nicknames?.ToList() ?? [];

        if (Contact is null)
        {
            author.Contact = null;
            return;
        }

        author.Contact ??= new ContactDetails();
        author.Contact.EmailAddress = Contact.EmailAddress;
        author.Contact.Phone = Contact.Phone;
    }
}

public record AuthorGraphDto(int AuthorId, string FullName, List<AuthorGraphBookDto> Books);

public record AuthorGraphBookDto(int BookId, string Title, string? CoverDesignIdeas, List<string> CoverArtists);

public record PagedResult<T>(int Page, int PageSize, int TotalCount, List<T> Items);
