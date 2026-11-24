using System.Security.Claims;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly IOfferService _service;

    public OffersController(IOfferService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<OfferDto>> Create(
        CreateOfferDto dto)
    {
        try
        {
            var offer = await _service.CreateAsync(
                dto, GetUserId());

            return StatusCode(
                StatusCodes.Status201Created, offer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<OfferDto>>> GetMine()
    {
        var offers = await _service.GetMyOffersAsync(
            GetUserId());

        return Ok(offers);
    }

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
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id:int}/accept")]
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
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}/reject")]
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
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}/withdraw")]
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
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
    }
}