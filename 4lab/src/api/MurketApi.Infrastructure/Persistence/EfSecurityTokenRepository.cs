using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Security;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий токенов безопасности (refresh, коды восстановления, сброс пароля) на EF Core.</summary>
public sealed class EfSecurityTokenRepository(MurketDbContext db) : ISecurityTokenRepository
{
    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(string hash) =>
        db.RefreshTokens.FirstOrDefaultAsync(token => token.Hash == hash);

    public async Task<IReadOnlyList<RefreshToken>> RefreshTokensOfAsync(string userId) =>
        await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync();

    public Task RevokeRefreshTokenAsync(RefreshToken token, DateTimeOffset now)
    {
        token.Revoke(now);
        return db.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, DateTimeOffset now)
    {
        foreach (var token in await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync())
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync();
    }

    public async Task ReplaceRecoveryCodeAsync(string contact, RecoveryCode code)
    {
        db.RecoveryCodes.RemoveRange(
            await db.RecoveryCodes.Where(existing => existing.Contact == contact).ToListAsync());
        db.RecoveryCodes.Add(code);
        await db.SaveChangesAsync();
    }

    public Task<RecoveryCode?> FindLiveRecoveryCodeAsync(string contact) =>
        db.RecoveryCodes.FirstOrDefaultAsync(code => code.Contact == contact && !code.Used);

    public async Task SaveRecoveryCodeAsync(RecoveryCode code)
    {
        db.RecoveryCodes.AddIfDetached(code);
        await db.SaveChangesAsync();
    }

    public async Task AddResetTokenAsync(ResetToken token)
    {
        db.ResetTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public async Task RevokeResetTokensAsync(string userId)
    {
        db.ResetTokens.RemoveRange(
            await db.ResetTokens.Where(token => token.UserId == userId).ToListAsync());
        await db.SaveChangesAsync();
    }

    public Task<ResetToken?> FindResetTokenAsync(string hash) =>
        db.ResetTokens.FirstOrDefaultAsync(token => token.Hash == hash);

    public async Task SaveResetTokenAsync(ResetToken token)
    {
        db.ResetTokens.AddIfDetached(token);
        await db.SaveChangesAsync();
    }
}
