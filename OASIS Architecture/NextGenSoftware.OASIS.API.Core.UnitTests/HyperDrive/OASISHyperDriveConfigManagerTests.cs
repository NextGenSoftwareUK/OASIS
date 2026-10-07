using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class OASISHyperDriveConfigManagerTests
{
    [Fact]
    public void UpdateConfiguration_CommitsOnlyAfterPersistenceSucceeds()
    {
        var original = CreateConfiguration("original");
        var replacement = CreateConfiguration("replacement");
        OASISHyperDriveConfig? persisted = null;
        var manager = new OASISHyperDriveConfigManager(original, candidate =>
        {
            persisted = candidate;
            return Success();
        });

        OASISResult<bool> result = manager.UpdateConfiguration(replacement);

        Assert.False(result.IsError);
        Assert.True(result.Result);
        Assert.Same(replacement, persisted);
        Assert.Same(replacement, manager.GetConfiguration());
    }

    [Fact]
    public void UpdateConfiguration_PreservesCurrentConfigurationWhenPersistenceFails()
    {
        var original = CreateConfiguration("original");
        var manager = new OASISHyperDriveConfigManager(original, _ => Failure("DNA_WRITE_FAILED"));

        OASISResult<bool> result = manager.UpdateConfiguration(CreateConfiguration("replacement"));

        Assert.True(result.IsError);
        Assert.False(result.Result);
        Assert.Equal("DNA_WRITE_FAILED", result.ErrorCode);
        Assert.Same(original, manager.GetConfiguration());
    }

    [Fact]
    public void UpdateConfiguration_TreatsMissingPersistenceResultAsAnError()
    {
        var original = CreateConfiguration("original");
        var manager = new OASISHyperDriveConfigManager(original, _ => null!);

        OASISResult<bool> result = manager.UpdateConfiguration(CreateConfiguration("replacement"));

        Assert.True(result.IsError);
        Assert.Equal("HYPERDRIVE_CONFIG_SAVE_FAILED", result.ErrorCode);
        Assert.Same(original, manager.GetConfiguration());
    }

    [Fact]
    public void ResetToDefaults_PreservesCurrentConfigurationWhenPersistenceFails()
    {
        var original = CreateConfiguration("original");
        var manager = new OASISHyperDriveConfigManager(original, _ => Failure("DNA_WRITE_FAILED"));

        OASISResult<bool> result = manager.ResetToDefaults();

        Assert.True(result.IsError);
        Assert.Equal("DNA_WRITE_FAILED", result.ErrorCode);
        Assert.Same(original, manager.GetConfiguration());
    }

    [Fact]
    public void ResetToDefaults_CommitsPersistedDefaults()
    {
        var original = CreateConfiguration("original");
        OASISHyperDriveConfig? persisted = null;
        var manager = new OASISHyperDriveConfigManager(original, candidate =>
        {
            persisted = candidate;
            return Success();
        });

        OASISResult<bool> result = manager.ResetToDefaults();

        Assert.False(result.IsError);
        Assert.True(result.Result);
        Assert.NotNull(persisted);
        Assert.Same(persisted, manager.GetConfiguration());
        Assert.Equal("Auto", persisted!.DefaultStrategy);
    }

    [Fact]
    public void UpdateConfiguration_AppliesPersistedConfigurationToRuntime()
    {
        var replacement = CreateConfiguration("replacement");
        var runtimeApplied = false;
        var manager = new OASISHyperDriveConfigManager(CreateConfiguration("original"), _ => Success(), candidate =>
        {
            runtimeApplied = ReferenceEquals(candidate, replacement);
            return Success();
        });

        OASISResult<bool> result = manager.UpdateConfiguration(replacement);

        Assert.False(result.IsError, result.Message);
        Assert.True(runtimeApplied);
        Assert.Same(replacement, manager.GetConfiguration());
    }

    [Fact]
    public void UpdateConfiguration_RollsPersistenceBackWhenRuntimeRejectsPolicy()
    {
        var original = CreateConfiguration("original");
        var persisted = new List<OASISHyperDriveConfig>();
        var manager = new OASISHyperDriveConfigManager(original, candidate =>
        {
            persisted.Add(candidate);
            return Success();
        }, _ => Failure("RUNTIME_REJECTED"));

        OASISResult<bool> result = manager.UpdateConfiguration(CreateConfiguration("replacement"));

        Assert.True(result.IsError);
        Assert.Equal("RUNTIME_REJECTED", result.ErrorCode);
        Assert.Equal(2, persisted.Count);
        Assert.Same(original, persisted[1]);
        Assert.Same(original, manager.GetConfiguration());
    }

    [Fact]
    public void V2RuntimePolicy_IsTheSingleAuthorityForFlagsAndOrderedLists()
    {
        var manager = new ProviderManager(null, null);
        var configuration = CreateConfiguration("Performance");
        configuration.AutoFailoverEnabled = true;
        configuration.AutoReplicationEnabled = false;
        configuration.AutoLoadBalancingEnabled = true;
        configuration.AutoFailoverProviders = new List<string> { "IPFSOASIS", "MongoDBOASIS" };
        configuration.AutoReplicationProviders = new List<string>();
        configuration.LoadBalancingProviders = new List<string> { "MongoDBOASIS", "IPFSOASIS" };

        OASISResult<bool> result = manager.ApplyHyperDriveConfiguration(configuration);

        Assert.False(result.IsError, result.Message);
        Assert.True(manager.IsAutoFailOverEnabled);
        Assert.False(manager.IsAutoReplicationEnabled);
        Assert.True(manager.IsAutoLoadBalanceEnabled);
        Assert.Equal(new[] { ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS },
            manager.GetProviderAutoFailOverList().Select(x => x.Value));
        Assert.Equal(new[] { ProviderType.MongoDBOASIS, ProviderType.IPFSOASIS },
            manager.GetProviderAutoLoadBalanceList().Select(x => x.Value));
        Assert.Equal("OASIS.OASISHyperDriveConfig", manager.EffectiveHyperDriveConfigurationSource);
    }

    private static OASISHyperDriveConfig CreateConfiguration(string strategy) => new()
    {
        DefaultStrategy = strategy
    };

    private static OASISResult<bool> Success() => new()
    {
        Result = true
    };

    private static OASISResult<bool> Failure(string errorCode) => new()
    {
        IsError = true,
        ErrorCode = errorCode,
        Message = "Persistence failed."
    };
}
