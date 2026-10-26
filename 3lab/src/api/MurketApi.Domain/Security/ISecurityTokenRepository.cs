namespace MurketApi.Domain.Security;

/// <summary>
/// Токены безопасности: refresh-токены, коды и reset-токены восстановления доступа.
/// Заглушка БД: InMemorySecurityTokenRepository.
/// </summary>
public interface ISecurityTokenRepository
{
    // ----- Refresh-токены -----
    void AddRefreshToken(RefreshToken token);

    RefreshToken? FindRefreshToken(string hash);

    IReadOnlyList<RefreshToken> RefreshTokensOf(string userId);

    void RevokeRefreshToken(RefreshToken token, DateTimeOffset now);

    void RevokeAllRefreshTokens(string userId, DateTimeOffset now);

    // ----- Коды восстановления -----
    void ReplaceRecoveryCode(string contact, RecoveryCode code);

    RecoveryCode? FindLiveRecoveryCode(string contact);

    void SaveRecoveryCode(RecoveryCode code);

    // ----- Reset-токены -----
    void AddResetToken(ResetToken token);

    void RevokeResetTokens(string userId);

    ResetToken? FindResetToken(string hash);

    void SaveResetToken(ResetToken token);
}
