using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task AddAsync(Category category);
    Task DeleteAsync(Category category);
    Task SaveChangesAsync();
    Task<bool> HasListingsAsync(int categoryId);
}