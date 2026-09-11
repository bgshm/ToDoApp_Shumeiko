using ToDoApp.Interfaces.Dtos;

namespace ToDoApp.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(Guid userId, SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(Guid userId, Guid categoryId, SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);

    Task SeedDefaultsAsync(Guid userId, CancellationToken cancellationToken = default);
}
