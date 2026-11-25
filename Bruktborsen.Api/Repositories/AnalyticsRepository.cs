using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly BruktborsenDbContext _context;

    public AnalyticsRepository(BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetSoldListingsByCategoryAsync(
        int categoryId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Where(l => l.CategoryId == categoryId &&
                        l.Status == ListingStatus.Sold &&
                        l.SoldPrice.HasValue)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetListingsByCategoryAsync(
        int categoryId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.PriceChanges)
            .Where(l => l.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetAllListingsAsync()
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.PriceChanges)
            .ToListAsync();
    }
}