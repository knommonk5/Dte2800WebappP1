using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly BruktborsenDbContext _context;

    public CategoryRepository(BruktborsenDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null)
    {
        return await _context.Categories.AnyAsync(c =>
            c.Name == name &&
            (!excludeId.HasValue || c.Id != excludeId.Value));
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public Task DeleteAsync(Category category)
    {
        _context.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> HasListingsAsync(int categoryId)
    {
        return await _context.Listings
            .AnyAsync(l => l.CategoryId == categoryId);
    }
}