namespace EfCoreMiniApp.Domain;

public record Address
{
    public string? Street { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? PostalCode { get; init; }
}
