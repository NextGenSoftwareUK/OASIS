using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

/// <summary>
/// The prepared-usage tests write to a real MongoDB replica set
/// (oasisRecoveryRs on 127.0.0.1:27029). They run only where that owned
/// replica set is provisioned and SUBSCRIPTION_TEST_MONGODB_URI names it;
/// everywhere else they are skipped instead of failing.
/// </summary>
internal static class SubscriptionMongo
{
    public const string Uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";

    public static string? SkipReason =>
        Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") == Uri
            ? null
            : "Needs the owned MongoDB replica set: set SUBSCRIPTION_TEST_MONGODB_URI to " + Uri;
}

public sealed class SubscriptionMongoFactAttribute : FactAttribute
{
    public SubscriptionMongoFactAttribute() => Skip = SubscriptionMongo.SkipReason;
}

public sealed class SubscriptionMongoTheoryAttribute : TheoryAttribute
{
    public SubscriptionMongoTheoryAttribute() => Skip = SubscriptionMongo.SkipReason;
}
