using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories.Interfaces;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(int id);

    Task<List<Offer>> GetByBuyerIdAsync(string buyerId);

    Task<List<Offer>> GetByListingIdAsync(int listingId);

    Task<List<Offer>> GetPendingByListingIdAsync(int listingId);

    Task<Listing?> GetListingByIdAsync(int listingId);

    Task AddAsync(Offer offer);

    Task SaveChangesAsync();
}