using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed partial class CosmicDataControllerHttpTests
{
    // A holon exactly as the API serialised it must save back unmodified (load, edit, save),
    // including after a client re-cases every key (as @oasisomniverse/web4-api 2.0.15 does),
    // and with no revision at all (last write wins).
    [Theory]
    [InlineData("asLoaded")]
    [InlineData("recasedKeys")]
    [InlineData("noRevision")]
    public async Task LoadedHolonSavesBackUnmodified(string shape)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            var revision = Guid.NewGuid();
            var stored = new Holon { Id = id, Name = "District A", ParentHolonId = Guid.NewGuid(), HolonType = HolonType.Custom,
                CreatedByAvatarId = actor, Version = 1, MetaData = new Dictionary<string, object> { ["piperName"] = "District A" } };
            CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
            provider.Setup(v => v.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
            Guid? expected = null;
            provider.As<IConditionalHolonStorageProvider>()
                .Setup(v => v.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => expected = request.Write.Conditions.Single().ExpectedRevisionId)
                .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code, Message = "stop after submission" });

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
              DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true));
            NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonJsonContracts.Configure(options);
            var holon = JsonNode.Parse(JsonSerializer.Serialize(stored, options))!.AsObject();
            holon["name"] = "District B";
            if (shape == "recasedKeys") holon = (JsonObject)Recase(holon);
            if (shape == "noRevision") holon.Remove("providerMetaData");

            var body = new JsonObject { ["holon"] = holon, ["saveChildren"] = false };
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/save-holon")
            { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
            request.Headers.Add("X-COSMIC-Operation-Id", Guid.NewGuid().ToString());
            using var response = await client.SendAsync(request);

            Assert.True(expected.HasValue, $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            Assert.Equal(revision, expected); // stored revision: claimed, or the last-write-wins fallback
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); // controlled provider conflict reached
        });
    }

    /// <summary>Lower-cases the first letter of every key, recursively (the npm client's normaliser).</summary>
    private static JsonNode? Recase(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Select(pair => KeyValuePair.Create(
            pair.Key.Length > 0 ? char.ToLowerInvariant(pair.Key[0]) + pair.Key[1..] : pair.Key, Recase(pair.Value)))),
        JsonArray array => new JsonArray(array.Select(Recase).ToArray()),
        _ => node?.DeepClone()
    };
}
