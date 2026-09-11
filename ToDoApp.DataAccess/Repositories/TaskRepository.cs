using Microsoft.EntityFrameworkCore;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Repositories;

namespace ToDoApp.DataAccess.Repositories;

public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    public TaskRepository(ToDoDbContext context) : base(context)
    {
    }

    public Task<TaskItem?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Set.Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        TaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(Set.AsNoTracking().Include(t => t.Category), userId, query);

        var total = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderBy(t => t.Position)
            .ThenBy(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<List<TaskItem>> GetColumnForUpdateAsync(
        Guid userId,
        TaskState state,
        CancellationToken cancellationToken = default) =>
        Set.Where(t => t.UserId == userId && t.State == state)
            .OrderBy(t => t.Position)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountInColumnAsync(Guid userId, TaskState state, CancellationToken cancellationToken = default) =>
        Set.CountAsync(t => t.UserId == userId && t.State == state, cancellationToken);

    public Task<int> ClearCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default) =>
        Set.Where(t => t.UserId == userId && t.CategoryId == categoryId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.CategoryId, (Guid?)null)
                    .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);

    private static IQueryable<TaskItem> ApplyFilters(IQueryable<TaskItem> source, Guid userId, TaskQuery query)
    {
        source = source.Where(t => t.UserId == userId);

        if (query.State.HasValue)
        {
            source = source.Where(t => t.State == query.State.Value);
        }

        if (query.CategoryId.HasValue)
        {
            source = source.Where(t => t.CategoryId == query.CategoryId.Value);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = "%" + Escape(search) + "%";
            source = source.Where(t =>
                EF.Functions.Like(t.Title, pattern, EscapeCharacter) ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, EscapeCharacter)));
        }

        return source;
    }

    private const string EscapeCharacter = "\\";

    private static string Escape(string term) => term
        .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
        .Replace("%", EscapeCharacter + "%")
        .Replace("_", EscapeCharacter + "_")
        .Replace("[", EscapeCharacter + "[");
}
