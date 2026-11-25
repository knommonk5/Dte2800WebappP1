using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class OfferService : IOfferService
{
    private readonly IOfferRepository _repository;

    public OfferService(IOfferRepository repository)
    {
        _repository = repository;
    }

    public async Task<OfferDto> CreateAsync(
        int listingId,
        CreateOfferDto dto,
        string buyerId)
    {
        var listing = await _repository
            .GetListingByIdAsync(listingId);

        if (listing is null)
        {
            throw new KeyNotFoundException(
                "Annonsen finnes ikke.");
        }

        // Bare aktive annonser kan motta bud.
        if (listing.Status != ListingStatus.Active)
        {
            throw new InvalidOperationException(
                "Annonsen er ikke aktiv.");
        }

        // Kjøper kan ikke by på egen annonse.
        if (listing.SellerId == buyerId)
        {
            throw new ArgumentException(
                "Du kan ikke legge inn bud på egen annonse.");
        }

        // Budbeløpet må være positivt.
        if (dto.Amount <= 0)
        {
            throw new ArgumentException(
                "Budbeløpet må være større enn null.");
        }

        // Maksimalt ett ventende bud per kjøper.
        if (await _repository.HasPendingOfferAsync(
                listingId, buyerId))
        {
            throw new InvalidOperationException(
                "Du har allerede et ventende bud på denne annonsen.");
        }

        // Budet må være minst 50 prosent av annonseprisen.
        var minimumOffer = listing.Price * 0.5m;

        if (dto.Amount < minimumOffer)
        {
            throw new ArgumentException(
                $"Budet må være minst {minimumOffer:0.##} kr.");
        }

        var offer = new Offer
        {
            ListingId = listingId,
            BuyerId = buyerId,
            Amount = dto.Amount,
            Status = OfferStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        // Oppdater annonsens versjon for å oppdage samtidige bud, prisendringer, salg og fjerning.
        listing.Version = Guid.NewGuid();

        await _repository.AddAsync(offer);
        await _repository.SaveChangesAsync();

        var createdOffer = await _repository
            .GetByIdAsync(offer.Id);

        return MapToDto(createdOffer!);
    }

    public async Task<List<OfferDto>> GetMyOffersAsync(
        string buyerId)
    {
        var offers = await _repository
            .GetByBuyerIdAsync(buyerId);

        return offers.Select(MapToDto).ToList();
    }

    public async Task<List<OfferDto>> GetListingOffersAsync(
        int listingId, string sellerId)
    {
        var listing = await _repository
            .GetListingByIdAsync(listingId);

        if (listing is null)
        {
            throw new KeyNotFoundException(
                "Annonsen finnes ikke.");
        }

        if (listing.SellerId != sellerId)
        {
            throw new UnauthorizedAccessException(
                "Du har ikke tilgang til budene.");
        }

        var offers = await _repository
            .GetByListingIdAsync(listingId);

        return offers.Select(MapToDto).ToList();
    }

    public async Task<OfferDto> AcceptAsync(
        int offerId, string sellerId)
    {
        var offer = await GetPendingOfferForSellerAsync(
            offerId, sellerId);

        if (offer.Listing.Status != ListingStatus.Active)
        {
            throw new InvalidOperationException(
                "Annonsen er ikke aktiv.");
        }

        var pendingOffers = await _repository
            .GetPendingByListingIdAsync(offer.ListingId);

        // Aksepter valgt bud.
        offer.Status = OfferStatus.Accepted;

        // Registrer salget.
        offer.Listing.Status = ListingStatus.Sold;
        offer.Listing.SoldDate = DateTime.UtcNow;
        offer.Listing.SoldPrice = offer.Amount;

        // Avslå alle øvrige ventende bud.
        foreach (var otherOffer in pendingOffers)
        {
            if (otherOffer.Id != offer.Id)
            {
                otherOffer.Status = OfferStatus.Rejected;
            }
        }

        // Alle endringer lagres atomisk.
        await _repository.SaveChangesAsync();

        return MapToDto(offer);
    }

    public async Task<OfferDto> RejectAsync(
        int offerId, string sellerId)
    {
        var offer = await GetPendingOfferForSellerAsync(
            offerId, sellerId);

        offer.Status = OfferStatus.Rejected;

        await _repository.SaveChangesAsync();

        return MapToDto(offer);
    }

    public async Task<OfferDto> WithdrawAsync(
        int offerId, string buyerId)
    {
        var offer = await _repository
            .GetByIdAsync(offerId);

        if (offer is null)
        {
            throw new KeyNotFoundException(
                "Budet finnes ikke.");
        }

        if (offer.BuyerId != buyerId)
        {
            throw new UnauthorizedAccessException(
                "Du kan bare trekke tilbake egne bud.");
        }

        if (offer.Status != OfferStatus.Pending)
        {
            throw new InvalidOperationException(
                "Bare ventende bud kan trekkes tilbake.");
        }

        offer.Status = OfferStatus.Withdrawn;

        await _repository.SaveChangesAsync();

        return MapToDto(offer);
    }

    private async Task<Offer> GetPendingOfferForSellerAsync(
        int offerId, string sellerId)
    {
        var offer = await _repository
            .GetByIdAsync(offerId);

        if (offer is null)
        {
            throw new KeyNotFoundException(
                "Budet finnes ikke.");
        }

        if (offer.Listing.SellerId != sellerId)
        {
            throw new UnauthorizedAccessException(
                "Bare selgeren kan behandle budet.");
        }

        if (offer.Status != OfferStatus.Pending)
        {
            throw new InvalidOperationException(
                "Budet er allerede behandlet.");
        }

        return offer;
    }

    private static OfferDto MapToDto(Offer offer)
    {
        return new OfferDto
        {
            Id = offer.Id,
            Amount = offer.Amount,
            Status = offer.Status,
            CreatedDate = offer.CreatedDate,
            ListingId = offer.ListingId,
            ListingTitle = offer.Listing.Title,
            BuyerId = offer.BuyerId,
            BuyerName = offer.Buyer.FullName
        };
    }
}