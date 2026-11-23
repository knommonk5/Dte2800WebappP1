using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly BruktborsenDbContext _context;

    public ListingRepository(BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<List<Listing>> GetAllAsync()
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<Listing?> GetByIdAsync(int id)
    {
        return await _context.Listings
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task AddAsync(Listing listing)
    {
        await _context.Listings.AddAsync(listing);
    }

    public void Delete(Listing listing)
    {
        _context.Listings.Remove(listing);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}