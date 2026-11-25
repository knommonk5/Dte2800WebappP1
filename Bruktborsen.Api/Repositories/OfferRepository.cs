using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Bruktborsen.Api.Repositories;

public class OfferRepository : IOfferRepository
{
    private readonly BruktborsenDbContext _context;

    public OfferRepository(BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(int id)
    {
        return await _context.Offers
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Offer>> GetByBuyerIdAsync(
        string buyerId)
    {
        return await _context.Offers
            .AsNoTracking()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedDate)
            .ThenBy(o => o.Id)
            .ToListAsync();
    }

    public async Task<List<Offer>> GetByListingIdAsync(
        int listingId)
    {
        return await _context.Offers
            .AsNoTracking()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Where(o => o.ListingId == listingId)
            .OrderByDescending(o => o.CreatedDate)
            .ThenBy(o => o.Id)
            .ToListAsync();
    }

    public async Task<List<Offer>> GetPendingByListingIdAsync(
        int listingId)
    {
        return await _context.Offers
            .Where(o =>
                o.ListingId == listingId &&
                o.Status == OfferStatus.Pending)
            .ToListAsync();
    }

    public async Task<Listing?> GetListingByIdAsync(
        int listingId)
    {
        return await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == listingId);
    }

    public async Task<bool> HasPendingOfferAsync(
        int listingId, string buyerId)
    {
        return await _context.Offers.AnyAsync(o =>
            o.ListingId == listingId &&
            o.BuyerId == buyerId &&
            o.Status == OfferStatus.Pending);
    }

    public async Task AddAsync(Offer offer)
    {
        await _context.Offers.AddAsync(offer);
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
                "Budet eller annonsen har blitt endret av en annen forespørsel.",
                ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqliteException sqlite &&
                  sqlite.SqliteExtendedErrorCode == 2067 &&
                  sqlite.Message.Contains(
                      "Offers.ListingId, Offers.BuyerId",
                      StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Du har allerede et ventende bud på denne annonsen.",
                ex);
        }
    }
}