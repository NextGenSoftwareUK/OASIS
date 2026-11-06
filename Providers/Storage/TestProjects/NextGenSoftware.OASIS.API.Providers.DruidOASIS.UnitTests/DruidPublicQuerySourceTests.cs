using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS.UnitTests;

[TestClass]
public sealed class DruidPublicQuerySourceTests
{
    private static readonly Uri Origin = new("http://127.0.0.1:8888");
    private static readonly Guid Actor = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly Guid[] Ids = Enumerable.Range(1, 6)
        .Select(value => Guid.Parse($"10000000-0000-4000-8000-{value:D12}")).ToArray();

    [TestMethod]
    public async Task AdmittedKeyUsesAPublicParameterizedSqlLookup()
    {
        using var transport = new Reply(id => new[] { Doc(id) });
        using var source = new DruidPublicQuerySource(Origin, Ids[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Query(source, Ids[..3]), Ids[..3]);
        CollectionAssert.AreEqual(new[] { Ids[0], Ids[0], Ids[1], Ids[2] }, transport.Reads.ToArray());
    }

    [DataTestMethod]
    [DataRow("duplicate")]
    [DataRow("private")]
    [DataRow("wrong-id")]
    public async Task InvalidNativeRowsFailWithoutPartialResults(string kind)
    {
        using var transport = new Reply(id => id != Ids[1] ? new[] { Doc(id) } : kind switch
        {
            "duplicate" => new[] { Doc(id), Doc(id) },
            "private" => new[] { Doc(id, isPublic: false) },
            _ => new[] { Doc(Ids[5]) }
        });
        using var source = new DruidPublicQuerySource(Origin, Ids[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var failed = await Query(source, Ids[..3]);
        Assert.IsTrue(failed.IsError);
        Assert.IsNull(failed.Result);
        CollectionAssert.AreEqual(new[] { Ids[0], Ids[0], Ids[1] }, transport.Reads.ToArray());
    }

    [TestMethod, TestCategory("DruidPublicCqlNative")]
    public async Task RealDruidDatasourceProvidesThreeTwoAndZeroPublicRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_DRUID_VERIFY_PUBLIC") != "owned-linux-8888")
        {
            Assert.Inconclusive("Enable the explicitly owned Druid Linux fixture.");
            return;
        }

        using var source = new DruidPublicQuerySource(Origin, Ids[0]);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        Check(await Query(source, Ids), Ids[..3]);
        Check(await Query(source, Ids[..2]), Ids[..2]);
        Check(await Query(source, Ids[3..]), Array.Empty<Guid>());
    }

    private static string Doc(Guid id, bool isPublic = true) => StoredRecordJson.Serialize<IHolon>(
        new Holon { Id = id, Name = "Druid teaching record", IsPublic = isPublic, IsActive = true, CreatedByAvatarId = Actor });

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Query(DruidPublicQuerySource source, Guid[] ids)
    {
        const ProviderType provider = ProviderType.DruidOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Actor, provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == Actor));
    }

    private sealed class Reply(Func<Guid, string[]> records) : HttpMessageHandler
    {
        public List<Guid> Reads { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual(new Uri(Origin, "/druid/v2/sql"), request.RequestUri);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var root = json.RootElement;
            Assert.AreEqual("SELECT \"doc\" FROM \"oasis_cql_public_holons\" WHERE \"id\" = ? AND \"is_public\" = 1 LIMIT 2",
                root.GetProperty("query").GetString());
            var parameter = root.GetProperty("parameters").EnumerateArray().Single();
            Assert.AreEqual("VARCHAR", parameter.GetProperty("type").GetString());
            var id = parameter.GetProperty("value").GetGuid();
            Reads.Add(id);
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(records(id).Select(doc => new { doc })) };
        }
    }
}
