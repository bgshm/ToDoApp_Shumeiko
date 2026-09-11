using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByProviderAsync(string provider, string providerKey, CancellationToken cancellationToken = default);
}
