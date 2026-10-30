using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests;

[TestClass]
public class QuestDbSaveOwnershipTests
{
    private sealed class Coffee : Holon { public string Category { get; set; } = "coffee"; }
    private sealed class Connection : DbConnection
    {
        public Func<DbCommand, CancellationToken, Task<int>> Execute = (_, _) => throw new InvalidOperationException();
        public bool FailOpen, Disposed;
        private ConnectionState state;
        public override string ConnectionString { get; set; } = "";
        public override string Database => "qdb";
        public override string DataSource => "admitted-test";
        public override string ServerVersion => "test";
        public override ConnectionState State => state;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() => state = ConnectionState.Closed;
        public override void Open() => throw new InvalidOperationException("Use asynchronous opening.");
        public override Task OpenAsync(CancellationToken token)
        {
            Assert.IsTrue(token.CanBeCanceled);
            if (FailOpen) throw new IOException("Open failed");
            state = ConnectionState.Open;
            return Task.CompletedTask;
        }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Command(this);
        protected override void Dispose(bool disposing) { Disposed = true; Close(); base.Dispose(disposing); }
    }
    private sealed class Command(Connection owner) : DbCommand
    {
        private readonly NpgsqlCommand parameterOwner = new();
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = owner;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => parameterOwner.Parameters;
        public override void Cancel() { }
        public override void Prepare() => throw new NotSupportedException();
        protected override DbParameter CreateDbParameter() => new NpgsqlParameter();
        public override int ExecuteNonQuery() => throw new InvalidOperationException("Use asynchronous execution.");
        public override Task<int> ExecuteNonQueryAsync(CancellationToken token) => owner.Execute(this, token);
        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) parameterOwner.Dispose(); base.Dispose(disposing); }
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("zero")]
    [DataRow("unknown")]
    [DataRow("multiple")]
    [DataRow("disconnect")]
    [DataRow("open")]
    public async Task ParameterizedNativeAcknowledgementOwnsIdentityPublication(string mode)
    {
        var holon = new Coffee { Name = "Espresso", MetaData = new() { ["precise"] = 1234567890.123456789012345678m } };
        var writes = 0;
        var connection = new Connection { FailOpen = mode == "open", Execute = (command, _) =>
        {
            writes++;
            Assert.AreEqual(Guid.Empty, holon.Id);
            Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.QuestDBOASIS) == true);
            Assert.AreEqual(15, command.CommandTimeout);
            StringAssert.Contains(command.CommandText, "INSERT INTO oasis_holons (id, data, ts) VALUES (@id, @data,");
            StringAssert.Contains(command.CommandText, "1970-01-01T00:00:00.000000Z");
            var id = Guid.Parse((string)command.Parameters["id"].Value!);
            using var payload = JsonDocument.Parse((string)command.Parameters["data"].Value!);
            Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
            Assert.AreEqual("coffee", payload.RootElement.GetProperty("category").GetString());
            Assert.AreEqual(1234567890.123456789012345678m, payload.RootElement.GetProperty("metaData").GetProperty("precise").GetDecimal());
            Assert.AreEqual(id.ToString("D"), payload.RootElement.GetProperty("providerUniqueStorageKey").GetProperty("QuestDBOASIS").GetString());
            if (mode == "disconnect") throw new IOException("Unknown write outcome");
            return Task.FromResult(mode == "zero" ? 0 : mode == "unknown" ? -1 : mode == "multiple" ? 2 : 1);
        } };
        var result = await new QuestDBOASIS(() => connection).SaveHolonAsync(holon, saveChildren: false);
        Assert.AreEqual(mode == "success", !result.IsError, result.Message);
        Assert.AreEqual(mode == "success", result.IsSaved);
        Assert.AreEqual(mode == "success" ? 1 : 0, result.SavedCount);
        Assert.AreEqual(mode == "open" ? 0 : 1, writes);
        Assert.IsTrue(connection.Disposed);
        if (mode == "success") Assert.AreEqual(holon.Id.ToString("D"), holon.ProviderUniqueStorageKey[ProviderType.QuestDBOASIS]);
        else { Assert.AreEqual(Guid.Empty, holon.Id); Assert.IsNull(result.Result); Assert.IsNotNull(result.Exception); }
    }

    [TestMethod]
    public async Task ExistingCallerKeysRemainUnchangedOnFailedWrite()
    {
        var keys = new Dictionary<ProviderType, string> { [ProviderType.QuestDBOASIS] = "original", [ProviderType.MongoDBOASIS] = "other" };
        var id = Guid.NewGuid();
        var holon = new Holon { Id = id, ProviderUniqueStorageKey = keys };
        var connection = new Connection { Execute = (_, _) => Task.FromResult(0) };
        var result = await new QuestDBOASIS(() => connection).SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(id, holon.Id);
        Assert.AreSame(keys, holon.ProviderUniqueStorageKey);
        Assert.AreEqual("original", keys[ProviderType.QuestDBOASIS]);
        Assert.AreEqual("other", keys[ProviderType.MongoDBOASIS]);
    }

    [DataTestMethod]
    [DataRow("history")]
    [DataRow("size")]
    public async Task UnsupportedWriteRejectsBeforeOpeningConnection(string mode)
    {
        var opens = 0;
        var provider = new QuestDBOASIS(() => { opens++; throw new InvalidOperationException(); }) { IsVersionControlEnabled = mode == "history" };
        var holon = new Holon { Name = mode == "size" ? new string('x', 1024 * 1024 + 1) : "coffee" };
        Assert.IsTrue((await provider.SaveHolonAsync(holon, saveChildren: false)).IsError);
        Assert.AreEqual(Guid.Empty, holon.Id);
        Assert.AreEqual(0, opens);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchReportsPartialAcknowledgementsAndHonoursStop(bool keepGoing)
    {
        var opens = 0;
        var provider = new QuestDBOASIS(() => new Connection { Execute = (_, _) => Task.FromResult(++opens == 1 ? 0 : 1) });
        var rows = new[] { new Holon(), new Holon() };
        var result = await provider.SaveHolonsAsync(rows, saveChildren: false, continueOnError: keepGoing);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(keepGoing ? 2 : 1, opens);
        Assert.AreEqual(keepGoing ? 1 : 0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, rows[0].Id);
    }
}
