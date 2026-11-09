using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Domain;

public class Artist
{
    public int ArtistId { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public virtual List<Cover> Covers { get; set; } = new();
}
