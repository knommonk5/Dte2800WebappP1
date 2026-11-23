using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly BruktborsenDbContext _context;

    public FavoriteRepository(BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<List<Favorite>> GetByUserIdAsync(string userId)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Include(f => f.Listing)
            .ThenInclude(l => l.Category)
            .Include(f => f.Listing)
            .ThenInclude(l => l.Seller)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Favorite?> GetAsync(
        string userId,
        int listingId)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f =>
                f.UserId == userId &&
                f.ListingId == listingId);
    }

    public async Task<Listing?> GetListingAsync(int listingId)
    {
        return await _context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listingId);
    }

    public async Task AddAsync(Favorite favorite)
    {
        await _context.Favorites.AddAsync(favorite);
    }

    public void Delete(Favorite favorite)
    {
        _context.Favorites.Remove(favorite);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}