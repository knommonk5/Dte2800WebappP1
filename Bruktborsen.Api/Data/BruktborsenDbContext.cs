using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Data;

public class BruktborsenDbContext : IdentityDbContext<ApplicationUser>
{
    public BruktborsenDbContext(
        DbContextOptions<BruktborsenDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<PriceChange> PriceChanges => Set<PriceChange>();
    
    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        UpdateListingVersions();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateListingVersions();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void UpdateListingVersions()
    {
        var modifiedListings = ChangeTracker
            .Entries<Listing>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in modifiedListings)
        {
            entry.Entity.Version = Guid.NewGuid();
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        builder.Entity<Listing>(entity =>
        {
            entity.Property(x => x.Status)
                .IsConcurrencyToken();
            
            entity.Property(x => x.Version)
                .IsConcurrencyToken();

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.ImageUrl)
                .HasMaxLength(2048);

            entity.Property(x => x.OriginalPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.SoldPrice)
                .HasPrecision(18, 2);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Listings_Price_Positive",
                    "\"Price\" > 0");
            });

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Listings)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Seller)
                .WithMany(x => x.Listings)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Offer>(entity =>
        {
            entity.Property(x => x.Status)
                .IsConcurrencyToken();
            
            entity.HasIndex(x => new { x.ListingId, x.BuyerId })
                .IsUnique()
                .HasDatabaseName("IX_Offers_UniquePending")
                .HasFilter($"\"Status\" = {(int)OfferStatus.Pending}");

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Offers)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Buyer)
                .WithMany(x => x.Offers)
                .HasForeignKey(x => x.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.ListingId })
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PriceChange>(entity =>
        {
            entity.Property(x => x.OldPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.NewPrice)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Listing)
                .WithMany(x => x.PriceChanges)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}