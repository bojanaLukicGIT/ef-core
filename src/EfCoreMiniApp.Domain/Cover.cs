using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Domain;

public class Cover
{
    public int CoverId { get; set; }

    [Required]
    public string DesignIdeas { get; set; } = string.Empty;

    public bool DigitalOnly { get; set; }

    public int BookId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual List<Artist> Artists { get; set; } = new();
}
