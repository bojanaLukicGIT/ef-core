using System;
using System.Collections.Generic;
using EfCoreMiniApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EfCoreMiniApp.Data.Migrations
{
    [DbContext(typeof(PubContext))]
    partial class PubContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.33")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("EfCoreMiniApp.Domain.Artist", b =>
                {
                    b.Property<int>("ArtistId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ArtistId"));

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Notes")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("ArtistId");

                    b.ToTable("Artists");

                    b.HasData(
                        new
                        {
                            ArtistId = 1,
                            FirstName = "Pablo",
                            LastName = "Picasso"
                        },
                        new
                        {
                            ArtistId = 2,
                            FirstName = "Dee",
                            LastName = "Bell"
                        },
                        new
                        {
                            ArtistId = 3,
                            FirstName = "Katharine",
                            LastName = "Kuharic"
                        },
                        new
                        {
                            ArtistId = 4,
                            FirstName = "Kir",
                            LastName = "Talmage"
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Author", b =>
                {
                    b.Property<int>("AuthorId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AuthorId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Nicknames")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("AuthorId");

                    b.HasIndex("LastName", "FirstName");

                    b.ToTable("Authors");

                    b.HasData(
                        new
                        {
                            AuthorId = 1,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Rhoda",
                            LastName = "Lerman",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        },
                        new
                        {
                            AuthorId = 2,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Ruth",
                            LastName = "Ozeki",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        },
                        new
                        {
                            AuthorId = 3,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Sofia",
                            LastName = "Segovia",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        },
                        new
                        {
                            AuthorId = 4,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Ursula K.",
                            LastName = "LeGuin",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[\"UKL\"]"
                        },
                        new
                        {
                            AuthorId = 5,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Hugh",
                            LastName = "Howey",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        },
                        new
                        {
                            AuthorId = 6,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Isabelle",
                            LastName = "Allende",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        },
                        new
                        {
                            AuthorId = 7,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FirstName = "Frank",
                            LastName = "Herbert",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Nicknames = "[]"
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.AuthorByArtist", b =>
                {
                    b.Property<string>("Artist")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Author")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.ToTable((string)null);

                    b.ToView("AuthorsByArtist", (string)null);
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Book", b =>
                {
                    b.Property<int>("BookId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("BookId"));

                    b.Property<int>("AuthorId")
                        .HasColumnType("int");

                    b.Property<decimal>("BasePrice")
                        .HasPrecision(10, 2)
                        .HasColumnType("decimal(10,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Genre")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("PrimaryColor")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateOnly>("PublishDate")
                        .HasColumnType("date");

                    b.Property<int?>("PublisherId")
                        .HasColumnType("int");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("BookTitle");

                    b.HasKey("BookId");

                    b.HasIndex("AuthorId");

                    b.HasIndex("PublisherId");

                    b.ToTable("Books");

                    b.HasData(
                        new
                        {
                            BookId = 1,
                            AuthorId = 1,
                            BasePrice = 12.99m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "HistoricalFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "SkyBlue",
                            PublishDate = new DateOnly(1989, 3, 1),
                            PublisherId = 1,
                            Title = "In God's Ear"
                        },
                        new
                        {
                            BookId = 2,
                            AuthorId = 2,
                            BasePrice = 16.50m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "Memoir",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "Gold",
                            PublishDate = new DateOnly(2013, 12, 31),
                            PublisherId = 2,
                            Title = "A Tale For the Time Being"
                        },
                        new
                        {
                            BookId = 3,
                            AuthorId = 4,
                            BasePrice = 9.99m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "ScienceFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "Black",
                            PublishDate = new DateOnly(1969, 3, 1),
                            PublisherId = 1,
                            Title = "The Left Hand of Darkness"
                        },
                        new
                        {
                            BookId = 4,
                            AuthorId = 5,
                            BasePrice = 14.00m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "ScienceFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "Gray",
                            PublishDate = new DateOnly(2012, 1, 1),
                            PublisherId = 2,
                            Title = "Wool"
                        },
                        new
                        {
                            BookId = 5,
                            AuthorId = 5,
                            BasePrice = 14.00m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "ScienceFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "SlateGray",
                            PublishDate = new DateOnly(2013, 1, 28),
                            PublisherId = 2,
                            Title = "Shift"
                        },
                        new
                        {
                            BookId = 6,
                            AuthorId = 3,
                            BasePrice = 18.75m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "HistoricalFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "Goldenrod",
                            PublishDate = new DateOnly(2019, 4, 1),
                            Title = "The Murmur of Bees"
                        },
                        new
                        {
                            BookId = 7,
                            AuthorId = 6,
                            BasePrice = 11.25m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "HistoricalFiction",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "DarkRed",
                            PublishDate = new DateOnly(1982, 1, 1),
                            PublisherId = 1,
                            Title = "The House of the Spirits"
                        },
                        new
                        {
                            BookId = 8,
                            AuthorId = 1,
                            BasePrice = 7.50m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "Adventure",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PublishDate = new DateOnly(1973, 1, 1),
                            Title = "Call Me Ishtar"
                        },
                        new
                        {
                            BookId = 9,
                            AuthorId = 4,
                            BasePrice = 8.99m,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Genre = "YoungAdult",
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            PrimaryColor = "DarkBlue",
                            PublishDate = new DateOnly(1968, 11, 1),
                            PublisherId = 1,
                            Title = "A Wizard of Earthsea"
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Cover", b =>
                {
                    b.Property<int>("CoverId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CoverId"));

                    b.Property<int>("BookId")
                        .HasColumnType("int");

                    b.Property<string>("DesignIdeas")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("DigitalOnly")
                        .HasColumnType("bit");

                    b.HasKey("CoverId");

                    b.HasIndex("BookId")
                        .IsUnique();

                    b.ToTable("Covers");

                    b.HasData(
                        new
                        {
                            CoverId = 1,
                            BookId = 3,
                            DesignIdeas = "How about a left hand in the dark?",
                            DigitalOnly = false
                        },
                        new
                        {
                            CoverId = 2,
                            BookId = 2,
                            DesignIdeas = "Should we put a clock?",
                            DigitalOnly = true
                        },
                        new
                        {
                            CoverId = 3,
                            BookId = 1,
                            DesignIdeas = "A big ear in the clouds?",
                            DigitalOnly = false
                        },
                        new
                        {
                            CoverId = 4,
                            BookId = 5,
                            DesignIdeas = "Rows and rows of freezers",
                            DigitalOnly = false
                        },
                        new
                        {
                            CoverId = 5,
                            BookId = 8,
                            DesignIdeas = "Image of Ishtar?",
                            DigitalOnly = true
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.CoverAssignment", b =>
                {
                    b.Property<int>("ArtistId")
                        .HasColumnType("int");

                    b.Property<int>("CoverId")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateCreated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("ArtistId", "CoverId");

                    b.HasIndex("CoverId");

                    b.ToTable("ArtistCover", (string)null);

                    b.HasData(
                        new
                        {
                            ArtistId = 1,
                            CoverId = 1,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            ArtistId = 2,
                            CoverId = 1,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            ArtistId = 3,
                            CoverId = 2,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            ArtistId = 1,
                            CoverId = 3,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            ArtistId = 2,
                            CoverId = 4,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            ArtistId = 3,
                            CoverId = 5,
                            DateCreated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Publisher", b =>
                {
                    b.Property<int>("PublisherId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("PublisherId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int?>("FoundedYear")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.ComplexProperty<Dictionary<string, object>>("Address", "EfCoreMiniApp.Domain.Publisher.Address#Address", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("City")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.Property<string>("PostalCode")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.Property<string>("State")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.Property<string>("Street")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");
                        });

                    b.HasKey("PublisherId");

                    b.ToTable("Publishers");

                    b.HasData(
                        new
                        {
                            PublisherId = 1,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FoundedYear = 1952,
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Blue Harbor Books"
                        },
                        new
                        {
                            PublisherId = 2,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FoundedYear = 1987,
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Northwind Publishing"
                        },
                        new
                        {
                            PublisherId = 3,
                            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            FoundedYear = 2005,
                            LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Lantern House"
                        });
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Author", b =>
                {
                    b.OwnsOne("EfCoreMiniApp.Domain.ContactDetails", "Contact", b1 =>
                        {
                            b1.Property<int>("AuthorId")
                                .HasColumnType("int");

                            b1.Property<string>("EmailAddress")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.Property<string>("Phone")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.HasKey("AuthorId");

                            b1.ToTable("Authors");

                            b1.ToJson("Contact");

                            b1.WithOwner()
                                .HasForeignKey("AuthorId");
                        });

                    b.Navigation("Contact");
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Book", b =>
                {
                    b.HasOne("EfCoreMiniApp.Domain.Author", "Author")
                        .WithMany("Books")
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EfCoreMiniApp.Domain.Publisher", "Publisher")
                        .WithMany("Books")
                        .HasForeignKey("PublisherId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Author");

                    b.Navigation("Publisher");
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Cover", b =>
                {
                    b.HasOne("EfCoreMiniApp.Domain.Book", "Book")
                        .WithOne("Cover")
                        .HasForeignKey("EfCoreMiniApp.Domain.Cover", "BookId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Book");
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.CoverAssignment", b =>
                {
                    b.HasOne("EfCoreMiniApp.Domain.Artist", null)
                        .WithMany()
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EfCoreMiniApp.Domain.Cover", null)
                        .WithMany()
                        .HasForeignKey("CoverId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Author", b =>
                {
                    b.Navigation("Books");
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Book", b =>
                {
                    b.Navigation("Cover");
                });

            modelBuilder.Entity("EfCoreMiniApp.Domain.Publisher", b =>
                {
                    b.Navigation("Books");
                });
#pragma warning restore 612, 618
        }
    }
}
