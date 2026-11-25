using Bruktborsen.Api.DTOs.Categories;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories;

namespace Bruktborsen.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _repository.GetAllAsync();

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto)
    {
        var name = ValidateName(dto.Name);

        if (await _repository.ExistsByNameAsync(name))
        {
            throw new InvalidOperationException(
                "Kategorien finnes allerede.");
        }

        var category = new Category
        {
            Name = name
        };

        await _repository.AddAsync(category);
        await _repository.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateCategoryDto dto)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
        {
            return false;
        }

        var name = ValidateName(dto.Name);

        if (await _repository.ExistsByNameAsync(name, id))
        {
            throw new InvalidOperationException(
                "Kategorien finnes allerede.");
        }

        category.Name = name;

        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
        {
            return false;
        }

        if (await _repository.HasListingsAsync(id))
        {
            throw new InvalidOperationException(
                "Kategorien kan ikke slettes fordi den har annonser.");
        }

        await _repository.DeleteAsync(category);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 50)
        {
            throw new ArgumentException(
                "Kategorinavn er påkrevd og kan ha maksimalt 50 tegn.");
        }

        return name.Trim();
    }
}