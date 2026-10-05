using NextGenSoftware.OASIS.API.Core.Configuration;
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
