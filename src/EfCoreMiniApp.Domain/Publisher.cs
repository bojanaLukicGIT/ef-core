using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Domain;

public class Publisher
{
    public int PublisherId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? FoundedYear { get; set; }

    public Address Address { get; set; } = new();

    public virtual List<Book> Books { get; set; } = new();
}
