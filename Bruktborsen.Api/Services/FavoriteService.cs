using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _repository;
    private readonly IAnalyticsService _analyticsService;

    public FavoriteService(
        IFavoriteRepository repository,
        IAnalyticsService analyticsService)
    {
        _repository = repository;
        _analyticsService = analyticsService;
    }

    public async Task<List<ListingDto>> GetMyFavoritesAsync(
        string userId)
    {
        var favorites = await _repository.GetByUserIdAsync(userId);

        var result = new List<ListingDto>();

        foreach (var favorite in favorites)
        {
            var listing = favorite.Listing;

            if (listing.Status != ListingStatus.Active)
                continue;

            var assessment =
                await _analyticsService.GetPriceAssessmentAsync(
                    listing.CategoryId,
                    listing.Price,
                    listing.Condition);

            result.Add(new ListingDto
            {
                Id = listing.Id,
                Title = listing.Title,
                Description = listing.Description,
                Price = listing.Price,
                OriginalPrice = listing.OriginalPrice,
                Condition = listing.Condition,
                Status = listing.Status,
                ImageUrl = listing.ImageUrl,
                CreatedDate = listing.CreatedDate,
                SoldDate = listing.SoldDate,
                SoldPrice = listing.SoldPrice,
                CategoryId = listing.CategoryId,
                CategoryName = listing.Category.Name,
                SellerId = listing.SellerId,
                SellerName = listing.Seller.FullName,
                OfferCount = listing.Offers.Count,
                PriceAssessment = assessment.Assessment
            });
        }

        return result;
    }

    public async Task<bool> AddAsync(
        string userId,
        int listingId)
    {
        var listing = await _repository.GetListingAsync(listingId);

        if (listing == null ||
            listing.Status != ListingStatus.Active)
        {
            throw new KeyNotFoundException(
                "Annonsen finnes ikke eller er ikke aktiv.");
        }

        var existing = await _repository.GetAsync(
            userId,
            listingId);

        if (existing != null)
        {
            return false;
        }

        var favorite = new Favorite
        {
            UserId = userId,
            ListingId = listingId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(favorite);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveAsync(
        string userId,
        int listingId)
    {
        var favorite = await _repository.GetAsync(
            userId,
            listingId);

        if (favorite == null)
        {
            return false;
        }

        _repository.Delete(favorite);
        await _repository.SaveChangesAsync();

        return true;
    }
}