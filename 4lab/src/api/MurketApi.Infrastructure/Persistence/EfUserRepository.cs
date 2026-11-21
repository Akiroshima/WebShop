using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Common;
using MurketApi.Domain.Users;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий пользователей на EF Core.</summary>
public sealed class EfUserRepository(MurketDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(string id) => db.Users.FirstOrDefaultAsync(user => user.Id == id);

    public Task<User?> FindForLoginAsync(string login)
    {
        var trimmed = login.Trim();
        var lower = trimmed.ToLowerInvariant();
        var phone = Phone.Normalize(trimmed);
        return db.Users.FirstOrDefaultAsync(user =>
            user.Login.ToLower() == lower
            || user.Email.ToLower() == lower
            || (user.Phone != null && user.Phone == phone));
    }

    public Task<bool> EmailInUseAsync(string email, string? exceptUserId = null) =>
        db.Users.AnyAsync(user => user.Id != exceptUserId && user.Email == email);

    public Task<bool> PhoneInUseAsync(string phone, string? exceptUserId = null) =>
        db.Users.AnyAsync(user => user.Id != exceptUserId && user.Phone == phone);

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<User>> AllAsync() =>
        await db.Users.OrderBy(user => user.CreatedAt).ThenBy(user => user.Id).ToListAsync();
}
