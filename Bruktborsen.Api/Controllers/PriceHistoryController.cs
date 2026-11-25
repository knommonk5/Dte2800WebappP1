using Bruktborsen.Api.DTOs.PriceHistory;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/analytics/listings/{listingId:int}/price-history")]
public class PriceHistoryController : ControllerBase
{
    private readonly IPriceChangeService _service;

    public PriceHistoryController(
        IPriceChangeService service)
    {
        _service = service;
    }

    // GET /api/analytics/listings/5/price-history
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<PriceChangeDto>>> GetByListingId(
        int listingId)
    {
        try
        {
            var priceHistory =
                await _service.GetByListingIdAsync(listingId);

            return Ok(priceHistory);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}