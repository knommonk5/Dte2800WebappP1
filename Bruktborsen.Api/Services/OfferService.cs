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
        CreateOfferDto dto, string buyerId)
    {
        var listing = await _repository
            .GetListingByIdAsync(dto.ListingId);

        if (listing is null)
            throw new KeyNotFoundException(
                "Annonsen finnes ikke.");

        if (listing.Status != ListingStatus.Active)
            throw new InvalidOperationException(
                "Annonsen er ikke aktiv.");

        if (listing.SellerId == buyerId)
            throw new InvalidOperationException(
                "Du kan ikke legge inn bud på egen annonse.");

        if (dto.Amount <= 0)
            throw new ArgumentException(
                "Budbeløpet må være større enn null.");

        var offer = new Offer
        {
            ListingId = dto.ListingId,
            BuyerId = buyerId,
            Amount = dto.Amount,
            Status = OfferStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

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
            throw new KeyNotFoundException(
                "Annonsen finnes ikke.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException(
                "Du har ikke tilgang til budene.");

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
            throw new InvalidOperationException(
                "Annonsen er ikke aktiv.");

        var pendingOffers = await _repository
            .GetPendingByListingIdAsync(offer.ListingId);

        offer.Status = OfferStatus.Accepted;

        offer.Listing.Status = ListingStatus.Sold;
        offer.Listing.SoldAt = DateTime.UtcNow;

        foreach (var otherOffer in pendingOffers)
        {
            if (otherOffer.Id != offer.Id)
            {
                otherOffer.Status = OfferStatus.Rejected;
            }
        }

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
            throw new KeyNotFoundException(
                "Budet finnes ikke.");

        if (offer.BuyerId != buyerId)
            throw new UnauthorizedAccessException(
                "Du kan bare trekke tilbake egne bud.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException(
                "Bare ventende bud kan trekkes tilbake.");

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
            throw new KeyNotFoundException(
                "Budet finnes ikke.");

        if (offer.Listing.SellerId != sellerId)
            throw new UnauthorizedAccessException(
                "Bare selgeren kan behandle budet.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException(
                "Budet er allerede behandlet.");

        return offer;
    }

    private static OfferDto MapToDto(Offer offer)
    {
        return new OfferDto
        {
            Id = offer.Id,
            Amount = offer.Amount,
            Status = offer.Status,
            CreatedAt = offer.CreatedAt,
            ListingId = offer.ListingId,
            ListingTitle = offer.Listing.Title,
            BuyerId = offer.BuyerId,
            BuyerName = offer.Buyer.FullName
        };
    }
}