using System.Security.Claims;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/offers")]
public class OffersController : ControllerBase
{
    private readonly IOfferService _service;

    public OffersController(IOfferService service)
    {
        _service = service;
    }

    // POST /api/listings/5/offers
    [HttpPost("/api/listings/{id:int}/offers")]
    public async Task<ActionResult<OfferDto>> Create(
        int id,
        [FromBody] CreateOfferDto dto)
    {
        try
        {
            var offer = await _service.CreateAsync(
                id, dto, GetUserId());

            return StatusCode(
                StatusCodes.Status201Created, offer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET /api/offers/my
    [HttpGet("my")]
    public async Task<ActionResult<List<OfferDto>>> GetMyOffers()
    {
        var offers = await _service.GetMyOffersAsync(
            GetUserId());

        return Ok(offers);
    }

    // GET /api/listings/5/offers
    [HttpGet("/api/listings/{id:int}/offers")]
    public async Task<ActionResult<List<OfferDto>>>
        GetListingOffers(int id)
    {
        try
        {
            var offers = await _service.GetListingOffersAsync(
                id, GetUserId());

            return Ok(offers);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // POST /api/offers/5/accept
    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<OfferDto>> Accept(int id)
    {
        try
        {
            var offer = await _service.AcceptAsync(
                id, GetUserId());

            return Ok(offer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST /api/offers/5/reject
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<OfferDto>> Reject(int id)
    {
        try
        {
            var offer = await _service.RejectAsync(
                id, GetUserId());

            return Ok(offer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST /api/offers/5/withdraw
    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<OfferDto>> Withdraw(int id)
    {
        try
        {
            var offer = await _service.WithdrawAsync(
                id, GetUserId());

            return Ok(offer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
    }
}