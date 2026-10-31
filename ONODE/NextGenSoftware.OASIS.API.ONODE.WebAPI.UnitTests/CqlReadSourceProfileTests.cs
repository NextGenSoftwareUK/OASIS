using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlReadSourceProfileTests
{
    public interface ReadSource : IOASISStorageProvider, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation { }

    public class Source : DispatchProxy
    {
        public bool Active, Fail;
        public int Activations;
        protected override object? Invoke(MethodInfo? method, object?[]? arguments)
        {
            switch (method!.Name)
            {
                case "get_ProviderType": return new EnumValue<ProviderType>(ProviderType.SolanaOASIS);
                case "get_QueryProviderType": return ProviderType.SolanaOASIS;
                case "get_QueryCapabilities": return CosmicQueryCapabilities.ForStringMetadataKeyLookup("SolanaAccountAddress", 44, 8);
                case "get_IsProviderActivated": return Active;
                case "ActivateQuerySourceAsync":
                    ((CancellationToken)arguments![0]!).ThrowIfCancellationRequested();
                    Activations++;
                    Active = !Fail;
                    return Task.FromResult(new OASISResult<bool>(Active) { IsError = Fail });
                default: throw new InvalidOperationException("No canonical storage, signing or write operation is allowed.");
            }
        }
    }

    [Fact]
    public async Task ExplicitProfileOwnsResolutionAndActivationWithoutCanonicalRegistration()
    {
        var source = DispatchProxy.Create<ReadSource, Source>();
        var state = (Source)(object)source;
        using var profile = new CqlReadSourceProfile(source);
        var registry = new CqlHostedSourceRegistry(new[] { profile });
        var resolver = new CqlV1Providers(registry);

        Assert.Same(source, registry.Find(ProviderType.SolanaOASIS));
        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(ProviderType.SolanaOASIS));
        await registry.ActivateReadSourceAsync(ProviderType.SolanaOASIS, default);
        Assert.Same(source, resolver.Resolve(ProviderType.SolanaOASIS));
        Assert.Equal(1, state.Activations);

        state.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => registry.ActivateReadSourceAsync(ProviderType.SolanaOASIS, default));
        Assert.Same(source, registry.Find(ProviderType.SolanaOASIS));
        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(ProviderType.SolanaOASIS));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => registry.ActivateReadSourceAsync(ProviderType.SolanaOASIS, cancelled.Token));
        Assert.Equal(2, state.Activations);
    }

    [Fact]
    public void DuplicateRepresentationsAndMissingReadContractFailBeforeActivation()
    {
        var source = DispatchProxy.Create<ReadSource, Source>();
        using var profile = new CqlReadSourceProfile(source);
        Assert.Throws<ArgumentException>(() => new CqlHostedSourceRegistry(new[] { profile, profile }));
        Assert.Throws<ArgumentException>(() => new CqlReadSourceProfile(null!));
        Assert.Equal(0, ((Source)(object)source).Activations);
    }
}
