using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(Guid userId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetTaskCountsAsync(Guid userId, CancellationToken cancellationToken = default);
}
