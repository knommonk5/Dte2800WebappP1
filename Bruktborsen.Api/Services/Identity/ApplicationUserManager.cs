using Bruktborsen.Api.Models;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bruktborsen.Api.Services.Identity;

public class ApplicationUserManager
    : UserManager<ApplicationUser>
{
    private readonly BruktborsenDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationUserManager(
        IUserStore<ApplicationUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ApplicationUser>> logger,
        BruktborsenDbContext context,
        IHttpContextAccessor httpContextAccessor)
        : base(
            store,
            optionsAccessor,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        string password)
    {
        var request = _httpContextAccessor.HttpContext?.Request;

        if (request is not null &&
            HttpMethods.IsPost(request.Method) &&
            string.Equals(
                request.Path.Value?.TrimEnd('/'),
                "/api/auth/register",
                StringComparison.OrdinalIgnoreCase))
        {
            request.Body.Position = 0;

            var registration =
                await request.ReadFromJsonAsync<RegisterDto>();

            if (registration is null ||
                string.IsNullOrWhiteSpace(registration.FullName) ||
                registration.FullName.Length > 150)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidFullName",
                    Description =
                        "Fullt navn er påkrevd og kan ha maksimalt 150 tegn."
                });
            }

            user.FullName = registration.FullName.Trim();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var result = await base.CreateAsync(user, password);

            if (!result.Succeeded)
                return result;

            var roleResult = await AddToRoleAsync(user, "User");

            if (!roleResult.Succeeded)
                return roleResult;

            await transaction.CommitAsync();

            return IdentityResult.Success;
        }

        return await base.CreateAsync(user, password);
    }
}