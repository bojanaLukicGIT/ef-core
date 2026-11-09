using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Domain;

public class Author
{
    public int AuthorId { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public virtual List<Book> Books { get; set; } = new();

    public virtual ContactDetails? Contact { get; set; }

    public List<string> Nicknames { get; set; } = new();
}
