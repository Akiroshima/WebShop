namespace MurketApi.Domain.Security;

/// <summary>
/// Токены безопасности: refresh-токены, коды и reset-токены восстановления доступа.
/// Реализация — EF Core (PostgreSQL).
/// </summary>
public interface ISecurityTokenRepository
{
    // ----- Refresh-токены -----
    Task AddRefreshTokenAsync(RefreshToken token);

    Task<RefreshToken?> FindRefreshTokenAsync(string hash);

    Task<IReadOnlyList<RefreshToken>> RefreshTokensOfAsync(string userId);

    Task RevokeRefreshTokenAsync(RefreshToken token, DateTimeOffset now);

    Task RevokeAllRefreshTokensAsync(string userId, DateTimeOffset now);

    // ----- Коды восстановления -----
    Task ReplaceRecoveryCodeAsync(string contact, RecoveryCode code);

    Task<RecoveryCode?> FindLiveRecoveryCodeAsync(string contact);

    Task SaveRecoveryCodeAsync(RecoveryCode code);

    // ----- Reset-токены -----
    Task AddResetTokenAsync(ResetToken token);

    Task RevokeResetTokensAsync(string userId);

    Task<ResetToken?> FindResetTokenAsync(string hash);

    Task SaveResetTokenAsync(ResetToken token);
}
