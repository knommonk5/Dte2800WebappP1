
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
                .HasMaxLength(100);

            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Listing>(entity =>
        {
            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.OriginalPrice).HasPrecision(18, 2);

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
            entity.Property(x => x.Amount).HasPrecision(18, 2);

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
            entity.Property(x => x.OldPrice).HasPrecision(18, 2);
            entity.Property(x => x.NewPrice).HasPrecision(18, 2);

            entity.HasOne(x => x.Listing)
                .WithMany(x => x.PriceChanges)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
