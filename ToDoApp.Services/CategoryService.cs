using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Exceptions;
using ToDoApp.Interfaces.Repositories;
using ToDoApp.Interfaces.Services;
using ToDoApp.Services.Mapping;

namespace ToDoApp.Services;

public class CategoryService : ICategoryService
{
    private static readonly (string Name, string Color)[] DefaultCategories =
    {
        ("Work", "#4c6ef5"),
        ("Personal", "#e8590c"),
        ("Study", "#2f9e44")
    };

    private readonly ICategoryRepository _categories;
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBoardNotifier _notifier;

    public CategoryService(
        ICategoryRepository categories,
        ITaskRepository tasks,
        IUnitOfWork unitOfWork,
        IBoardNotifier notifier)
    {
        _categories = categories;
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _categories.GetAllForUserAsync(userId, cancellationToken);
        var counts = await _categories.GetTaskCountsAsync(userId, cancellationToken);

        return categories
            .Select(c => c.ToDto(counts.TryGetValue(c.Id, out var count) ? count : 0))
            .ToList();
    }

    public async Task<CategoryDto> CreateAsync(
        Guid userId,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(userId, name, null, cancellationToken);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Color = request.Color,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            userId,
            BoardChange.Category(BoardChangeKind.CategoryCreated, category.Id),
            cancellationToken);

        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(
        Guid userId,
        Guid categoryId,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetForUserAsync(categoryId, userId, cancellationToken)
                       ?? throw NotFoundException.For("Category", categoryId);

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(userId, name, categoryId, cancellationToken);

        category.Name = name;
        category.Color = request.Color;

        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            userId,
            BoardChange.Category(BoardChangeKind.CategoryUpdated, category.Id),
            cancellationToken);

        var counts = await _categories.GetTaskCountsAsync(userId, cancellationToken);
        return category.ToDto(counts.TryGetValue(category.Id, out var count) ? count : 0);
    }

    public async Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetForUserAsync(categoryId, userId, cancellationToken)
                       ?? throw NotFoundException.For("Category", categoryId);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _tasks.ClearCategoryAsync(userId, categoryId, ct);

            _categories.Remove(category);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _notifier.NotifyAsync(
            userId,
            BoardChange.Category(BoardChangeKind.CategoryDeleted, categoryId),
            cancellationToken);
    }

    public async Task SeedDefaultsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _categories.GetAllForUserAsync(userId, cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        foreach (var (name, color) in DefaultCategories)
        {
            await _categories.AddAsync(
                new Category
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = name,
                    Color = color,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsFreeAsync(Guid userId, string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (await _categories.NameExistsAsync(userId, name, excludeId, cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }
    }
}
