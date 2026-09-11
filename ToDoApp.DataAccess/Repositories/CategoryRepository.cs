using Microsoft.EntityFrameworkCore;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Repositories;

namespace ToDoApp.DataAccess.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ToDoDbContext context) : base(context)
    {
    }

    public Task<Category?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        Set.AnyAsync(
            c => c.UserId == userId
                 && c.Name == name
                 && (excludeId == null || c.Id != excludeId.Value),
            cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetTaskCountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var counts = await Context.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.CategoryId, x => x.Count);
    }
}
