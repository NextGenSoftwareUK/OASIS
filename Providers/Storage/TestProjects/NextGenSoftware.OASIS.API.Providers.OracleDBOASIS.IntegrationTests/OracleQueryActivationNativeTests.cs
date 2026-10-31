using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class OracleQueryActivationNativeTests
{
    private sealed class Source(string connection) : OracleDBOASIS(connection)
    {
        public int ProvisionAttempts;
        protected override Task ProvisionStorageSchemaAsync(OracleConnection connection)
        {
            ProvisionAttempts++;
            throw new InvalidOperationException("Query activation must never provision.");
        }
    }

    [TestMethod, TestCategory("OracleQueryActivationNative")]
    public async Task ExistingSourceActivatesWithoutDdlOrChangingRetainedRecords()
    {
        var connectionString = Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Explicit retained Oracle connection required.");
        var seed = Environment.GetEnvironmentVariable("OASIS_ORACLE_CQL_SEED")
            ?? throw new InvalidOperationException("Explicit retained CQL seed path required.");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(seed));
        var ids = document.RootElement.GetProperty("Ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(4, ids.Length);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        async Task<string> Snapshot()
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellation.Token);
            var text = new StringBuilder();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OBJECT_NAME,OBJECT_TYPE,TO_CHAR(LAST_DDL_TIME,'YYYY-MM-DD HH24:MI:SS'),STATUS FROM USER_OBJECTS WHERE OBJECT_NAME LIKE 'OASIS_%' ORDER BY OBJECT_NAME,OBJECT_TYPE";
                await using var reader = await command.ExecuteReaderAsync(cancellation.Token);
                while (await reader.ReadAsync(cancellation.Token))
                    for (var index = 0; index < 4; index++) text.Append(reader.GetString(index)).Append('\n');
            }
            await using (var command = connection.CreateCommand())
            {
                command.BindByName = true;
                command.CommandText = "SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE ID IN (:i0,:i1,:i2,:i3) ORDER BY ID";
                for (var index = 0; index < ids.Length; index++)
                    command.Parameters.Add("i" + index, OracleDbType.Raw, 16).Value = ids[index].ToByteArray();
                await using var reader = await command.ExecuteReaderAsync(cancellation.Token);
                var count = 0;
                while (await reader.ReadAsync(cancellation.Token))
                {
                    text.Append(Convert.ToHexString((byte[])reader.GetValue(0))).Append('\n');
                    using var body = reader.GetTextReader(1);
                    text.Append(await body.ReadToEndAsync(cancellation.Token)).Append('\n');
                    count++;
                }
                Assert.AreEqual(4, count, "Do not reconstruct or overwrite missing retained records.");
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
        }

        var before = await Snapshot();
        var source = new Source(connectionString);
        var result = await ((ICosmicReadOnlyQueryActivation)source).ActivateQuerySourceAsync(cancellation.Token);
        Assert.IsFalse(result.IsError, result.Exception?.GetType().Name + ": " + result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(source.IsProviderActivated);
        Assert.AreEqual(0, source.ProvisionAttempts);
        Assert.AreEqual(before, await Snapshot(), "Read-only activation must preserve schema and source records.");
        Assert.IsTrue((await source.DeActivateProviderAsync()).Result);
    }
}
