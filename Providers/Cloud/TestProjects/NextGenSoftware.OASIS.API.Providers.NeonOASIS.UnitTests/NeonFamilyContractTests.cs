using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Npgsql;
using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS.UnitTests;

[TestClass]
public class NeonFamilyContractTests
{
    private const string Direct = "Host=ep-demo.us-east-2.aws.neon.tech;Database=oasis;Username=cosmic_reader;SslMode=VerifyFull";

    private static NpgsqlConnectionStringBuilder Config(NeonOASIS provider)
    {
        for (var type = provider.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field != null) return new NpgsqlConnectionStringBuilder((string)field.GetValue(provider)!);
        }
        throw new AssertFailedException("Expected effective Npgsql connection configuration.");
    }

    [TestMethod]
    public void FacadeSharesStorageImplementationAndKeepsIdentity()
    {
        var provider = new NeonOASIS("Host=127.0.0.1;Database=synthetic;Username=synthetic");
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlFamilyProviderBase));
        Assert.AreEqual("NeonOASIS", provider.ProviderType.Value.ToString());
        Assert.AreEqual("NeonOASIS", provider.ProviderName);
        Assert.AreEqual(typeof(NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.PostgreSQLOASIS), provider.GetType().GetMethod("SaveAvatarAsync")!.DeclaringType);
    }

    [DataTestMethod]
    [DataRow("ep-demo.us-east-2.aws.neon.tech", true, "ep-demo-pooler.us-east-2.aws.neon.tech")]
    [DataRow("ep-demo-pooler.us-east-2.aws.neon.tech", true, "ep-demo-pooler.us-east-2.aws.neon.tech")]
    [DataRow("ep-demo.us-east-2.aws.neon.tech", false, "ep-demo.us-east-2.aws.neon.tech")]
    public void NeonPoolerUsesHostnameAndVerifiedTls(string host, bool pooler, string expected)
    {
        var config = Config(new NeonOASIS($"Host={host};Port=5432;Database=synthetic;Username=synthetic", pooler));
        Assert.AreEqual(expected, config.Host);
        Assert.AreEqual(5432, config.Port);
        Assert.AreEqual(SslMode.VerifyFull, config.SslMode);
    }

    [TestMethod]
    public void ExplicitProxyPortAndNonNeonConfigurationArePreserved()
    {
        var config = Config(new NeonOASIS("Host=127.0.0.1;Port=15432;Database=synthetic;Username=synthetic;SslMode=Disable"));
        Assert.AreEqual("127.0.0.1", config.Host);
        Assert.AreEqual(15432, config.Port);
        Assert.AreEqual(SslMode.Disable, config.SslMode);
    }

    [TestMethod]
    public async Task ExplicitSourceAdmitsBoundedNeonIdentityAndHonoursCancellation()
    {
        var id = Guid.NewGuid();
        var provider = new NeonQuerySource(Direct, id);
        var settings = Config(provider);
        Assert.AreEqual("ep-demo.us-east-2.aws.neon.tech", settings.Host);
        Assert.AreEqual(SslMode.VerifyFull, settings.SslMode);
        const ProviderType authority = ProviderType.NeonOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, new[] { id }).Result).Result
            .Where(row => row.Id == id).Result;
        var plan = CosmicQueryPlanner.Plan(query, authority,
            new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        Assert.IsTrue(new NeonOASIS(Direct).QueryCapabilities.Admit(query).IsError);
        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var activation = await provider.ActivateQuerySourceAsync(cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", activation.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("ep-demo-pooler.us-east-2.aws.neon.tech", "VerifyFull")]
    [DataRow("ep-demo.neon.tech.invalid", "VerifyFull")]
    [DataRow("localhost", "VerifyFull")]
    [DataRow("ep-demo.us-east-2.aws.neon.tech", "Require")]
    public void RejectsNonDirectOrUnverifiedQueryEndpoints(string host, string tls)
    {
        Assert.ThrowsException<ArgumentException>(() => new NeonQuerySource(
            $"Host={host};Database=oasis;Username=cosmic_reader;SslMode={tls}", Guid.NewGuid()));
    }
}
