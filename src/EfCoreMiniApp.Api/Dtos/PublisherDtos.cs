using System.ComponentModel.DataAnnotations;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Domain;

namespace EfCoreMiniApp.Api.Dtos;

public record PublisherDto(int PublisherId, string Name, int? FoundedYear, AddressDto Address, int BookCount);

public record AddressDto
{
    [MaxLength(100)]
    public string? Street { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? State { get; init; }

    [MaxLength(20)]
    public string? PostalCode { get; init; }

    public static AddressDto From(Address address) => new()
    {
        Street = address.Street,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode
    };

    public Address ToAddress() => new()
    {
        Street = Street,
        City = City,
        State = State,
        PostalCode = PostalCode
    };
}

public record PublisherInput : IValidatableObject
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Range(1400, 2100)]
    public int? FoundedYear { get; init; }

    public AddressDto? Address { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ValidationExtensions.ValidateNested(Address, nameof(Address));

    public Publisher ToPublisher() => new()
    {
        Name = Name,
        FoundedYear = FoundedYear,
        Address = Address?.ToAddress() ?? new Address()
    };
}
