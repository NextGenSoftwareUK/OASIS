using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class HederaQueryConfigurationTests
{
    [Fact]
    public void OmittedPolicyStaysDisabled()
    {
        var settings = JsonSerializer.Deserialize<HashgraphOASISProviderSettings>("{}");
        Assert.Null(settings!.StorageQueryPreparation);
        Assert.Null(HederaQueryPreparationConfiguration.Create(settings.StorageQueryPreparation));
    }

    [Fact]
    public void ConfiguredPolicyRoundtripsExactCeilingsAndCapturesNodes()
    {
        var settings = JsonSerializer.Deserialize<HashgraphOASISProviderSettings>("""
            {"StorageQueryPreparation":{"AuthorizedNodeAccountIds":["0.0.3","0.0.4"],"MaxPaymentTinybars":135,
            "MaxTransactionFeeTinybars":9007199254740993,"MaximumRequestsPerInvocation":17,"PersistenceTimeoutMs":8000}}
            """)!;
        settings = JsonSerializer.Deserialize<HashgraphOASISProviderSettings>(JsonSerializer.Serialize(settings))!;
        var captured = HederaQueryPreparationConfiguration.Create(settings.StorageQueryPreparation);
        settings.StorageQueryPreparation.AuthorizedNodeAccountIds[0] = "0.0.99";
        Assert.Equal(new[] { "0.0.3", "0.0.4" }, captured.AuthorizedNodeAccountIds);
        Assert.Equal(135, captured.MaxPaymentTinybars); Assert.Equal(9007199254740993, captured.MaxTransactionFeeTinybars);
        Assert.Equal(17, captured.MaximumRequestsPerInvocation); Assert.Equal(8000, captured.PersistenceTimeoutMs);
        // Actual provider configuration capture validates without invoking secrets or a paid SDK operation.
        var provider = new HashgraphOASIS(storageConfiguration: new HederaStorageConfiguration { QueryPreparation = captured,
            SigningKeyResolver = () => throw new InvalidOperationException("No key access during configuration.") });
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(1, 0)] [InlineData(-1, 1)]
    public void MissingOrInvalidCeilingsAreNotDefaultedIntoAuthorization(long payment, long fee)
    {
        var policy = HederaQueryPreparationConfiguration.Create(new HashgraphStorageQueryPreparationSettings {
            AuthorizedNodeAccountIds = new[] { "0.0.3" }, MaxPaymentTinybars = payment, MaxTransactionFeeTinybars = fee });
        Assert.Throws<ArgumentException>(() => new HashgraphOASIS(storageConfiguration: new HederaStorageConfiguration { QueryPreparation = policy }));
    }
}
