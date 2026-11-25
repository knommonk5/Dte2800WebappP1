using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;
    private readonly IPriceChangeService _priceChangeService;
    private readonly IAnalyticsService _analyticsService;

    public ListingService(
        IListingRepository repository,
        IPriceChangeService priceChangeService,
        IAnalyticsService analyticsService)
    {
        _repository = repository;
        _priceChangeService = priceChangeService;
        _analyticsService = analyticsService;
    }

    public async Task<List<ListingDto>> GetAllAsync(
        ListingQueryDto? query = null)
    {
        var listings = await _repository.GetAllAsync(query);

        var result = new List<ListingDto>();

        foreach (var listing in listings)
        {
            result.Add(await ToDtoAsync(listing));
        }

        return result;
    }

    public async Task<List<ListingDto>> GetMyListingsAsync(
        string sellerId)
    {
        var listings =
            await _repository.GetBySellerIdAsync(sellerId);

        var result = new List<ListingDto>();

        foreach (var listing in listings)
        {
            result.Add(await ToDtoAsync(listing));
        }

        return result;
    }

    public async Task<ListingDto?> GetByIdAsync(int id)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing == null ||
            listing.Status == ListingStatus.Removed)
        {
            return null;
        }

        return await ToDtoAsync(listing);
    }

    public async Task<ListingDto> CreateAsync(
        CreateListingDto dto,
        string sellerId)
    {
        Validate(
            dto.Title,
            dto.Description,
            dto.Price,
            dto.OriginalPrice,
            dto.Condition);

        if (!await _repository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new ArgumentException(
                "Kategorien finnes ikke.");
        }

        var listing = new Listing
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            OriginalPrice = dto.OriginalPrice,
            Condition = dto.Condition,
            ImageUrl = dto.ImageUrl?.Trim(),
            CategoryId = dto.CategoryId,
            SellerId = sellerId,
            Status = ListingStatus.Active,
            CreatedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(listing);
        await _repository.SaveChangesAsync();

        // Hent på nytt for å inkludere relasjoner.
        var created =
            await _repository.GetByIdAsync(listing.Id);

        return await ToDtoAsync(created!);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateListingDto dto,
        string userId,
        bool isAdmin)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing == null)
        {
            return false;
        }

        EnsureOwnerOrAdmin(listing, userId, isAdmin);

        // Bare aktive annonser kan redigeres.
        if (listing.Status != ListingStatus.Active)
        {
            throw new InvalidOperationException(
                "Bare aktive annonser kan endres.");
        }

        Validate(
            dto.Title,
            dto.Description,
            dto.Price,
            dto.OriginalPrice,
            dto.Condition);

        if (!await _repository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new ArgumentException(
                "Kategorien finnes ikke.");
        }

        // Registrer eventuell prisendring.
        await _priceChangeService.RegisterChangeAsync(
            listing.Id,
            listing.Price,
            dto.Price);

        listing.Title = dto.Title.Trim();
        listing.Description = dto.Description.Trim();
        listing.Price = dto.Price;
        listing.OriginalPrice = dto.OriginalPrice;
        listing.Condition = dto.Condition;
        listing.ImageUrl = dto.ImageUrl?.Trim();
        listing.CategoryId = dto.CategoryId;

        // Lagre annonse og prishistorikk atomisk.
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        string userId,
        bool isAdmin)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing == null)
        {
            return false;
        }

        EnsureOwnerOrAdmin(listing, userId, isAdmin);

        // Gjentatt fjerning skal ikke endre databasen.
        if (listing.Status == ListingStatus.Removed)
        {
            return true;
        }

        // Behold historiske salg til analysemodulen.
        if (listing.Status == ListingStatus.Sold)
        {
            throw new InvalidOperationException(
                "Solgte annonser kan ikke fjernes.");
        }

        // Soft delete.
        listing.Status = ListingStatus.Removed;

        // Avslå utestående bud på annonsen.
        foreach (var offer in listing.Offers)
        {
            if (offer.Status == OfferStatus.Pending)
            {
                offer.Status = OfferStatus.Rejected;
            }
        }

        await _repository.SaveChangesAsync();

        return true;
    }

    private static void EnsureOwnerOrAdmin(
        Listing listing,
        string userId,
        bool isAdmin)
    {
        if (listing.SellerId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException(
                "Du har ikke tilgang til å endre denne annonsen.");
        }
    }

    private static void Validate(
        string title,
        string description,
        int price,
        decimal originalPrice,
        Condition condition)
    {
        if (string.IsNullOrWhiteSpace(title) ||
            title.Length > 100)
        {
            throw new ArgumentException(
                "Tittel er påkrevd og kan ha maksimalt 100 tegn.");
        }

        if (description == null ||
            description.Length > 2000)
        {
            throw new ArgumentException(
                "Beskrivelsen kan ha maksimalt 2000 tegn.");
        }

        if (price <= 0)
        {
            throw new ArgumentException(
                "Prisen må være større enn null.");
        }

        if (originalPrice < 0)
        {
            throw new ArgumentException(
                "Opprinnelig pris kan ikke være negativ.");
        }

        if (!Enum.IsDefined(condition))
        {
            throw new ArgumentException(
                "Ugyldig tilstand.");
        }
    }

    private async Task<ListingDto> ToDtoAsync(Listing listing)
    {
        var assessment =
            await _analyticsService.GetPriceAssessmentAsync(
                listing.CategoryId,
                listing.Price,
                listing.Condition);

        return new ListingDto
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
        };
    }
}