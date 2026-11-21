using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Users;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий адресов доставки на EF Core.</summary>
public sealed class EfAddressRepository(MurketDbContext db) : IAddressRepository
{
    public async Task<IReadOnlyList<Address>> ForUserAsync(string userId) =>
        await db.Addresses
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Label)
            .ToListAsync();

    public Task<Address?> FindForUserAsync(string userId, string addressId) =>
        db.Addresses.FirstOrDefaultAsync(address => address.Id == addressId && address.UserId == userId);

    public Task<int> CountForUserAsync(string userId) =>
        db.Addresses.CountAsync(address => address.UserId == userId);

    public async Task AddAsync(Address address)
    {
        db.Addresses.Add(address);
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Address address)
    {
        db.Addresses.Remove(address);
        await db.SaveChangesAsync();
    }
}
