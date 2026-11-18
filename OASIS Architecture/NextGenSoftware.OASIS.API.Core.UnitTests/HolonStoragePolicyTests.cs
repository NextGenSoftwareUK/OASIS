using System;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class HolonStoragePolicyTests
{
    [Fact]
    public void EachModeHasExplicitPersistenceAndNativeReadSemantics()
    {
        var full = new HolonStoragePolicy(HolonStorageMode.Materialized, HolonReadCacheMode.None, 0, 0, true);
        var mapped = new HolonStoragePolicy(HolonStorageMode.Virtual, HolonReadCacheMode.None, 0, 0, false);
        var hybrid = new HolonStoragePolicy(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, 1048576, 60, true);
        Assert.False(full.ResolvesNativeRows);
        Assert.True(full.MaterializeOnExplicitSave);
        Assert.True(mapped.ResolvesNativeRows);
        Assert.False(mapped.MaterializeOnExplicitSave);
        Assert.True(hybrid.ResolvesNativeRows);
        Assert.Equal(1048576, hybrid.MaximumCacheBytes);
    }

    [Fact]
    public void PolicySelectionCannotCrossOwnersOrInventAnApplicationScope()
    {
        var owner = Guid.NewGuid();
        var avatar = new HolonStoragePolicy(HolonStorageMode.Virtual, HolonReadCacheMode.None, 0, 0, false);
        var app = new HolonStoragePolicy(HolonStorageMode.Hybrid, HolonReadCacheMode.None, 0, 0, true);
        var scope = new HolonStoragePolicyScope(owner, Guid.NewGuid());
        Assert.Same(app, scope.Resolve(owner, avatar, app));
        Assert.Same(avatar, scope.Resolve(owner, avatar, null));
        Assert.Throws<UnauthorizedAccessException>(() => scope.Resolve(Guid.NewGuid(), avatar, app));
        Assert.Throws<UnauthorizedAccessException>(() => scope.Resolve(Guid.Empty, avatar, app));
        Assert.Throws<ArgumentException>(() => new HolonStoragePolicyScope(owner).Resolve(owner, avatar, app));
        Assert.Throws<InvalidOperationException>(() => scope.Resolve(owner, null, null));
        Assert.Throws<ArgumentException>(() => new HolonStoragePolicyScope(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new HolonStoragePolicyScope(owner, Guid.Empty));
    }

    [Theory]
    [InlineData(HolonStorageMode.Virtual, HolonReadCacheMode.None, 0, 0, true)]
    [InlineData(HolonStorageMode.Materialized, HolonReadCacheMode.None, 0, 0, false)]
    [InlineData(HolonStorageMode.Virtual, HolonReadCacheMode.Memory, 1, 1, false)]
    [InlineData(HolonStorageMode.Materialized, HolonReadCacheMode.Memory, 1, 1, true)]
    [InlineData(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, 0, 1, true)]
    [InlineData(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, 1, 0, true)]
    [InlineData(HolonStorageMode.Hybrid, HolonReadCacheMode.None, 1, 0, true)]
    [InlineData(HolonStorageMode.Hybrid, HolonReadCacheMode.None, 0, 1, true)]
    [InlineData((HolonStorageMode)99, HolonReadCacheMode.None, 0, 0, false)]
    [InlineData(HolonStorageMode.Hybrid, (HolonReadCacheMode)99, 0, 0, false)]
    public void InvalidCombinationsAreRejected(HolonStorageMode mode, HolonReadCacheMode cache,
        long bytes, int seconds, bool materialize)
        => Assert.Throws<ArgumentException>(() => new HolonStoragePolicy(mode, cache, bytes, seconds, materialize));

    [Theory]
    [InlineData(-1L, 1)]
    [InlineData(1073741825L, 1)]
    [InlineData(1L, -1)]
    [InlineData(1L, 86401)]
    public void CacheBudgetsCannotBeUnbounded(long bytes, int seconds)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HolonStoragePolicy(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, bytes, seconds, true));
}
