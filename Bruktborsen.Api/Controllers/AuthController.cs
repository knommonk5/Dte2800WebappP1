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

    public AuthController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles
        });
    }
}