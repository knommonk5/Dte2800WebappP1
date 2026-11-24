using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
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

    public async Task<List<Listing>> GetAllAsync(
        ListingQueryDto? query = null)
    {
        query ??= new ListingQueryDto();

        IQueryable<Listing> listings = _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .Where(l => l.Status == ListingStatus.Active);

        // Søk i tittel og beskrivelse.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();

            listings = listings.Where(l =>
                l.Title.ToLower().Contains(search) ||
                l.Description.ToLower().Contains(search));
        }

        // Filtrer etter kategori.
        if (query.CategoryId.HasValue)
        {
            listings = listings.Where(l =>
                l.CategoryId == query.CategoryId.Value);
        }

        // Filtrer etter minimumspris.
        if (query.MinPrice.HasValue)
        {
            listings = listings.Where(l =>
                l.Price >= query.MinPrice.Value);
        }

        // Filtrer etter maksimumspris.
        if (query.MaxPrice.HasValue)
        {
            listings = listings.Where(l =>
                l.Price <= query.MaxPrice.Value);
        }

        // Filtrer etter tilstand.
        if (query.Condition.HasValue)
        {
            listings = listings.Where(l =>
                l.Condition == query.Condition.Value);
        }

        // Bestem sorteringsretning.
        var ascending = string.Equals(
            query.SortOrder,
            "asc",
            StringComparison.OrdinalIgnoreCase);

        var sortBy = query.SortBy.ToLowerInvariant();

        // Sorter resultatet.
        IOrderedQueryable<Listing> orderedListings =
            sortBy switch
            {
                "price" => ascending
                    ? listings.OrderBy(l => (double)l.Price)
                    : listings.OrderByDescending(l => (double)l.Price),

                "title" => ascending
                    ? listings.OrderBy(l => l.Title)
                    : listings.OrderByDescending(l => l.Title),

                _ => ascending
                    ? listings.OrderBy(l => l.CreatedAt)
                    : listings.OrderByDescending(l => l.CreatedAt)
            };

        // Stabil sortering ved identiske verdier.
        return await orderedListings
            .ThenBy(l => l.Id)
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