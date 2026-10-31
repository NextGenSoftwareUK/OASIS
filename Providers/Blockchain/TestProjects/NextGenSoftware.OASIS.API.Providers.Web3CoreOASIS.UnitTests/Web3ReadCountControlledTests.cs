using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

// Real Nethereum ABI encoding/decoding, controlled IClient responses; NOT live-chain proof.
[TestClass, DoNotParallelize]
public class Web3ReadCountControlledTests
{
    [DataTestMethod]
    [DataRow("avatars", false)][DataRow("avatars", true)]
    [DataRow("details", false)][DataRow("details", true)]
    [DataRow("holons", false)][DataRow("holons", true)]
    [DataRow("parent", false)][DataRow("parent", true)]
    [DataRow("metadata", false)][DataRow("metadata", true)]
    [DataRow("export", false)][DataRow("export", true)]
    [DataRow("nearby", false)][DataRow("nearby", true)]
    [DataRow("search", false)][DataRow("search", true)]
    [DataRow("search-filter", false)][DataRow("search-filter", true)]
    [DataRow("avatar-point", false)][DataRow("avatar-point", true)]
    [DataRow("detail-point", false)][DataRow("detail-point", true)]
    [DataRow("holon-point", false)][DataRow("holon-point", true)]
    [DataRow("provider-root", false)][DataRow("provider-root", true)]
    [DataRow("username", false)][DataRow("username", true)]
    [DataRow("email", false)][DataRow("email", true)]
    public Task ReturnedRootsDetermineCount(string route, bool empty) => WithKey(async () =>
    {
        var fixture = new Fixture(empty);
        var provider = fixture.Provider;
        var count = empty ? 0 : route == "holons" ? 3 : route == "search" ? 4 : 1;
        switch (route)
        {
            case "avatars": Check(await provider.LoadAllAvatarsAsync(), count); break;
            case "details": Check(await provider.LoadAllAvatarDetailsAsync(), count); break;
            case "holons": Check(await provider.LoadAllHolonsAsync(HolonType.All, loadChildrenFromProvider: true), count); break;
            case "parent": Check(await fixture.Call<IEnumerable<IHolon>>("ReadPrivateParentAsync", fixture.Owner.ToString(), HolonType.All, true, true, 0, 0, true, 0), count); break;
            case "metadata": Check(await fixture.Call<IEnumerable<IHolon>>("ReadPrivateMetadataAsync", new Dictionary<string, string> { ["group"] = "owned" }, MetaKeyValuePairMatchMode.All, HolonType.All, true, true, 0, 0, true, 0), count); break;
            case "export": Check(await fixture.Call<IEnumerable<IHolon>>("ExportPrivateOwnedAsync", fixture.Owner, 0), count); break;
            case "nearby":
                var nearby = typeof(Web3CoreOASISBaseProvider).GetMethod("ReadNearbyAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IHolon));
                Func<Task<OASISResult<IEnumerable<IHolon>>>> load = () => provider.LoadAllHolonsAsync(HolonType.All);
                Check(await (Task<OASISResult<IEnumerable<IHolon>>>)nearby.Invoke(provider, new object[] { 0L, 0L, 10, load })!, count); break;
            case "search":
                var search = await provider.SearchAsync(new SearchParams { SearchOnlyForCurrentAvatar = false });
                Check(search, count); Assert.AreEqual(count, search.Result.NumberOfResults); break;
            case "search-filter":
                var filtered = await provider.SearchAsync(new SearchParams { SearchOnlyForCurrentAvatar = false, FilterByMetaData = new() { ["group"] = "owned" } });
                Check(filtered, count); Assert.AreEqual(count, filtered.Result.NumberOfResults); break;
            case "avatar-point": Check(await provider.LoadAvatarAsync(fixture.Owner), count, empty); break;
            case "detail-point": Check(await provider.LoadAvatarDetailAsync(fixture.Owner), count, empty); break;
            case "holon-point": Check(await provider.LoadHolonAsync(fixture.Root.Id), count, empty); break;
            case "provider-root":
                var root = await provider.LoadHolonAsync(fixture.Root.Id, loadChildrenFromProvider: true);
                Check(root, count, empty); if (!empty) Assert.AreEqual(1, root.Result.Children.Count()); break;
            case "username": Check(await provider.LoadAvatarByUsernameAsync("owned"), count, empty); break;
            case "email": Check(await provider.LoadAvatarByEmailAsync("owned@example.invalid"), count, empty); break;
            default: Assert.Fail(route); break;
        }
        Assert.IsTrue(fixture.Rpc.Calls > 0, "Must traverse the real SDK read boundary.");
    });

    [TestMethod]
    public Task ProjectionFailureDoesNotClaimLoadedRecords() => WithKey(async () =>
    {
        var fixture = new Fixture(false);
        // Navigation is deliberately not stored by the canonical serializer. Use a
        // persisted malformed role manifest, not an embedded-Children cycle fixture.
        fixture.Root.ChildRoleManifestJson = "not-json";
        fixture.ReplaceHolons(fixture.Root);
        var result = await fixture.Provider.LoadHolonAsync(fixture.Root.Id, loadChildrenFromProvider: true);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded);
        Assert.IsNull(result.Result); Assert.AreEqual(0, result.LoadedCount);
    });

    [DataTestMethod][DataRow("avatars")][DataRow("holons")][DataRow("search")]
    public Task TransportFailureDoesNotClaimLoadedRecords(string route) => WithKey(async () =>
    {
        var fixture = new Fixture(false); fixture.Rpc.Fail = true;
        if (route == "avatars") Check(await fixture.Provider.LoadAllAvatarsAsync(), 0, true);
        else if (route == "holons") Check(await fixture.Provider.LoadAllHolonsAsync(HolonType.All), 0, true);
        else Check(await fixture.Provider.SearchAsync(new SearchParams { SearchOnlyForCurrentAvatar = false }), 0, true);
    });

    [TestMethod]
    public Task AmbiguousIdentityDoesNotClaimLoadedRecords() => WithKey(async () =>
    {
        var fixture = new Fixture(false);
        fixture.Rpc.Records["avatar"].Add(fixture.Encode("avatar", new Avatar { Id = Guid.NewGuid(), IsActive = true, Username = "owned" }));
        Check(await fixture.Provider.LoadAvatarByUsernameAsync("owned"), 0, true);
    });

    [DataTestMethod][DataRow(false, false)][DataRow(false, true)][DataRow(true, false)][DataRow(true, true)]
    public Task SuccessfulEncryptedExportsUseIndependentVersions(bool synchronous, bool email) => WithKey(async () =>
    {
        var fixture = new Fixture(false);
        fixture.Rpc.Records["avatar"] = new() { fixture.Encode("avatar", new Avatar { Id = fixture.Owner, Version = 2, IsActive = true, Username = "owned", Email = "owned@example.invalid" }) };
        fixture.Root.Version = 3;
        var child = new Holon { Id = Guid.NewGuid(), Version = 1, IsActive = true, ParentHolonId = fixture.Root.Id };
        fixture.ReplaceHolons(fixture.Root, child, new Holon { Id = Guid.NewGuid(), Version = 3, IsActive = true });
        var provider = fixture.Provider;
        var result = email
            ? synchronous ? provider.ExportAllDataForAvatarByEmail("owned@example.invalid", 3) : await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid", 3)
            : synchronous ? provider.ExportAllDataForAvatarByUsername("owned", 3) : await provider.ExportAllDataForAvatarByUsernameAsync("owned", 3);
        Assert.IsFalse(result.IsError, result.Message);
        var root = result.Result.Single(); Assert.AreEqual(fixture.Root.Id, root.Id); Assert.AreEqual(3, root.Version);
        Assert.AreEqual(child.Id, root.Children.Single().Id); Assert.AreEqual(1, root.Children.Single().Version);
        Check(result, 1);
    });

    private static void Check<T>(OASISResult<T> result, int count, bool error = false)
    {
        Assert.AreEqual(error, result.IsError, result.Message);
        Assert.AreEqual(!error, result.IsLoaded, result.Message);
        Assert.AreEqual(count, result.LoadedCount, "Count returned roots, not candidates, descendants or RPC calls.");
    }

    private static async Task WithKey(Func<Task> operation)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        var key = RandomNumberGenerator.GetBytes(32);
        try { Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY", Convert.ToBase64String(key)); await operation(); }
        finally { Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY", previous); CryptographicOperations.ZeroMemory(key); }
    }

    private sealed class Fixture
    {
        public readonly Guid Owner = Guid.NewGuid();
        public readonly Holon Root;
        public readonly Web3CoreOASISBaseProvider Provider;
        public readonly ReadRpc Rpc;
        public Fixture(bool empty)
        {
            const string address = "0x0000000000000000000000000000000000000001";
            Provider = new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1", "0x" + new string('1', 64), address);
            var client = DispatchProxy.Create<IClient, ReadRpc>(); Rpc = (ReadRpc)client;
            var web3 = new Web3(client);
            var sdk = new Web3CoreOASIS("0x" + new string('1', 64), "http://127.0.0.1:1", address, Web3CoreOASISBaseProviderHelper.Abi);
            typeof(Web3CoreOASIS).GetField("_web3", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sdk, web3);
            typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Provider, sdk);
            var contract = web3.Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi, address);
            foreach (var (kind, page, point) in new[] { ("avatar", "getAvatarsPage", "lookupAvatar"), ("detail", "getAvatarDetailsPage", "lookupAvatarDetail"), ("holon", "getHolonsPage", "lookupHolon") })
            {
                Rpc.Selectors.Add(contract.GetFunction(page).GetData(BigInteger.Zero, 128).Substring(0, 10), (kind, true));
                Rpc.Selectors.Add(contract.GetFunction(point).GetData(0).Substring(0, 10), (kind, false));
                Rpc.Records[kind] = new();
            }
            Root = new Holon { Id = Guid.NewGuid(), IsActive = true, CreatedByAvatarId = Owner, ParentHolonId = Owner, MetaData = new() { ["group"] = "owned", ["Latitude"] = 0d, ["Longitude"] = 0d } };
            if (empty) return;
            Rpc.Records["avatar"].Add(Encode("avatar", new Avatar { Id = Owner, IsActive = true, Username = "owned", Email = "owned@example.invalid" }));
            Rpc.Records["detail"].Add(Encode("detail", new AvatarDetail { Id = Owner, IsActive = true }));
            ReplaceHolons(Root, new Holon { Id = Guid.NewGuid(), IsActive = true, ParentHolonId = Root.Id }, new Holon { Id = Guid.NewGuid(), IsActive = true });
        }
        public void ReplaceHolons(params IHolon[] rows) => Rpc.Records["holon"] = rows.Select(row => Encode("holon", row)).ToList();
        public EntityOASIS Encode(string kind, IHolonBase row) => new() { EntityId = (uint)HashUtility.GetNumericHash(row.Id.ToString()), ExternalId = Web3CoreOASISBaseProvider.PrivateExternalId(row.Id), Info = Encoding.UTF8.GetBytes(Provider.EncodePrivateRecord(kind, row)) };
        public Task<OASISResult<T>> Call<T>(string name, params object[] args) => (Task<OASISResult<T>>)typeof(Web3CoreOASISBaseProvider).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Provider, args)!;
    }

    public class ReadRpc : DispatchProxy
    {
        public int Calls;
        public bool Fail;
        public readonly Dictionary<string, (string Kind, bool Page)> Selectors = new();
        public readonly Dictionary<string, List<EntityOASIS>> Records = new();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name.StartsWith("get_") || method.Name.StartsWith("set_")) return null;
            if (method.Name != "SendRequestAsync") throw new InvalidOperationException(method.Name);
            var request = (RpcRequest)args![0]!; Calls++; string response;
            if (Fail) throw new IOException("Controlled read transport failure; no external request attempted.");
            if (request.Method == "eth_blockNumber") response = "0x7";
            else if (request.Method == "eth_call")
            {
                var data = JObject.FromObject(request.RawParameters[0])["data"]!.Value<string>()!;
                var (kind, page) = Selectors[data.Substring(0, 10)];
                object output;
                if (page) output = new EntityOASISPageOutput { Entities = Records[kind] };
                else
                {
                    var id = BigInteger.Parse("0" + data.Substring(10), NumberStyles.AllowHexSpecifier);
                    output = new EntityOASISOutput { Entity = Records[kind].SingleOrDefault(row => row.EntityId == id) ?? new EntityOASIS { ExternalId = new byte[32] } };
                }
                response = "0x" + Convert.ToHexString(new ParametersEncoder().EncodeParametersFromTypeAttributes(output.GetType(), output)).ToLowerInvariant();
            }
            else throw new InvalidOperationException("Unexpected RPC: " + request.Method);
            var type = method.ReturnType.GetGenericArguments()[0];
            var decoded = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(response), type);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(type).Invoke(null, new[] { decoded });
        }
    }
}
