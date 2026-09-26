using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class OASISProviderFanOutHandlerTests
{
    [Fact]
    public async Task AppliesSameOperationToEveryExplicitTarget()
    {
        var first = new Target("first", true);
        var second = new Target("second", true);
        var item = NewItem();

        var result = await new OASISProviderFanOutHandler(new[] { first, second }).ApplyAsync(item, default);

        Assert.False(result.IsError, result.Message);
        Assert.Equal(item.OperationId, Assert.Single(first.Operations));
        Assert.Equal(item.OperationId, Assert.Single(second.Operations));
    }

    [Fact]
    public async Task PartialTargetFailureIsVisibleAndRetryable()
    {
        var successful = new Target("successful", true);
        var failed = new Target("failed", false);

        var result = await new OASISProviderFanOutHandler(new[] { successful, failed })
            .ApplyAsync(NewItem(), default);

        Assert.True(result.IsError);
        Assert.Equal("HOSTED_FANOUT_TARGETS_INCOMPLETE", result.ErrorCode);
        Assert.Contains("failed", result.Message);
    }

    [Fact]
    public void DuplicateTargetIdsAreRejectedAtComposition()
    {
        Assert.Throws<ArgumentException>(() => new OASISProviderFanOutHandler(new[]
        {
            new Target("same", true), new Target("same", true)
        }));
    }

    private static HostedSyncFanOutItem NewItem() => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
        EntityType = "holon", Kind = SyncOperationKind.Upsert,
        VersionId = Guid.NewGuid(), PayloadJson = "{}"
    };

    private sealed class Target : IHyperDriveIdempotentReplicationTarget
    {
        private readonly bool _success;
        public Target(string id, bool success) { ReplicationTargetId = id; _success = success; }
        public string ReplicationTargetId { get; }
        public List<Guid> Operations { get; } = new();
        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken)
        {
            Operations.Add(item.OperationId);
            return Task.FromResult(_success ? new OASISResult<bool>(true) : new OASISResult<bool>
            { IsError = true, ErrorCode = "TARGET_DOWN", Message = "down" });
        }
    }
}
