using Bruktborsen.Api.DTOs.Auth;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }
    
    
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return Unauthorized("Ugyldig e-post eller passord.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            dto.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized("Ugyldig e-post eller passord.");
        }

        _signInManager.AuthenticationScheme =
            IdentityConstants.BearerScheme;

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return new EmptyResult();
    }


    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(
            user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user, "User");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return StatusCode(500,
                "Kunne ikke tildele brukerrolle.");
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName
        });
    }
}