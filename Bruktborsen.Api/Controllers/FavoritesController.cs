
using System.Security.Claims;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _service;

    public FavoritesController(IFavoriteService service)
    {
        _service = service;
    }

    // GET /api/favorites
    [HttpGet]
    public async Task<ActionResult<List<ListingDto>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var favorites = await _service.GetMyFavoritesAsync(userId);

        return Ok(favorites);
    }

    // POST /api/favorites/5
    [HttpPost("{listingId:int}")]
    public async Task<IActionResult> Add(int listingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var added = await _service.AddAsync(userId, listingId);

            if (!added)
                return Conflict(new
                {
                    message = "Annonsen er allerede en favoritt."
                });

            return StatusCode(StatusCodes.Status201Created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE /api/favorites/5
    [HttpDelete("{listingId:int}")]
    public async Task<IActionResult> Remove(int listingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var removed = await _service.RemoveAsync(userId, listingId);

        if (!removed)
            return NotFound();

        return NoContent();
    }
}
