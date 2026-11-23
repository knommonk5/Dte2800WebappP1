
using System.Security.Claims;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _service;

    public ListingsController(IListingService service)
    {
        _service = service;
    }

    // GET /api/listings
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ListingDto>>> GetAll()
    {
        var listings = await _service.GetAllAsync();
        return Ok(listings);
    }

    // GET /api/listings/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDto>> GetById(int id)
    {
        var listing = await _service.GetByIdAsync(id);

        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    // POST /api/listings
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ListingDto>> Create(
        [FromBody] CreateListingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var listing = await _service.CreateAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = listing.Id },
                listing);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT /api/listings/5
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateListingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var updated = await _service.UpdateAsync(
                id,
                dto,
                userId,
                User.IsInRole("Admin"));

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /api/listings/5
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var deleted = await _service.DeleteAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
