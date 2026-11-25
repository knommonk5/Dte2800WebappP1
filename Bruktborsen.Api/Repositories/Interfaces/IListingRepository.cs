using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories.Interfaces;

public interface IListingRepository
{
    Task<List<Listing>> GetAllAsync(
        ListingQueryDto? query = null);

    Task<List<Listing>> GetBySellerIdAsync(
        string sellerId);

    Task<Listing?> GetByIdAsync(int id);

    Task AddAsync(Listing listing);

    Task<bool> CategoryExistsAsync(int categoryId);

    Task SaveChangesAsync();
}