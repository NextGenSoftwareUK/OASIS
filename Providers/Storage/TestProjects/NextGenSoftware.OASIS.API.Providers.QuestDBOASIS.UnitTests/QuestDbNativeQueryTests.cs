using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests;

[TestClass]
public class QuestDbNativeQueryTests
{
    private static CosmicQueryPlan<Holon> Plan(QuestDBOASIS provider, Guid actor, Guid[] ids, bool paging = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.QuestDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.QuestDBOASIS, ids).Result).Result;
        if (paging) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.QuestDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.QuestDBOASIS, provider.QueryCapabilities) }).Result;
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("paging")]
    [DataRow("foreign")]
    [DataRow("payload")]
    [DataRow("duplicate")]
    [DataRow("timestamp")]
    [DataRow("projection")]
    [DataRow("null")]
    [DataRow("transport")]
    [DataRow("open")]
    [DataRow("docsize")]
    [DataRow("unicode")]
    [DataRow("split")]
    [DataRow("aggregate")]
    [DataRow("empty")]
    [DataRow("budget")]
    [DataRow("deleted")]
    [DataRow("overflow")]
    [DataRow("cancel")]
    public async Task ParameterizedNativeQueryChecksIdentityEpochAndStreamBudgets(string mode)
    {
        var actor = Guid.NewGuid();
        var ids = Enumerable.Range(0, mode == "aggregate" ? 5 : 2).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var calls = 0;
        using var cancellation = new CancellationTokenSource();
        var connection = new QuestDbSaveOwnershipTests.Connection { FailOpen = mode == "open", Read = (command, token) =>
        {
            calls++;
            Assert.IsTrue(token.CanBeCanceled);
            Assert.AreEqual(15, command.CommandTimeout);
            Assert.AreEqual("SELECT id, cast(ts AS LONG) AS cosmic_ts, data FROM oasis_holons WHERE id IN (" +
                string.Join(",", ids.Select((_, index) => "@id" + index)) + ") LIMIT " + (ids.Length + 1) + ";", command.CommandText);
            Assert.AreEqual(ids.Length, command.Parameters.Count);
            for (var index = 0; index < ids.Length; index++)
                Assert.AreEqual(ids[index].ToString("D"), command.Parameters["id" + index].Value);
            if (mode == "transport") throw new IOException("Native read failed");
            if (mode == "cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            var table = new DataTable();
            table.Columns.Add("id", typeof(string));
            table.Columns.Add("cosmic_ts", typeof(long));
            table.Columns.Add(mode == "projection" ? "other" : "data", typeof(string));
            var nativeIds = mode == "empty" ? System.Array.Empty<Guid>() : mode == "overflow" ? ids.Append(Guid.NewGuid()).ToArray() : ids.Reverse().ToArray();
            for (var index = 0; index < nativeIds.Length; index++)
            {
                var id = mode == "duplicate" ? ids[0] : mode == "foreign" ? Guid.NewGuid() : nativeIds[index];
                var json = JsonSerializer.Serialize(new Holon
                {
                    Id = mode == "payload" ? Guid.NewGuid() : id,
                    CreatedByAvatarId = actor,
                    Name = mode == "docsize" ? new string('x', 1024 * 1024 + 1) : mode == "unicode" ? new string('€', 400000) : mode == "aggregate" ? new string('x', 900000) : "coffee",
                    DeletedDate = mode == "deleted" ? DateTime.UtcNow : default,
                    MetaData = new() { ["precise"] = 1234567890.123456789012345678m }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                if (mode == "split")
                {
                    var prefix = "{\"id\":\"" + id.ToString("D") + "\",\"name\":\"";
                    json = prefix + new string('a', 4095 - prefix.Length) + "😀\"}";
                }
                table.Rows.Add(id.ToString("D"), mode == "timestamp" ? 1L : 0L, mode == "null" ? (object)DBNull.Value : json);
            }
            return Task.FromResult<DbDataReader>(table.CreateDataReader());
        } };
        var provider = new QuestDBOASIS(() => connection) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids, mode == "paging"),
            maximumResults: mode == "budget" ? 1 : 100, cancellationToken: cancellation.Token);
        var success = mode is "success" or "paging" or "empty" or "deleted" or "split";
        Assert.AreEqual(success, !result.IsError, result.Message + result.Exception);
        Assert.AreEqual(mode == "open" ? 0 : 1, calls);
        Assert.IsTrue(connection.Disposed);
        if (mode is "success" or "split") CollectionAssert.AreEqual(ids, result.Result.Select(row => row.Id).ToArray());
        else if (mode == "paging") Assert.AreEqual(ids[1], result.Result.Single().Id);
        else if (success) Assert.AreEqual(0, result.Result.Count);
        else { Assert.IsNull(result.Result); Assert.IsFalse(result.IsLoaded); }
        if (mode == "success") Assert.AreEqual(1234567890.123456789012345678m, ((JsonElement)result.Result[0].MetaData["precise"]).GetDecimal());
        if (mode == "cancel") Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        if (mode is "docsize" or "unicode" or "aggregate" or "overflow" or "budget") Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
    }

    [TestMethod]
    public async Task EmptyCatalogueAndPreCancellationDoNotOpenConnections()
    {
        var opens = 0;
        var provider = new QuestDBOASIS(() => { opens++; throw new InvalidOperationException(); }) { IsProviderActivated = true };
        var empty = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), System.Array.Empty<Guid>()));
        Assert.IsFalse(empty.IsError, empty.Message);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), new[] { Guid.NewGuid() }), cancellationToken: cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.AreEqual(0, opens);
    }
}
