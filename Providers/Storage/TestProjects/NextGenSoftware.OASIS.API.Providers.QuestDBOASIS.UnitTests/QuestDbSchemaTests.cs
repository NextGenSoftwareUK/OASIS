using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests;

[TestClass]
public class QuestDbSchemaTests
{
    private sealed class Fixture
    {
        public string Mode = "success";
        public int Calls;
        public readonly List<string> Tables = new();
        public readonly List<QuestDbSaveOwnershipTests.Connection> Connections = new();
        public DbConnection Open()
        {
            var connection = new QuestDbSaveOwnershipTests.Connection { Read = (command, token) =>
            {
                Calls++;
                Assert.IsTrue(token.CanBeCanceled);
                Assert.AreEqual(15, command.CommandTimeout);
                Assert.IsFalse(command.CommandText.Contains("CREATE", StringComparison.OrdinalIgnoreCase));
                if (Mode == "transport") throw new IOException("Native metadata unavailable");
                if (command.CommandText.Contains("FROM tables()"))
                {
                    StringAssert.Contains(command.CommandText, "WHERE table_name = @name LIMIT 2");
                    var name = (string)command.Parameters["name"].Value!;
                    Tables.Add(name);
                    CollectionAssert.Contains(new[] { "oasis_avatars", "oasis_avatar_details", "oasis_holons" }, name);
                    var table = new DataTable();
                    foreach (var pair in new[] { ("id", typeof(int)), ("table_name", typeof(string)), ("designatedTimestamp", typeof(string)),
                        ("partitionBy", typeof(string)), ("walEnabled", typeof(bool)), ("dedup", typeof(bool)), ("ttlValue", typeof(int)), ("matView", typeof(bool)) })
                        table.Columns.Add(pair.Item1, pair.Item2);
                    if (Mode != "missing") table.Rows.Add(Mode == "id" ? 0 : 42, Mode == "name" ? "other" : name,
                        Mode == "time" ? "other" : "ts", Mode == "partition" ? "MONTH" : "DAY", Mode != "wal", Mode != "dedup", Mode == "ttl" ? 1 : 0, Mode == "matview");
                    if (Mode == "duplicate") table.Rows.Add(43, name, "ts", "DAY", true, true, 0, false);
                    return Task.FromResult<DbDataReader>(table.CreateDataReader());
                }
                StringAssert.Contains(command.CommandText, "FROM table_columns('oasis_");
                StringAssert.Contains(command.CommandText, "LIMIT 4");
                var columns = new DataTable();
                columns.Columns.Add("column", typeof(string)); columns.Columns.Add("type", typeof(string));
                columns.Columns.Add("designated", typeof(bool)); columns.Columns.Add("upsertKey", typeof(bool));
                columns.Rows.Add(Mode == "column" ? "other" : "id", Mode == "type" ? "STRING" : "SYMBOL", false, Mode != "key");
                columns.Rows.Add("data", "VARCHAR", false, Mode == "datakey");
                if (Mode != "count") columns.Rows.Add("ts", "TIMESTAMP", Mode != "designated", true);
                if (Mode == "extra") columns.Rows.Add("other", "STRING", false, false);
                return Task.FromResult<DbDataReader>(columns.CreateDataReader());
            } };
            Connections.Add(connection);
            return connection;
        }
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("missing")]
    [DataRow("duplicate")]
    [DataRow("id")]
    [DataRow("name")]
    [DataRow("time")]
    [DataRow("partition")]
    [DataRow("wal")]
    [DataRow("dedup")]
    [DataRow("ttl")]
    [DataRow("matview")]
    [DataRow("column")]
    [DataRow("type")]
    [DataRow("key")]
    [DataRow("datakey")]
    [DataRow("designated")]
    [DataRow("count")]
    [DataRow("extra")]
    [DataRow("transport")]
    public async Task ReadOnlyAdmissionValidatesCanonicalWalDedupSchema(string mode)
    {
        var fixture = new Fixture { Mode = mode };
        var provider = new QuestDBOASIS(fixture.Open) { IsProviderActivated = true };
        var result = await provider.ActivateProviderAsync();
        Assert.AreEqual(mode == "success", !result.IsError, result.Message);
        Assert.AreEqual(mode == "success", provider.IsProviderActivated);
        Assert.AreEqual(1, fixture.Connections.Count);
        Assert.IsTrue(fixture.Connections.Single().Disposed);
        if (mode == "success")
        {
            Assert.AreEqual(6, fixture.Calls);
            CollectionAssert.AreEquivalent(new[] { "oasis_avatars", "oasis_avatar_details", "oasis_holons" }, fixture.Tables);
        }
        else Assert.IsTrue(fixture.Calls <= 2, "Native failures never trigger DDL or polling.");
        if (mode != "success") Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task FailedReactivationClearsPreviouslyActiveState()
    {
        var fixture = new Fixture();
        var provider = new QuestDBOASIS(fixture.Open);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        fixture.Mode = "missing";
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(7, fixture.Calls);
        Assert.IsTrue(fixture.Connections.All(connection => connection.Disposed));
    }
}
