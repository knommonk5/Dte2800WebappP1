using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data.Seed;

public static class DataSeeder
{
    private const string SeedPassword = "Brukt2026!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        // Brukere
        await EnsureUserAsync(
            userManager, "admin@brukt.no", "Administrator", "Admin");

        var ola = await EnsureUserAsync(
            userManager, "ola@brukt.no", "Ola Nordmann", "User");

        var kari = await EnsureUserAsync(
            userManager, "kari@brukt.no", "Kari Nordmann", "User");

        var per = await EnsureUserAsync(
            userManager, "per@brukt.no", "Per Hansen", "User");

        ApplicationUser[] users = [ola, kari, per];

        // Kategorier
        var electronics = await EnsureCategoryAsync(
            context, "Elektronikk");

        var bicycles = await EnsureCategoryAsync(
            context, "Sykkel");

        var furniture = await EnsureCategoryAsync(
            context, "Møbler");

        var outdoor = await EnsureCategoryAsync(
            context, "Friluft");

        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // Unngå duplisering ved omstart.
        if (!await context.Listings.AnyAsync(
                x => x.Title == "Brukt bærbar PC - 01"
                     && x.SellerId == ola.Id))
        {
            var listings = new List<Listing>();

            AddListings(
                listings, electronics, users,
                "Brukt bærbar PC", 12, 8, 5000, now);

            AddListings(
                listings, bicycles, users,
                "Brukt terrengsykkel", 10, 6, 4000, now);

            AddListings(
                listings, furniture, users,
                "Brukt spisebord", 10, 5, 3000, now);

            AddListings(
                listings, outdoor, users,
                "Brukt tomannstelt", 8, 1, 2500, now);

            context.Listings.AddRange(listings);
            await context.SaveChangesAsync();

            // Bud på solgte annonser
            foreach (var listing in listings.Where(
                         x => x.Status == ListingStatus.Sold))
            {
                var buyers = users
                    .Where(x => x.Id != listing.SellerId)
                    .ToArray();

                context.Offers.AddRange(
                    new Offer
                    {
                        ListingId = listing.Id,
                        BuyerId = buyers[0].Id,
                        Amount = listing.SoldPrice!.Value,
                        Status = OfferStatus.Accepted,
                        CreatedDate = listing.SoldDate!.Value
                            .AddHours(-2)
                    },
                    new Offer
                    {
                        ListingId = listing.Id,
                        BuyerId = buyers[1].Id,
                        Amount = listing.SoldPrice.Value * 0.85m,
                        Status = OfferStatus.Rejected,
                        CreatedDate = listing.SoldDate.Value
                            .AddHours(-4)
                    });
            }

            // Bud og favoritter på aktive annonser
            var activeListings = listings
                .Where(x => x.Status == ListingStatus.Active)
                .ToList();

            foreach (var listing in activeListings.Take(8))
            {
                var buyers = users
                    .Where(x => x.Id != listing.SellerId)
                    .ToArray();

                context.Offers.AddRange(
                    new Offer
                    {
                        ListingId = listing.Id,
                        BuyerId = buyers[0].Id,
                        Amount = listing.Price * 0.80m,
                        Status = OfferStatus.Pending,
                        CreatedDate = now.AddHours(-6)
                    },
                    new Offer
                    {
                        ListingId = listing.Id,
                        BuyerId = buyers[1].Id,
                        Amount = listing.Price * 0.70m,
                        Status = OfferStatus.Withdrawn,
                        CreatedDate = now.AddHours(-12)
                    });

                context.Favorites.Add(new Favorite
                {
                    ListingId = listing.Id,
                    UserId = buyers[0].Id,
                    CreatedAt = now.AddHours(-3)
                });
            }

            // Prishistorikk
            foreach (var listing in listings.Where(
                         x => x.Status == ListingStatus.Sold)
                     .Take(10))
            {
                context.PriceChanges.AddRange(
                    new PriceChange
                    {
                        ListingId = listing.Id,
                        OldPrice = listing.Price + 1000,
                        NewPrice = listing.Price + 500,
                        ChangedDate = listing.CreatedDate
                            .AddDays(1)
                    },
                    new PriceChange
                    {
                        ListingId = listing.Id,
                        OldPrice = listing.Price + 500,
                        NewPrice = listing.Price,
                        ChangedDate = listing.CreatedDate
                            .AddDays(2)
                    });
            }

            await context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                user, SeedPassword);

            EnsureSuccess(result);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(
                user, role);

            EnsureSuccess(result);
        }

        return user;
    }

    private static async Task<Category> EnsureCategoryAsync(
        BruktborsenDbContext context,
        string name)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(x => x.Name == name);

        if (category is null)
        {
            category = new Category
            {
                Name = name
            };

            context.Categories.Add(category);
        }

        return category;
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(x => x.Description)));
        }
    }

    private static void AddListings(
        List<Listing> listings,
        Category category,
        ApplicationUser[] users,
        string productName,
        int totalCount,
        int soldCount,
        int basePrice,
        DateTime now)
    {
        Condition[] conditions =
        [
            Condition.New,
            Condition.LikeNew,
            Condition.Good,
            Condition.Used,
            Condition.Defect
        ];

        decimal[] factors =
        [
            1.00m,
            0.85m,
            0.70m,
            0.55m,
            0.30m
        ];

        for (var i = 0; i < totalCount; i++)
        {
            var conditionIndex = i % conditions.Length;
            var condition = conditions[conditionIndex];

            var sold = i < soldCount;

            var variation = 0.90m + (i % 5) * 0.05m;

            var estimatedPrice =
                basePrice * factors[conditionIndex] * variation;

            var price = (int)(
                Math.Round(
                    estimatedPrice / 10m,
                    MidpointRounding.AwayFromZero) * 10m);

            // Én tydelig overpriset annonse.
            if (category.Name == "Elektronikk" &&
                i == totalCount - 1)
            {
                price = basePrice * 4;
            }

            var createdDate = now.AddDays(-60 + i * 2);

            listings.Add(new Listing
            {
                Title = $"{productName} - {i + 1:D2}",

                Description =
                    $"{productName} i tilstand {condition}. " +
                    "Kan hentes etter avtale.",

                Price = price,
                OriginalPrice = basePrice * 1.5m,
                Condition = condition,

                Status = sold
                    ? ListingStatus.Sold
                    : ListingStatus.Active,

                CreatedDate = createdDate,

                SoldDate = sold
                    ? createdDate.AddDays(3 + i % 7)
                    : null,

                SoldPrice = sold
                    ? Math.Round(price * 0.95m, 2)
                    : null,

                CategoryId = category.Id,
                SellerId = users[i % users.Length].Id
            });
        }
    }
}