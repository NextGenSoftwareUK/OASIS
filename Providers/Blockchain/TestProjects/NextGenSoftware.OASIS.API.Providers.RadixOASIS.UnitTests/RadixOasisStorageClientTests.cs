using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.UnitTests
{
    /// <summary>
    /// RadixOasisStorageClient against a fake Radix Gateway (no network): Gateway response parsing, the two-step
    /// store lookup, index lookups and write preconditions.
    /// </summary>
    [TestClass]
    public class RadixOasisStorageClientTests
    {
        private const string Component = "component_tdx_2_1cqtest";
        private static readonly Guid AvatarId = Guid.Parse("3f2c6a8e-1d4b-4c1a-9e7f-2b5d8c0a1e93");

        /// <summary>A /state/entity/details response for an OasisStorage component (programmatic SBOR state).</summary>
        private static string EntityDetails() => new JsonObject
        {
            ["items"] = new JsonArray(new JsonObject
            {
                ["address"] = Component,
                ["details"] = new JsonObject
                {
                    ["type"] = "Component",
                    ["state"] = new JsonObject
                    {
                        ["kind"] = "Tuple",
                        ["fields"] = new JsonArray(
                            Own("avatars", "internal_keyvaluestore_tdx_2_avatars"),
                            Own("avatar_details", "internal_keyvaluestore_tdx_2_details"),
                            Own("holons", "internal_keyvaluestore_tdx_2_holons"),
                            Own("username_to_avatar_id", "internal_keyvaluestore_tdx_2_usernames"),
                            Own("email_to_avatar_id", "internal_keyvaluestore_tdx_2_emails"),
                            Own("provider_key_to_holon_id", "internal_keyvaluestore_tdx_2_pkeys"),
                            new JsonObject { ["kind"] = "U64", ["field_name"] = "avatar_count", ["value"] = "1" })
                    }
                }
            })
        }.ToJsonString();

        private static JsonObject Own(string name, string address) =>
            new JsonObject { ["kind"] = "Own", ["type_name"] = "KeyValueStore", ["field_name"] = name, ["value"] = address };

        private static string KvsData(string? value) => new JsonObject
        {
            ["entries"] = value == null
                ? new JsonArray()
                : new JsonArray(new JsonObject
                {
                    ["value"] = new JsonObject { ["programmatic_json"] = new JsonObject { ["kind"] = "String", ["value"] = value } }
                })
        }.ToJsonString();

        private sealed class FakeGateway : HttpMessageHandler
        {
            public readonly Dictionary<string, Dictionary<string, string>> Stores = new();
            public readonly List<string> Calls = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var path = request.RequestUri!.AbsolutePath;
                Calls.Add(path);
                var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(token))!;
                string json = path switch
                {
                    "/state/entity/details" => EntityDetails(),
                    "/state/key-value-store/data" => KvsData(Lookup((string)body["key_value_store_address"]!,
                        (string)body["keys"]![0]!["key_json"]!["value"]!)),
                    _ => throw new InvalidOperationException("unexpected " + path)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            private string? Lookup(string store, string key) =>
                Stores.TryGetValue(store, out var s) && s.TryGetValue(key, out var v) ? v : null;
        }

        private static (RadixOasisStorageClient Client, FakeGateway Gateway) Create(RadixOASISConfig? config = null)
        {
            var gateway = new FakeGateway();
            gateway.Stores["internal_keyvaluestore_tdx_2_avatars"] = new() { [AvatarId.ToString()] = "{\"Username\":\"david\"}" };
            gateway.Stores["internal_keyvaluestore_tdx_2_usernames"] = new() { ["david"] = AvatarId.ToString() };
            var client = new RadixOasisStorageClient(config ?? new RadixOASISConfig
            {
                HostUri = "https://stokenet.example",
                NetworkId = 2,
                OasisBlueprintAddress = Component
            }, new HttpClient(gateway));
            return (client, gateway);
        }

        [TestMethod]
        public void ParseOwnedStores_ReadsKeyValueStoreAddressesFromComponentState()
        {
            var stores = RadixOasisStorageClient.ParseOwnedStores(JsonNode.Parse(EntityDetails())).ToDictionary(s => s.Name, s => s.Address);

            Assert.AreEqual(6, stores.Count);
            Assert.AreEqual("internal_keyvaluestore_tdx_2_avatars", stores["avatars"]);
            Assert.IsFalse(stores.ContainsKey("avatar_count"));
        }

        [TestMethod]
        public void ParseKeyValueStoreString_ReturnsTheValueOrNull()
        {
            Assert.AreEqual("{}", RadixOasisStorageClient.ParseKeyValueStoreString(JsonNode.Parse(KvsData("{}"))));
            Assert.IsNull(RadixOasisStorageClient.ParseKeyValueStoreString(JsonNode.Parse(KvsData(null))));
        }

        [TestMethod]
        public async Task GetAvatarJsonAsync_LooksUpTheAvatarsStoreByOasisId()
        {
            var (client, _) = Create();

            var result = await client.GetAvatarJsonAsync(AvatarId);

            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual("{\"Username\":\"david\"}", result.Result);
        }

        [TestMethod]
        public async Task GetAvatarJsonByUsernameAsync_FollowsTheIndexThenLoadsTheAvatar()
        {
            var (client, gateway) = Create();

            var result = await client.GetAvatarJsonByUsernameAsync("david");

            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual("{\"Username\":\"david\"}", result.Result);
            // Store addresses are fetched once and cached.
            Assert.AreEqual(1, gateway.Calls.Count(c => c == "/state/entity/details"));
        }

        [TestMethod]
        public async Task MissingEntries_AreNullNotErrors()
        {
            var (client, _) = Create();

            Assert.IsNull((await client.GetAvatarJsonByUsernameAsync("nobody")).Result);
            Assert.IsNull((await client.GetHolonJsonAsync(Guid.NewGuid())).Result);
        }

        [TestMethod]
        public async Task Writes_WithoutBadgeOrKeys_FailWithoutCallingTheGateway()
        {
            var (client, gateway) = Create();

            Assert.IsFalse(client.CanWrite);
            var result = await client.SaveAvatarAsync(AvatarId, "{}", "david", "d@example.com");

            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "OasisStorageOwnerBadgeAddress");
            Assert.AreEqual(0, gateway.Calls.Count);
        }

        [TestMethod]
        public void Key_IsTheLowerCaseDFormatGuid()
        {
            Assert.AreEqual("3f2c6a8e-1d4b-4c1a-9e7f-2b5d8c0a1e93", RadixOasisStorageClient.Key(AvatarId));
        }
    }
}
