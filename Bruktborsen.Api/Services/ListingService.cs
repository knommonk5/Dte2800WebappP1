
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

    public ListingService(
        IListingRepository repository,
        IPriceChangeService priceChangeService)
    {
        _repository = repository;
        _priceChangeService = priceChangeService;
    }

    public async Task<List<ListingDto>> GetAllAsync()
    {
        var listings = await _repository.GetAllAsync();

        return listings
            .Where(l => l.Status == ListingStatus.Active)
            .Select(ToDto)
            .ToList();
    }

    public async Task<ListingDto?> GetByIdAsync(int id)
    {
        var listing = await _repository.GetByIdAsync(id);

        if (listing == null ||
            listing.Status != ListingStatus.Active)
        {
            return null;
        }

        return ToDto(listing);
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
            throw new ArgumentException("Kategorien finnes ikke.");
        }

        var listing = new Listing
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            OriginalPrice = dto.OriginalPrice,
            Condition = dto.Condition,
            CategoryId = dto.CategoryId,
            SellerId = sellerId,
            Status = ListingStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(listing);
        await _repository.SaveChangesAsync();

        // Hent på nytt for å inkludere kategori og selger.
        var created = await _repository.GetByIdAsync(listing.Id);

        return ToDto(created!);
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

        _repository.Delete(listing);
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
        decimal price,
        decimal originalPrice,
        Condition condition)
    {
        if (string.IsNullOrWhiteSpace(title) ||
            title.Trim().Length < 3)
        {
            throw new ArgumentException(
                "Tittel må inneholde minst 3 tegn.");
        }

        if (string.IsNullOrWhiteSpace(description) ||
            description.Trim().Length < 10)
        {
            throw new ArgumentException(
                "Beskrivelsen må inneholde minst 10 tegn.");
        }

        if (price < 0 || originalPrice < 0)
        {
            throw new ArgumentException(
                "Pris kan ikke være negativ.");
        }

        if (!Enum.IsDefined(condition))
        {
            throw new ArgumentException(
                "Ugyldig tilstand.");
        }
    }

    private static ListingDto ToDto(Listing listing)
    {
        return new ListingDto
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            OriginalPrice = listing.OriginalPrice,
            Condition = listing.Condition,
            Status = listing.Status,
            CreatedAt = listing.CreatedAt,
            SoldAt = listing.SoldAt,
            CategoryId = listing.CategoryId,
            CategoryName = listing.Category.Name,
            SellerId = listing.SellerId,
            SellerName = listing.Seller.FullName
        };
    }
}
