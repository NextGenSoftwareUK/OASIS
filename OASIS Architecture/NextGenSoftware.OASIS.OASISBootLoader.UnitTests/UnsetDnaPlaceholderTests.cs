using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests;

/// <summary>The shipped DNA templates hold "not configured" placeholders. They must mean
/// "disabled" and never stop a read-only provider from registering.</summary>
public class UnsetDnaPlaceholderTests
{
    [Fact]
    public void HederaTemplatePlaceholderMeansPaidQueriesDisabled()
    {
        var template = new HashgraphStorageQueryPreparationSettings
            { AuthorizedNodeAccountIds = null, MaxPaymentTinybars = 0, MaxTransactionFeeTinybars = 0,
              MaximumRequestsPerInvocation = 64, PersistenceTimeoutMs = 10000 };
        Assert.Null(HederaQueryPreparationConfiguration.Create(template));
        Assert.Null(HederaQueryPreparationConfiguration.Create(null));
    }

    [Fact]
    public void HederaPartialPolicyIsStillPassedThroughForRejection()
    {
        var partial = new HashgraphStorageQueryPreparationSettings
            { AuthorizedNodeAccountIds = new[] { "0.0.3" }, MaxPaymentTinybars = 0, MaxTransactionFeeTinybars = 0 };
        Assert.NotNull(HederaQueryPreparationConfiguration.Create(partial));
    }

    [Fact]
    public void CosmosEmptyFeeCeilingsRegisterWithoutLimits()
    {
        var settings = new CosmosBlockChainOASISProviderSettings
            { MaximumFeeUnitsByDenomination = new Dictionary<string, string>() };
        Assert.NotNull(CosmosProviderConfiguration.Create(settings));
    }

    [Fact]
    public void CosmosInvalidFeeCeilingIsStillRejected()
    {
        var settings = new CosmosBlockChainOASISProviderSettings
            { MaximumFeeUnitsByDenomination = new Dictionary<string, string> { ["uatom"] = "0" } };
        Assert.Throws<ArgumentException>(() => CosmosProviderConfiguration.Create(settings));
    }
}
