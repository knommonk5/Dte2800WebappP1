
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _repository;

    public FavoriteService(IFavoriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ListingDto>> GetMyFavoritesAsync(
        string userId)
    {
        var favorites = await _repository.GetByUserIdAsync(userId);

        return favorites
            .Where(f => f.Listing.Status == ListingStatus.Active)
            .Select(f => new ListingDto
            {
                Id = f.Listing.Id,
                Title = f.Listing.Title,
                Description = f.Listing.Description,
                Price = f.Listing.Price,
                OriginalPrice = f.Listing.OriginalPrice,
                Condition = f.Listing.Condition,
                Status = f.Listing.Status,
                CreatedAt = f.Listing.CreatedAt,
                SoldAt = f.Listing.SoldAt,
                CategoryId = f.Listing.CategoryId,
                CategoryName = f.Listing.Category.Name,
                SellerId = f.Listing.SellerId,
                SellerName = f.Listing.Seller.FullName
            })
            .ToList();
    }

    public async Task<bool> AddAsync(
        string userId,
        int listingId)
    {
        var listing = await _repository.GetListingAsync(listingId);

        if (listing == null ||
            listing.Status != ListingStatus.Active)
        {
            throw new KeyNotFoundException(
                "Annonsen finnes ikke eller er ikke aktiv.");
        }

        var existing = await _repository.GetAsync(
            userId,
            listingId);

        if (existing != null)
        {
            return false;
        }

        var favorite = new Favorite
        {
            UserId = userId,
            ListingId = listingId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(favorite);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveAsync(
        string userId,
        int listingId)
    {
        var favorite = await _repository.GetAsync(
            userId,
            listingId);

        if (favorite == null)
        {
            return false;
        }

        _repository.Delete(favorite);
        await _repository.SaveChangesAsync();

        return true;
    }
}
