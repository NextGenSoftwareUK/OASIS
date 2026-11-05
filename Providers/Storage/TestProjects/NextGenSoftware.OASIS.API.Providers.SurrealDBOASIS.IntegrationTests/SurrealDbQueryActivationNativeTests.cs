using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests;

[TestClass]
public class SurrealDbQueryActivationNativeTests
{
    [TestMethod]
    public async Task DatabaseReaderActivatesAndReadsTheAdmittedTeachingRecord()
    {
        var settings = new[] { "ENDPOINT", "USERNAME", "PASSWORD", "NAMESPACE", "DATABASE", "RECORD_ID" };
        if (settings.All(suffix => Environment.GetEnvironmentVariable("SURREALDB_CQL_READER_" + suffix) == null))
            Assert.Inconclusive("No dedicated SurrealDB query reader is configured.");

        static string Required(string suffix) =>
            Environment.GetEnvironmentVariable("SURREALDB_CQL_READER_" + suffix)
            ?? throw new InvalidOperationException($"Set the dedicated reader setting {suffix}.");

        var id = Guid.Parse(Required("RECORD_ID"));
        var provider = new SurrealDBOASIS(Required("ENDPOINT"), Required("USERNAME"),
            Required("PASSWORD"), Required("NAMESPACE"), Required("DATABASE"), id);
        var active = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Message);
        Assert.IsTrue(provider.IsProviderActivated);

        try
        {
            const ProviderType authority = ProviderType.SurrealDBOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, new[] { id }).Result)
                .Result.Where(row => row.Id == id).Result;
            var plan = CosmicQueryPlanner.Plan(query, authority,
                new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(id, result.Result.Single().Id);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
