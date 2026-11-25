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
            .Include(l => l.Offers)
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

        // Sorter etter oppgavens API-kontrakt.
        var sort = query.Sort.ToLowerInvariant();

        IOrderedQueryable<Listing> orderedListings =
            sort switch
            {
                "price_asc" =>
                    listings.OrderBy(l => l.Price),

                "price_desc" =>
                    listings.OrderByDescending(l => l.Price),

                _ =>
                    listings.OrderByDescending(l => l.CreatedDate)
            };

        // Stabil sortering ved identiske verdier.
        return await orderedListings
            .ThenBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetBySellerIdAsync(
        string sellerId)
    {
        return await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .Include(l => l.Offers)
            .Where(l => l.SellerId == sellerId)
            .OrderByDescending(l => l.CreatedDate)
            .ThenBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<Listing?> GetByIdAsync(int id)
    {
        return await _context.Listings
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .Include(l => l.Offers)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task AddAsync(Listing listing)
    {
        await _context.Listings.AddAsync(listing);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Annonsen har blitt endret av en annen forespørsel.",
                ex);
        }
    }
}