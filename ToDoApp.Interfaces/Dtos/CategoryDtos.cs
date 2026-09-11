using System.ComponentModel.DataAnnotations;

namespace ToDoApp.Interfaces.Dtos;

public class CategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Color { get; init; } = string.Empty;

    public int TaskCount { get; init; }
}

public class SaveCategoryRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "A name is required.")]
    [StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Colour must be a hex value such as #4c6ef5.")]
    public string Color { get; set; } = "#8c8c8c";
}
