using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    Task<bool> CategoryExistsAsync(int categoryId);

    Task<List<Category>> GetCategoriesAsync();

    Task<List<Listing>> GetSoldListingsByCategoryAsync(
        int categoryId);

    Task<List<Listing>> GetListingsByCategoryAsync(
        int categoryId);

    Task<List<Listing>> GetAllListingsAsync();
}