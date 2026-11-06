using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.NileOASIS;
using NileStorage = NextGenSoftware.OASIS.API.Providers.NileOASIS.NileOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class NileQuerySourceTests
{
    private const string Reader = "Database=oasis;Username=fixture;Password=fixture";

    [DataTestMethod]
    [DataRow("db.thenile.dev")]
    [DataRow("us-west-2.db.thenile.dev")]
    public async Task ExplicitSourceAdmitsNileIdentityAndHonoursCancellation(string host)
    {
        var id = Guid.NewGuid();
        var connection = $"Host={host};{Reader};SslMode=VerifyFull";
        var source = new NileQuerySource(connection, id);
        const ProviderType authority = ProviderType.NileOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, new[] { id }).Result).Result
            .Where(row => row.Id == id).Result;
        var plan = CosmicQueryPlanner.Plan(query, authority,
            new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        Assert.IsTrue(new NileStorage(connection).QueryCapabilities.Admit(query).IsError);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await source.ExecuteCosmicQueryAsync(plan.Result)).ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await source.ActivateQuerySourceAsync(cancelled.Token)).ErrorCode);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("db.thenile.dev.invalid", 5432, "VerifyFull")]
    [DataRow("api.thenile.dev", 5432, "VerifyFull")]
    [DataRow("extra.us-west-2.db.thenile.dev", 5432, "VerifyFull")]
    [DataRow("db.thenile.dev", 15432, "VerifyFull")]
    [DataRow("db.thenile.dev", 5432, "Require")]
    public void RejectsForeignEndpointsAndUnverifiedTls(string host, int port, string sslMode)
    {
        Assert.ThrowsException<ArgumentException>(() => new NileQuerySource(
            $"Host={host};Port={port};{Reader};SslMode={sslMode}", Guid.NewGuid()));
    }
}
