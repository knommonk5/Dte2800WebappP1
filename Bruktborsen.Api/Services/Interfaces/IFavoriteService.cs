using Bruktborsen.Api.DTOs.Listings;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IFavoriteService
{
    Task<List<ListingDto>> GetMyFavoritesAsync(string userId);

    Task<bool> AddAsync(string userId, int listingId);

    Task<bool> RemoveAsync(string userId, int listingId);
}