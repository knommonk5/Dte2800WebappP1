using Bruktborsen.Api.DTOs.PriceHistory;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class PriceChangeService : IPriceChangeService
{
    private readonly IPriceChangeRepository _repository;

    public PriceChangeService(
        IPriceChangeRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PriceChangeDto>> GetByListingIdAsync(
        int listingId)
    {
        if (!await _repository.ListingExistsAsync(listingId))
        {
            throw new KeyNotFoundException(
                "Annonsen finnes ikke.");
        }

        var priceChanges =
            await _repository.GetByListingIdAsync(listingId);

        return priceChanges
            .Select(ToDto)
            .ToList();
    }

    public async Task RegisterChangeAsync(
        int listingId,
        decimal oldPrice,
        decimal newPrice)
    {
        if (oldPrice <= 0 || newPrice <= 0)
        {
            throw new ArgumentException(
                "Pris må være større enn null.");
        }

        if (oldPrice == newPrice)
        {
            return;
        }

        var priceChange = new PriceChange
        {
            ListingId = listingId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(priceChange);
    }

    private static PriceChangeDto ToDto(
        PriceChange priceChange)
    {
        return new PriceChangeDto
        {
            Id = priceChange.Id,
            ListingId = priceChange.ListingId,
            OldPrice = priceChange.OldPrice,
            NewPrice = priceChange.NewPrice,
            ChangedDate = priceChange.ChangedDate
        };
    }
}