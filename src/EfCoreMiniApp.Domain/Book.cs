using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace EfCoreMiniApp.Domain;

public class Book
{
    public int BookId { get; set; }

    [Required, MaxLength(200)]
    [Column("BookTitle")]
    public string Title { get; set; } = string.Empty;

    public DateOnly PublishDate { get; set; }

    public decimal BasePrice { get; set; }

    public BookGenre Genre { get; set; }

    public Color? PrimaryColor { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public int AuthorId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public int? PublisherId { get; set; }

    public virtual Publisher? Publisher { get; set; }

    public virtual Cover? Cover { get; set; }
}
