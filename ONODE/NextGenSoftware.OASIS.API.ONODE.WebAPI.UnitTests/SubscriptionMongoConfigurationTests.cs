using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class SubscriptionMongoConfigurationTests
{
    [Fact]
    public void ExplicitConnectionOverridesDnaWithoutReadingIt()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [SubscriptionMongoConfiguration.ConnectionSetting] = "mongodb://explicit.example/",
            [SubscriptionMongoConfiguration.DatabaseSetting] = "oasis_subscription_accounting"
        }).Build();
        bool dnaRead = false;

        string result = SubscriptionMongoConfiguration.ConnectionString(configuration, _ =>
        {
            dnaRead = true;
            return "mongodb://dna.example/";
        });

        Assert.Equal("mongodb://explicit.example/", result);
        Assert.False(dnaRead);
        Assert.Equal("oasis_subscription_accounting", SubscriptionMongoConfiguration.DatabaseName(configuration));
    }

    [Fact]
    public void DnaConnectionIsTheCanonicalDefault()
    {
        var configuration = new ConfigurationBuilder().Build();

        string result = SubscriptionMongoConfiguration.ConnectionString(
            configuration, _ => "mongodb+srv://dna.example/");

        Assert.Equal("mongodb+srv://dna.example/", result);
    }

    [Fact]
    public void MissingConnectionAndDedicatedDatabaseFailClosed()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            SubscriptionMongoConfiguration.ConnectionString(configuration, _ => null));
        Assert.Equal(SubscriptionMongoConfiguration.DefaultDatabase,
            SubscriptionMongoConfiguration.DatabaseName(configuration));
    }

    [Fact]
    public void EmptyInitializationRequiresTheExactAuditedMode()
    {
        var empty = new ConfigurationBuilder().Build();
        var enabled = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [SubscriptionMongoConfiguration.InitialStateSetting] = SubscriptionMongoConfiguration.EmptyInstallation
        }).Build();
        var typo = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [SubscriptionMongoConfiguration.InitialStateSetting] = "empty"
        }).Build();

        Assert.False(SubscriptionMongoConfiguration.InitializeEmptyLedger(empty));
        Assert.True(SubscriptionMongoConfiguration.InitializeEmptyLedger(enabled));
        Assert.Throws<InvalidOperationException>(() => SubscriptionMongoConfiguration.InitializeEmptyLedger(typo));
    }

    [Fact]
    public void ExistingRailwayStripeVariablesOverrideDnaValues()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["STRIPE_SECRET_KEY"] = "sk_existing",
            ["STRIPE_WEBHOOK_SECRET"] = "whsec_existing",
            ["STRIPE_PRICE_GOLD"] = "price_existing"
        }).Build();

        Assert.Equal("sk_existing", SubscriptionStripeConfiguration.SecretKey(configuration));
        Assert.Equal("whsec_existing", SubscriptionStripeConfiguration.WebhookSecret(configuration));
        Assert.Equal("price_existing", SubscriptionStripeConfiguration.PriceId(configuration, "gold"));
        Assert.Null(SubscriptionStripeConfiguration.PriceId(configuration, "unknown"));
    }
}
