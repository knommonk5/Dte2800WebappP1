using Bruktborsen.Api.DTOs.Listings;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IListingService
{
    Task<List<ListingDto>> GetAllAsync();
    Task<ListingDto?> GetByIdAsync(int id);

    Task<ListingDto> CreateAsync(
        CreateListingDto dto,
        string sellerId);

    Task<bool> UpdateAsync(
        int id,
        UpdateListingDto dto,
        string userId,
        bool isAdmin);

    Task<bool> DeleteAsync(
        int id,
        string userId,
        bool isAdmin);
}