using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [Authorize]
    [HttpGet("price-suggestion")]
    public async Task<ActionResult<PriceSuggestionDto>> GetPriceSuggestion(
        [FromQuery] int categoryId,
        [FromQuery] Condition? condition)
    {
        if (categoryId <= 0)
            return BadRequest(new
            {
                message = "Ugyldig kategori-ID."
            });

        if (!condition.HasValue ||
            !Enum.IsDefined(condition.Value))
        {
            return BadRequest(new
            {
                message = "En gyldig tilstand må angis."
            });
        }

        try
        {
            var result =
                await _analyticsService.GetPriceSuggestionAsync(
                    categoryId,
                    condition.Value);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("categories/{id:int}/stats")]
    public async Task<ActionResult<CategoryStatsDto>> GetCategoryStats(
        int id)
    {
        try
        {
            var result =
                await _analyticsService.GetCategoryStatsAsync(id);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("overview")]
    public async Task<ActionResult<MarketOverviewDto>> GetOverview()
    {
        var result = await _analyticsService.GetOverviewAsync();

        return Ok(result);
    }
}