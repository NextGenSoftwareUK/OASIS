using System.Collections;
using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public Task NftListsReturnEmptyCollectionsAndPreserveMetadataSelectionOnNativeMongo() =>
        RunSubscriptionHttp(false, usage: true, nftLists: true);

    [Fact]
    public Task NftReadsRejectNullAndMissingPayloadsWithoutPartialResultsOnNativeMongo() =>
        RunSubscriptionHttp(false, usage: true, nftLists: true, nftNullPayloads: true);

    private static async Task VerifyNftListsNativeAsync(IOASISStorageProvider provider, string[] accounts, bool nullPayloads = false)
    {
        var routing = Routing([provider], false, NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy);
        // The shipping ONODE constructor consumes the application singleton registry.
        // Supply the actual isolated registry as startup would, and restore it even on assertion failure.
        var registryField = typeof(ProviderManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.NotNull(registryField);
        var previousRegistry = registryField.GetValue(null);
        try
        {
            registryField.SetValue(null, routing);
            Assert.Same(routing, ProviderManager.Instance);
            await VerifyNftListsWithOwnedRegistryAsync(provider, accounts, routing, nullPayloads);
        }
        finally
        {
            registryField.SetValue(null, previousRegistry);
            Assert.Same(previousRegistry, registryField.GetValue(null));
            Console.WriteLine("Owned NFT application provider registry restored.");
        }
    }

    private static async Task VerifyNftListsWithOwnedRegistryAsync(IOASISStorageProvider provider, string[] accounts,
        ProviderManager routing, bool nullPayloads)
    {
        var actor = Guid.Parse(accounts[0]);
        var other = Guid.Parse(accounts[1]);
        var managerType = Assembly.Load("NextGenSoftware.OASIS.API.ONODE.Core")
            .GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager", true)!;
        var manager = Activator.CreateInstance(managerType, [provider, actor, routing.OASISDNA])!;
        managerType.GetProperty("Data")!.SetValue(manager, new HolonManager(provider, routing.OASISDNA, routing));
        var kinds = new[] {
            (Type: HolonType.Web3NFT, Method: "LoadAllWeb3NFTsForAvatarAsync", Owner: "NFT.MintedByAvatarId", Payload: "NFT.WEB3NFT"),
            (Type: HolonType.Web4NFT, Method: "LoadAllWeb4NFTsForAvatarAsync", Owner: "NFT.MintedByAvatarId", Payload: "NFT.WEB4NFT"),
            (Type: HolonType.Web4GeoNFT, Method: "LoadAllWeb4GeoNFTsForAvatarAsync", Owner: "GEONFT.PlacedByAvatarId", Payload: "GEONFT.WEB4GEONFT")
        };
        async Task<object> Load(string method, Guid owner)
        {
            object[] args = method == "LoadAllWeb3NFTsForAvatarAsync"
                ? [owner, Guid.Empty, ProviderType.MongoDBOASIS] : [owner, ProviderType.MongoDBOASIS];
            var task = (Task)managerType.GetMethod(method)!.Invoke(manager, args)!;
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        static Guid[] Ids(object result)
        {
            Assert.False((bool)result.GetType().GetProperty("IsError")!.GetValue(result)!,
                result.GetType().GetProperty("Message")!.GetValue(result)?.ToString());
            var values = result.GetType().GetProperty("Result")!.GetValue(result);
            Assert.NotNull(values); // Successful emptiness must be [], not unavailable/null.
            return ((IEnumerable)values).Cast<object>()
                .Select(item => (Guid)item.GetType().GetProperty("Id")!.GetValue(item)!).Order().ToArray();
        }
        async Task<Guid> Store(HolonType type, string ownerKey, Guid owner, string payloadKey, string? payload = null, bool includePayload = true)
        {
            var id = Guid.NewGuid();
            var metadata = new Dictionary<string, object> { [ownerKey] = owner.ToString("D") };
            if (includePayload) metadata[payloadKey] = payload ?? JsonSerializer.Serialize(new { Id = id, Title = "Owned native NFT" });
            var saved = await provider.SaveHolonAsync(new Holon {
                Id = id, Name = "Owned NFT list record", HolonType = type, IsActive = true, IsPublic = true,
                CreatedByAvatarId = other, CreatedDate = DateTime.UtcNow, Version = 1, VersionId = Guid.NewGuid(),
                MetaData = metadata
            }, saveChildren: false);
            Assert.False(saved.IsError, saved.Message);
            Assert.True(saved.IsSaved);
            return id;
        }
        if (nullPayloads)
        {
            static void RequireError(object result)
            {
                Assert.True((bool)result.GetType().GetProperty("IsError")!.GetValue(result)!);
                Assert.Null(result.GetType().GetProperty("Result")!.GetValue(result));
            }
            foreach (var kind in kinds)
            {
                await Store(kind.Type, kind.Owner, actor, kind.Payload);
                var nullId = await Store(kind.Type, kind.Owner, actor, kind.Payload, "null");
                RequireError(await Load(kind.Method, actor));
                var missingOwner = Guid.NewGuid();
                var missingId = await Store(kind.Type, kind.Owner, missingOwner, kind.Payload, includePayload: false);
                RequireError(await Load(kind.Method, missingOwner));
                var singleMethod = kind.Type == HolonType.Web3NFT ? "LoadWeb3NftAsync"
                    : kind.Type == HolonType.Web4NFT ? "LoadWeb4NftAsync" : "LoadWeb4GeoNftAsync";
                foreach (var id in new[] { nullId, missingId })
                {
                    var task = (Task)managerType.GetMethod(singleMethod, [typeof(Guid), typeof(ProviderType)])!
                        .Invoke(manager, [id, ProviderType.MongoDBOASIS])!;
                    await task;
                    RequireError(task.GetType().GetProperty("Result")!.GetValue(task)!);
                }
            }
            Console.WriteLine("Native NFT null payloads verified: Web3/Web4/GeoNFT JSON-null and missing stored metadata refuse single and list reads; valid sibling never becomes a partial success. Actual manager/HolonManager and Mongo, nine owned records and twelve error reads. No mocked response, unchanged empty-list case rerun, browser, chain, hosted or full portal acceptance.");
            return;
        }
        foreach (var kind in kinds)
        {
            Assert.Empty(Ids(await Load(kind.Method, actor)));
            var first = await Store(kind.Type, kind.Owner, actor, kind.Payload);
            var second = await Store(kind.Type, kind.Owner, actor, kind.Payload);
            await Store(kind.Type, kind.Owner, other, kind.Payload);
            await Store(HolonType.Holon, kind.Owner, actor, kind.Payload);
            Assert.Equal(new[] { first, second }.Order().ToArray(), Ids(await Load(kind.Method, actor)));
            Assert.Empty(Ids(await Load(kind.Method, Guid.NewGuid())));
        }
        await Store(HolonType.Web4NFT, "NFT.MintedByAvatarId", actor, "NFT.WEB4NFT", "{invalid-json");
        var malformed = await Load("LoadAllWeb4NFTsForAvatarAsync", actor);
        Assert.True((bool)malformed.GetType().GetProperty("IsError")!.GetValue(malformed)!);
        Assert.Null(malformed.GetType().GetProperty("Result")!.GetValue(malformed));
        Console.WriteLine("Native NFT lists verified: Web3/Web4/GeoNFT successful empty collections, exact two metadata-selected records despite different creator, foreign owner and wrong-type exclusions, fresh empty owner and malformed payload error retained. Shipping NFT manager/HolonManager and real Mongo provider; thirteen owned records. No count-query, browser, chain, hosted or full portal acceptance.");
    }
}
