using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfCoreMiniApp.Data.Migrations
{
    public partial class ViewAndStoredProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW dbo.AuthorsByArtist
                AS
                SELECT Artists.FirstName + ' ' + Artists.LastName AS Artist,
                       Authors.FirstName + ' ' + Authors.LastName AS Author
                FROM Artists
                LEFT JOIN ArtistCover ON Artists.ArtistId = ArtistCover.ArtistId
                LEFT JOIN Covers ON ArtistCover.CoverId = Covers.CoverId
                LEFT JOIN Books ON Books.BookId = Covers.BookId
                LEFT JOIN Authors ON Books.AuthorId = Authors.AuthorId");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.AuthorsPublishedinYearRange
                    @yearstart int,
                    @yearend int
                AS
                SELECT DISTINCT Authors.* FROM Authors
                LEFT JOIN Books ON Authors.AuthorId = Books.AuthorId
                WHERE YEAR(Books.PublishDate) >= @yearstart
                  AND YEAR(Books.PublishDate) <= @yearend");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.GetAuthorNames
                AS
                SELECT LastName, FirstName FROM Authors");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.DeleteCover
                    @coverId int
                AS
                BEGIN
                    DELETE FROM Covers WHERE CoverId = @coverId;
                    RETURN @@ROWCOUNT;
                END");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.CoverInsert
                    @designIdeas nvarchar(100),
                    @digitalOnly bit,
                    @bookId int,
                    @coverId int OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    INSERT INTO Covers (DesignIdeas, DigitalOnly, BookId)
                    VALUES (@designIdeas, @digitalOnly, @bookId);
                    SET @coverId = SCOPE_IDENTITY();
                END");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.CoverUpdate
                    @coverId int,
                    @designIdeas nvarchar(100),
                    @digitalOnly bit,
                    @bookId int
                AS
                BEGIN
                    UPDATE Covers
                    SET DesignIdeas = @designIdeas, DigitalOnly = @digitalOnly, BookId = @bookId
                    WHERE CoverId = @coverId;
                    RETURN @@ROWCOUNT;
                END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE dbo.CoverUpdate");
            migrationBuilder.Sql("DROP PROCEDURE dbo.CoverInsert");
            migrationBuilder.Sql("DROP PROCEDURE dbo.DeleteCover");
            migrationBuilder.Sql("DROP PROCEDURE dbo.GetAuthorNames");
            migrationBuilder.Sql("DROP PROCEDURE dbo.AuthorsPublishedinYearRange");
            migrationBuilder.Sql("DROP VIEW dbo.AuthorsByArtist");
        }
    }
}
