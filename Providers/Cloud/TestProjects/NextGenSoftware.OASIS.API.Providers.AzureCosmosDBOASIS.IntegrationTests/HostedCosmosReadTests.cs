using System.Text.Json;
using System.Text.Json.Serialization;
using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

// Separate opt-in test: never included in the emulator or controlled CQL filters.
[TestClass, DoNotParallelize]
public sealed class HostedCosmosReadTests
{
    [TestMethod]
    public async Task ReadExistingHostedRecordsThroughSharedQueries()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_HOSTED_COSMOS_EXECUTE"));
        var configPath = Environment.GetEnvironmentVariable("OASIS_HOSTED_COSMOS_CONFIG");
        Assert.IsFalse(string.IsNullOrWhiteSpace(configPath), "Supply the non-secret configuration path.");
        var settings = HostedCosmosReadSettings.FromJson(await File.ReadAllTextAsync(configPath!));
        var authKey = Environment.GetEnvironmentVariable("OASIS_HOSTED_COSMOS_AUTH_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(authKey), "Supply authentication through the protected process environment, never the configuration file.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var output = Path.Combine(root.FullName, "TestResults/HostedCosmosCurrent/read-only-report.json");
        Assert.IsFalse(File.Exists(output) || File.Exists(output + ".next"), "Preserve prior hosted evidence before another run.");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var provider = new AzureCosmosDBOASIS(settings.Endpoint, authKey!, settings.DatabaseName,
            new() { "holonItems", "avatarItems", "avatarDetailItems" }, CosmosResourceInitializationMode.ExistingResourcesOnly);
        try
        {
            // Hosted endpoint admission precedes client construction. Activation
            // validates existing schemas and uses the provider's normal TLS checks.
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.ErrorCode);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(provider);
            var manager = new HolonManager(null, dna, registry);
            var ids = settings.ReadableIds.ToArray();
            var scope = CosmicQueryScope.Named(ProviderType.AzureCosmosDBOASIS).Result;
            var access = CosmicQueryReadAccess.AuthoritativeRecords(settings.Actor, ProviderType.AzureCosmosDBOASIS, ids).Result;
            var source = CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Where(row => ids.Contains(row.Id));
            Assert.IsFalse(source.IsError, source.ErrorCode);
            var query = CosmicLinq.Create(manager, provider, source.Result, maximumResults: 8);
            Assert.IsFalse(query.IsError, query.ErrorCode);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var rows = await query.Result.OrderBy(row => row.Id).ToCosmicListAsync(timeout.Token);
            Assert.IsFalse(rows.IsError, rows.ErrorCode);
            CollectionAssert.AreEquivalent(ids, rows.Result.Select(row => row.Id).ToArray());

            var subsetIds = ids.Take(2).ToArray();
            var subset = await query.Result.Where(row => subsetIds.Contains(row.Id)).ToCosmicListAsync(timeout.Token);
            Assert.IsFalse(subset.IsError, subset.ErrorCode);
            CollectionAssert.AreEquivalent(subsetIds, subset.Result.Select(row => row.Id).ToArray());
            var selected = await query.Result.Select(row => new { row.Id, row.Name }).ToCosmicListAsync(timeout.Token);
            Assert.IsFalse(selected.IsError, selected.ErrorCode);
            Assert.AreEqual(ids.Length, selected.Result.Count);
            var longCount = query.Result.LongCount();
            Assert.AreEqual((long)ids.Length, longCount);

            var paired = Environment.GetEnvironmentVariable("OASIS_HOSTED_COSMOS_MONGO") == "1";
            if (paired)
                await CqlNativeCloudMongoAcceptance.VerifyAsync(root.FullName, manager, provider,
                    ids.Take(3).ToArray(), settings.Actor, hosted: true);

            // Read-only evidence contains no key, connection string, raw data or
            // claim that operator confirmation independently verified Azure billing.
            using (var file = new FileStream(output + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new
                {
                    provider = "AzureCosmosDBOASIS", endpointHost = settings.Endpoint.Host,
                    hostedReadVerified = true, scope = "Existing owned synthetic records; SDK read acceptance only",
                    rows = rows.Result.Count, subsetRows = subset.Result.Count, projectionRows = selected.Result.Count,
                    longCount = longCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    operatorFreeAccountConfirmation = true, independentBillingVerification = false,
                    infrastructureProvisioned = false, hostedDataWrites = 0,
                    mongoFederationAndHttpVerified = paired, mongoFixtureWrites = paired ? 3 : 0,
                    mongoFixtureCleanupVerified = paired,
                    browserInteractionRequested = paired && Environment.GetEnvironmentVariable("OASIS_COSMOS_MONGO_SERVE") == "1"
                });
                file.Flush(true);
            }
            File.Move(output + ".next", output);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
