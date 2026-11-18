using Microsoft.AspNetCore.Identity;

namespace Bruktborsen.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<Listing> Listings { get; set; }
        = new List<Listing>();

    public ICollection<Offer> Offers { get; set; }
        = new List<Offer>();

    public ICollection<Favorite> Favorites { get; set; }
        = new List<Favorite>();
}