using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainIndexedReadAttemptTests
{
    private readonly CosmicChainQuerySource source = new(ProviderType.SolanaOASIS, "network", ProviderType.SQLLiteDBOASIS,
        Guid.NewGuid(), 2, new string('A', 64), 0);
    private sealed class Owner : ICosmicIndexedReadAttemptOwner
    {
        public readonly List<CosmicIndexedReadOperation> Begun = new();
        public readonly List<CosmicIndexedReadOperation> Completed = new();
        public Func<Task>? BeforeInvoke;
        public async Task<T> ExecuteAsync<T>(CosmicChainQuerySource source, CosmicIndexedReadOperation operation,
            Func<Task<T>> invoke, CancellationToken cancellationToken)
        {
            Begun.Add(operation);
            if (BeforeInvoke != null) await BeforeInvoke();
            var result = await invoke();
            Completed.Add(operation);
            return result;
        }
    }

    [Fact]
    public async Task EachActualTransportAndSqlSnapshotGetsItsOwnBoundAttempt()
    {
        var owner = new Owner(); var calls = 0;
        await CosmicIndexedReadAttempts.RunAsync(source, owner, async () => {
            await CosmicIndexedReadAttempts.ExecuteIndexSnapshotAsync(source, () => Task.FromResult(++calls));
            await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider,
                CosmicIndexedReadOperation.SourceGenesis, () => Task.FromResult(++calls));
            // A root and two chunks are three transport reads, not one observer receipt.
            for (var i = 0; i < 3; i++)
                await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider,
                    CosmicIndexedReadOperation.SourceTransaction, () => Task.FromResult(++calls));
            return true;
        });
        Assert.Equal(5, calls);
        Assert.Equal(owner.Begun, owner.Completed);
        Assert.Equal(3, owner.Begun.Count(x => x == CosmicIndexedReadOperation.SourceTransaction));
    }

    [Theory]
    [InlineData("provider")] [InlineData("index")]
    public async Task WrongBindingCannotBeginAnAttempt(string mode)
    {
        var owner = new Owner();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CosmicIndexedReadAttempts.RunAsync(source, owner, async () => {
            if (mode == "provider") return await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.ExecutionProvider,
                CosmicIndexedReadOperation.SourceGenesis, () => Task.FromResult(1));
            var other = new CosmicChainQuerySource(source.SourceProvider, source.NetworkIdentity, source.ExecutionProvider,
                Guid.NewGuid(), 2, source.CatalogueHash, 0);
            return await CosmicIndexedReadAttempts.ExecuteIndexSnapshotAsync(other, () => Task.FromResult(1));
        }));
        Assert.Empty(owner.Begun);
    }

    [Fact]
    public async Task CancellationDuringDurableBeginStopsTransportAndDoesNotInventCompletion()
    {
        using var cancellation = new CancellationTokenSource(); var called = false;
        var owner = new Owner { BeforeInvoke = () => { cancellation.Cancel(); return Task.CompletedTask; } };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CosmicIndexedReadAttempts.RunAsync(source, owner,
            () => CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider, CosmicIndexedReadOperation.SourceBlock,
                () => { called = true; return Task.FromResult(1); }, cancellation.Token)));
        Assert.False(called); Assert.Single(owner.Begun); Assert.Empty(owner.Completed);
    }

    [Fact]
    public async Task ExpiredDetachedScopeAndNestedReplacementCannotPerformIo()
    {
        var owner = new Owner(); var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> detached = null!; var called = false;
        await CosmicIndexedReadAttempts.RunAsync(source, owner, async () => {
            await Assert.ThrowsAsync<InvalidOperationException>(() => CosmicIndexedReadAttempts.RunAsync(source, owner, () => Task.FromResult(1)));
            detached = Task.Run(async () => {
                await release.Task;
                return await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider,
                    CosmicIndexedReadOperation.SourceSlot, () => { called = true; return Task.FromResult(1); });
            });
            return true;
        });
        release.SetResult(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => detached);
        Assert.False(called); Assert.Empty(owner.Begun);
    }

    [Fact]
    public async Task TransportFailurePreservesBegunAttemptWithoutInventedAcknowledgement()
    {
        var owner = new Owner();
        await Assert.ThrowsAsync<IOException>(() => CosmicIndexedReadAttempts.RunAsync(source, owner,
            () => CosmicIndexedReadAttempts.ExecuteSourceRpcAsync<int>(source.SourceProvider,
                CosmicIndexedReadOperation.SourceTransaction, () => throw new IOException("unknown transport outcome"))));
        Assert.Single(owner.Begun); Assert.Empty(owner.Completed);
    }
}
