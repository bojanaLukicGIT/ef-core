using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Api.Dtos;

public record ArtistDto(int ArtistId, string FirstName, string LastName, string? Notes, int CoverCount);

public record ArtistDetailsDto(int ArtistId, string FirstName, string LastName, string? Notes, List<ArtistCoverDto> Covers);

public record ArtistCoverDto(int CoverId, string DesignIdeas, int BookId, string BookTitle);

public record ArtistInput
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; init; }
}

public record CoverDto(int CoverId, string DesignIdeas, bool DigitalOnly, int BookId);

public record CoverDetailsDto(
    int CoverId,
    string DesignIdeas,
    bool DigitalOnly,
    int BookId,
    string BookTitle,
    List<CoverArtistDto> Artists);

public record CoverArtistDto(int ArtistId, string FullName, DateTime AssignedOn);

public record CoverInput
{
    [Required, MaxLength(100)]
    public string DesignIdeas { get; init; } = string.Empty;

    public bool DigitalOnly { get; init; }
}

public record ReassignArtistInput
{
    [Range(1, int.MaxValue)]
    public int NewArtistId { get; init; }
}

public record AuthorByArtistDto(string Artist, string? Author);
