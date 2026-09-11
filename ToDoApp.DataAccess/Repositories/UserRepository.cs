using Microsoft.EntityFrameworkCore;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Repositories;

namespace ToDoApp.DataAccess.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ToDoDbContext context) : base(context)
    {
    }

    public Task<User?> GetByProviderAsync(
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderKey == providerKey, cancellationToken);
}
