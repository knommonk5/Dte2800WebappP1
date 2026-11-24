using Bruktborsen.Api.DTOs.PriceHistory;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IPriceChangeService
{
    Task<List<PriceChangeDto>> GetByListingIdAsync(
        int listingId);

    Task RegisterChangeAsync(
        int listingId,
        decimal oldPrice,
        decimal newPrice);
}