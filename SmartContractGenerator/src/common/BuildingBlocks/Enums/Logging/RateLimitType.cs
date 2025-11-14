namespace BuildingBlocks.Enums.Logging;

public enum RateLimitType
{
    ApiCalls,
    FileUploads,
    EmailSending,
    SmsDelivery,
    DatabaseConnections,
    ConcurrentUsers,
    BandwidthUsage,
    StorageUsage,
    LoginAttempts,
    PasswordResets
}