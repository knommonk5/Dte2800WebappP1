using Bruktborsen.Api.DTOs.Offers;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IOfferService
{
    Task<OfferDto> CreateAsync(
        int listingId,
        CreateOfferDto dto,
        string buyerId);

    Task<List<OfferDto>> GetMyOffersAsync(
        string buyerId);

    Task<List<OfferDto>> GetListingOffersAsync(
        int listingId, string sellerId);

    Task<OfferDto> AcceptAsync(
        int offerId, string sellerId);

    Task<OfferDto> RejectAsync(
        int offerId, string sellerId);

    Task<OfferDto> WithdrawAsync(
        int offerId, string buyerId);
}