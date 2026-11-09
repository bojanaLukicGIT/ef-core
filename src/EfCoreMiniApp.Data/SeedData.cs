using System.Drawing;
using EfCoreMiniApp.Domain;

namespace EfCoreMiniApp.Data;

public static class SeedData
{
    public static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static object[] Publishers =>
    [
        new { PublisherId = 1, Name = "Blue Harbor Books", FoundedYear = (int?)1952, CreatedAt = SeedDate, LastUpdated = SeedDate },
        new { PublisherId = 2, Name = "Northwind Publishing", FoundedYear = (int?)1987, CreatedAt = SeedDate, LastUpdated = SeedDate },
        new { PublisherId = 3, Name = "Lantern House", FoundedYear = (int?)2005, CreatedAt = SeedDate, LastUpdated = SeedDate }
    ];

    public static object[] Authors =>
    [
        Author(1, "Rhoda", "Lerman"),
        Author(2, "Ruth", "Ozeki"),
        Author(3, "Sofia", "Segovia"),
        Author(4, "Ursula K.", "LeGuin", "UKL"),
        Author(5, "Hugh", "Howey"),
        Author(6, "Isabelle", "Allende"),
        Author(7, "Frank", "Herbert")
    ];

    public static object[] Books =>
    [
        Book(1, "In God's Ear", new DateOnly(1989, 3, 1), 12.99m, BookGenre.HistoricalFiction, Color.SkyBlue, authorId: 1, publisherId: 1),
        Book(2, "A Tale For the Time Being", new DateOnly(2013, 12, 31), 16.50m, BookGenre.Memoir, Color.Gold, authorId: 2, publisherId: 2),
        Book(3, "The Left Hand of Darkness", new DateOnly(1969, 3, 1), 9.99m, BookGenre.ScienceFiction, Color.Black, authorId: 4, publisherId: 1),
        Book(4, "Wool", new DateOnly(2012, 1, 1), 14.00m, BookGenre.ScienceFiction, Color.Gray, authorId: 5, publisherId: 2),
        Book(5, "Shift", new DateOnly(2013, 1, 28), 14.00m, BookGenre.ScienceFiction, Color.SlateGray, authorId: 5, publisherId: 2),
        Book(6, "The Murmur of Bees", new DateOnly(2019, 4, 1), 18.75m, BookGenre.HistoricalFiction, Color.Goldenrod, authorId: 3, publisherId: null),
        Book(7, "The House of the Spirits", new DateOnly(1982, 1, 1), 11.25m, BookGenre.HistoricalFiction, Color.DarkRed, authorId: 6, publisherId: 1),
        Book(8, "Call Me Ishtar", new DateOnly(1973, 1, 1), 7.50m, BookGenre.Adventure, null, authorId: 1, publisherId: null),
        Book(9, "A Wizard of Earthsea", new DateOnly(1968, 11, 1), 8.99m, BookGenre.YoungAdult, Color.DarkBlue, authorId: 4, publisherId: 1)
    ];

    public static Artist[] Artists =>
    [
        new() { ArtistId = 1, FirstName = "Pablo", LastName = "Picasso" },
        new() { ArtistId = 2, FirstName = "Dee", LastName = "Bell" },
        new() { ArtistId = 3, FirstName = "Katharine", LastName = "Kuharic" },
        new() { ArtistId = 4, FirstName = "Kir", LastName = "Talmage" }
    ];

    public static Cover[] Covers =>
    [
        new() { CoverId = 1, BookId = 3, DesignIdeas = "How about a left hand in the dark?", DigitalOnly = false },
        new() { CoverId = 2, BookId = 2, DesignIdeas = "Should we put a clock?", DigitalOnly = true },
        new() { CoverId = 3, BookId = 1, DesignIdeas = "A big ear in the clouds?", DigitalOnly = false },
        new() { CoverId = 4, BookId = 5, DesignIdeas = "Rows and rows of freezers", DigitalOnly = false },
        new() { CoverId = 5, BookId = 8, DesignIdeas = "Image of Ishtar?", DigitalOnly = true }
    ];

    public static CoverAssignment[] CoverAssignments =>
    [
        new() { ArtistId = 1, CoverId = 1, DateCreated = SeedDate },
        new() { ArtistId = 2, CoverId = 1, DateCreated = SeedDate },
        new() { ArtistId = 3, CoverId = 2, DateCreated = SeedDate },
        new() { ArtistId = 1, CoverId = 3, DateCreated = SeedDate },
        new() { ArtistId = 2, CoverId = 4, DateCreated = SeedDate },
        new() { ArtistId = 3, CoverId = 5, DateCreated = SeedDate }
    ];

    private static object Author(int authorId, string firstName, string lastName, params string[] nicknames) => new
    {
        AuthorId = authorId,
        FirstName = firstName,
        LastName = lastName,
        Nicknames = nicknames.ToList(),
        CreatedAt = SeedDate,
        LastUpdated = SeedDate
    };

    private static object Book(
        int bookId,
        string title,
        DateOnly publishDate,
        decimal basePrice,
        BookGenre genre,
        Color? primaryColor,
        int authorId,
        int? publisherId) => new
    {
        BookId = bookId,
        Title = title,
        PublishDate = publishDate,
        BasePrice = basePrice,
        Genre = genre,
        PrimaryColor = primaryColor,
        AuthorId = authorId,
        PublisherId = publisherId,
        CreatedAt = SeedDate,
        LastUpdated = SeedDate
    };
}
