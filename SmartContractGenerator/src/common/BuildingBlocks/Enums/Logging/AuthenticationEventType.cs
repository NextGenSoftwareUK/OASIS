namespace BuildingBlocks.Enums.Logging;

public enum AuthenticationEventType
{
    Login,
    Logout,
    LoginFailed,
    PasswordChanged,
    AccountLocked,
    AccountUnlocked,
    TwoFactorEnabled,
    TwoFactorDisabled,
    SessionExpired,
    ForcedLogout,
    RefreshToken,
    InvalidToken
}