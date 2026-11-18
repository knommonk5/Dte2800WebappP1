
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class DatabaseModelTests
{
    private static (SqliteConnection Connection,
        BruktborsenDbContext Context) CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BruktborsenDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BruktborsenDbContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    private static async Task<(ApplicationUser User,
        Category Category, Listing Listing)> SeedListingAsync(
        BruktborsenDbContext context)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "seller",
            FullName = "Test Seller"
        };

        var category = new Category { Name = "Elektronikk" };

        var listing = new Listing
        {
            Title = "Brukt laptop",
            Description = "Laptop i god stand",
            Price = 5000m,
            OriginalPrice = 6000m,
            Condition = Condition.Good,
            Seller = user,
            Category = category
        };

        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        return (user, category, listing);
    }

    [Fact]
    public async Task Listing_CanBeSaved_WithSellerAndCategory()
    {
        var (connection, context) = CreateDatabase();
        using (connection)
        using (context)
        {
            var (user, category, listing) =
                await SeedListingAsync(context);

            context.ChangeTracker.Clear();

            var saved = await context.Listings
                .Include(x => x.Seller)
                .Include(x => x.Category)
                .SingleAsync(x => x.Id == listing.Id);

            Assert.Equal(user.Id, saved.SellerId);
            Assert.Equal("Test Seller", saved.Seller.FullName);
            Assert.Equal(category.Id, saved.CategoryId);
            Assert.Equal("Elektronikk", saved.Category.Name);
            Assert.Equal(ListingStatus.Active, saved.Status);
        }
    }

    [Fact]
    public async Task Listing_CanHaveOffersFavoritesAndPriceChanges()
    {
        var (connection, context) = CreateDatabase();
        using (connection)
        using (context)
        {
            var (_, _, listing) = await SeedListingAsync(context);

            var buyer = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "buyer",
                FullName = "Test Buyer"
            };

            context.Offers.Add(new Offer
            {
                Listing = listing,
                Buyer = buyer,
                Amount = 4500m
            });

            context.Favorites.Add(new Favorite
            {
                Listing = listing,
                User = buyer
            });

            context.PriceChanges.Add(new PriceChange
            {
                Listing = listing,
                OldPrice = 6000m,
                NewPrice = 5000m
            });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var saved = await context.Listings
                .Include(x => x.Offers)
                .Include(x => x.Favorites)
                .Include(x => x.PriceChanges)
                .SingleAsync(x => x.Id == listing.Id);

            Assert.Single(saved.Offers);
            Assert.Single(saved.Favorites);
            Assert.Single(saved.PriceChanges);
            Assert.Equal(4500m, saved.Offers.Single().Amount);
        }
    }

    [Fact]
    public async Task Favorite_DuplicateUserAndListing_IsRejected()
    {
        var (connection, context) = CreateDatabase();
        using (connection)
        using (context)
        {
            var (user, _, listing) =
                await SeedListingAsync(context);

            context.Favorites.Add(new Favorite
            {
                UserId = user.Id,
                ListingId = listing.Id
            });

            await context.SaveChangesAsync();

            context.Favorites.Add(new Favorite
            {
                UserId = user.Id,
                ListingId = listing.Id
            });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Listing_WithInvalidCategory_IsRejected()
    {
        var (connection, context) = CreateDatabase();
        using (connection)
        using (context)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "seller",
                FullName = "Test Seller"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.Listings.Add(new Listing
            {
                Title = "Ugyldig annonse",
                Description = "Kategori finnes ikke",
                Price = 100m,
                OriginalPrice = 100m,
                Condition = Condition.Used,
                SellerId = user.Id,
                CategoryId = 999999
            });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
    }
}
