using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories.Interfaces;

public interface IPriceChangeRepository
{
    Task<List<PriceChange>> GetByListingIdAsync(
        int listingId);

    Task<bool> ListingExistsAsync(
        int listingId);

    Task AddAsync(
        PriceChange priceChange);
}