using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests;

[TestClass]
public class SqlServerHostedCqlNativeTests
{
    private sealed record Source(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId, string ConnectionSha256);

    [DataTestMethod]
    [TestCategory("SqlServerHostedConnectionPolicy")]
    [DataRow("Encrypt=Optional;TrustServerCertificate=False", false)]
    [DataRow("Encrypt=Mandatory;TrustServerCertificate=True", false)]
    [DataRow("Encrypt=Mandatory;TrustServerCertificate=False", true)]
    [DataRow("Encrypt=Strict;TrustServerCertificate=False", true)]
    public void HostedConnectionRequiresValidatedTls(string options, bool accepted)
    {
        var connection = "Server=sql.example;Database=teaching;" + options;
        if (accepted)
            Assert.IsFalse(SQLServerDBOASIS.CreateHostedQuerySource(connection).IsProviderActivated);
        else
            Assert.ThrowsException<InvalidOperationException>(() => SQLServerDBOASIS.CreateHostedQuerySource(connection));
    }

    [TestMethod]
    [TestCategory("SqlServerHostedCqlNative")]
    public async Task ReviewedReadOnlyLoginReadsExactPublicCatalogue()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_SQLSERVER_HOSTED_CQL_SOURCE");
        var connectionString = Environment.GetEnvironmentVariable("CQL_SQLSERVER_QUERY_CONNECTION_STRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsFalse(string.IsNullOrWhiteSpace(connectionString));
        Assert.IsTrue(new FileInfo(path!).Length is > 0 and <= 16384);
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllBytesAsync(path!))!;
        Assert.IsNotNull(source);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connectionString!))).ToLowerInvariant();
        Assert.IsTrue(digest == source.ConnectionSha256, "SQL Server connection differs from the reviewed catalogue.");
        var provider = SQLServerDBOASIS.CreateHostedQuerySource(connectionString!);
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = deadline.Token;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(token);
        await using var permissions = new SqlCommand("""
            DECLARE @id int = OBJECT_ID(N'OASISHolons', N'U');
            DECLARE @table nvarchar(517) =
                QUOTENAME(OBJECT_SCHEMA_NAME(@id)) + N'.' + QUOTENAME(OBJECT_NAME(@id));
            SELECT HAS_PERMS_BY_NAME(@table, N'OBJECT', N'SELECT'),
                   HAS_PERMS_BY_NAME(@table, N'OBJECT', N'INSERT'),
                   HAS_PERMS_BY_NAME(@table, N'OBJECT', N'UPDATE'),
                   HAS_PERMS_BY_NAME(@table, N'OBJECT', N'DELETE'),
                   HAS_PERMS_BY_NAME(@table, N'OBJECT', N'ALTER'),
                   HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CREATE TABLE');
            """, connection);
        await using (var reader = await permissions.ExecuteReaderAsync(token))
        {
            Assert.IsTrue(await reader.ReadAsync(token));
            for (var ordinal = 0; ordinal < 6; ordinal++)
            {
                Assert.IsFalse(await reader.IsDBNullAsync(ordinal, token), "SQL Server permissions are indeterminate.");
                Assert.AreEqual(ordinal == 0 ? 1 : 0, reader.GetInt32(ordinal), "SQL Server reader permissions differ.");
            }
        }

        const ProviderType authority = ProviderType.SQLServerDBOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, authority, ids).Result).Result;

        async Task<Guid[]> Read(CosmicQuery<Holon> selection)
        {
            var plan = CosmicQueryPlanner.Plan(selection, authority, new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result.All(row => row.CreatedByAvatarId == source.Actor && row.IsPublic && !row.IsDeleted));
            return result.Result.Select(row => row.Id).ToArray();
        }

        try
        {
            var active = await provider.ActivateQuerySourceAsync(token);
            Assert.IsFalse(active.IsError, active.Message);
            CollectionAssert.AreEquivalent(source.LiveIds, await Read(query));
            var subset = source.LiveIds.Take(2).ToArray();
            CollectionAssert.AreEquivalent(subset, await Read(query.Where(row => subset.Contains(row.Id)).Result));
            var excluded = new[] { source.DeletedId, source.MissingId };
            Assert.AreEqual(0, (await Read(query.Where(row => excluded.Contains(row.Id)).Result)).Length);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
