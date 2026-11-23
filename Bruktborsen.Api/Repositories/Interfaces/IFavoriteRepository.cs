using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories.Interfaces;

public interface IFavoriteRepository
{
    Task<List<Favorite>> GetByUserIdAsync(string userId);

    Task<Favorite?> GetAsync(string userId, int listingId);

    Task<Listing?> GetListingAsync(int listingId);

    Task AddAsync(Favorite favorite);

    void Delete(Favorite favorite);

    Task SaveChangesAsync();
}