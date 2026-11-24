using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repositories;

public class PriceChangeRepository : IPriceChangeRepository
{
    private readonly BruktborsenDbContext _context;

    public PriceChangeRepository(
        BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<List<PriceChange>> GetByListingIdAsync(
        int listingId)
    {
        return await _context.PriceChanges
            .AsNoTracking()
            .Where(p => p.ListingId == listingId)
            .OrderBy(p => p.ChangedAt)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<bool> ListingExistsAsync(
        int listingId)
    {
        return await _context.Listings
            .AnyAsync(l => l.Id == listingId);
    }

    public async Task AddAsync(
        PriceChange priceChange)
    {
        await _context.PriceChanges.AddAsync(priceChange);
    }
}