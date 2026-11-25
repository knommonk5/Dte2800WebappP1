using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs.Categories;

public class UpdateCategoryDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
}