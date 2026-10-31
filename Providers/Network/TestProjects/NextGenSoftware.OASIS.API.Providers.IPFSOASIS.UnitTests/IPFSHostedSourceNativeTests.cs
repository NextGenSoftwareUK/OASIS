using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSHostedSourceNativeTests
{
    [Fact, Trait("Category", "IPFSHostedSourceNative")]
    public async Task ProvisionOnceOrVerifyTheRetainedPublicHostedSource()
    {
        var folder = Environment.GetEnvironmentVariable("OASIS_HOSTED_IPFS_SOURCE_ROOT")
            ?? throw new InvalidOperationException("Use the explicit hosted-source runner.");
        Assert.Equal("http://127.0.0.1:18547", Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        Directory.CreateDirectory(folder);
        var intent = Path.Combine(folder, "source.intent.json");
        var acknowledgement = Path.Combine(folder, "source.acknowledged.json");
        var dnaPath = Path.Combine(folder, "source-dna.json");
        var priorDna = OASISDNAManager.OASISDNA;
        var priorPath = OASISDNAManager.OASISDNAPath;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        IPFSOASIS? provider = null;
        try
        {
            Source source;
            if (File.Exists(acknowledgement))
            {
                source = JsonSerializer.Deserialize<Source>(await File.ReadAllTextAsync(acknowledgement))!;
                Assert.True(File.Exists(intent));
                OASISDNAManager.LoadDNA(dnaPath);
                provider = new IPFSOASIS(OASISDNAManager.OASISDNA, dnaPath) { EnableCqlCidKeySets = true };
                Assert.Equal(source.CatalogueCid, OASISDNAManager.OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                Assert.False((await provider.ActivateQuerySourceAsync(timeout.Token)).IsError);
            }
            else
            {
                Assert.False(File.Exists(intent) || File.Exists(dnaPath), "Reconcile the original write; never seed over an unknown outcome.");
                var owner = Guid.NewGuid();
                var records = new[]
                {
                    new Holon { Id = Guid.NewGuid(), Name = "IPFS Espresso", Description = "A public encrypted IPFS coffee record" },
                    new Holon { Id = Guid.NewGuid(), Name = "IPFS Flat White", Description = "A second public IPFS coffee record" },
                    new Holon { Id = Guid.NewGuid(), Name = "IPFS Green Tea", Description = "A public IPFS tea record" }
                };
                foreach (var record in records)
                {
                    record.CreatedByAvatarId = owner;
                    record.IsPublic = true;
                    record.IsActive = true;
                    record.MetaData = new Dictionary<string, object> { ["COSMIC.Demo.Dataset"] = "COSMIC.CQL.HostedIPFS.v1" };
                }
                records[0].Children.Add(records[1]);
                records[0].Children.Add(records[2]);
                await WriteNew(intent, new { owner, ids = records.Select(record => record.Id), purpose = "retained hosted public demo source" });
                var dna = new OASISDNA
                {
                    OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                    {
                        HyperDriveMode = HyperDriveModes.V2,
                        StorageProviders = new StorageProviderSettings
                        {
                            IPFSOASIS = new IPFSOASISSettings { ConnectionString = "http://127.0.0.1:18547", EnableCqlCidKeySets = true }
                        }
                    }
                };
                provider = new IPFSOASIS(dna, dnaPath) { EnableCqlCidKeySets = true };
                Assert.False((await provider.ActivateQuerySourceAsync(timeout.Token)).IsError);
                // One deliberately journalled source publication. An interrupted result
                // is retained for inspection, never automatically submitted again.
                var saved = await provider.SaveHolonAsync(records[0]);
                Assert.False(saved.IsError, saved.Message);
                Assert.True(saved.IsSaved);
                Assert.Equal(3, saved.SavedCount);
                source = new Source(owner, dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress,
                    records.Select(record => new Record(record.Id, record.ProviderUniqueStorageKey[ProviderType.IPFSOASIS], record.Name)).ToArray());
                await WriteNew(acknowledgement, source);
            }

            Assert.Equal(3, source.Records.Length);
            var catalogue = await provider.LoadLookupToJson();
            Assert.Equal(3, catalogue.Count);
            Assert.True(source.Records.Select(record => record.Id).ToHashSet().SetEquals(catalogue.Values.Select(record => record.Id)));
            foreach (var count in new[] { 3, 2 })
            {
                var keys = source.Records.Take(count).Select(record => record.Cid).ToArray();
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
                    .Where(record => keys.Contains(CosmicQueryMetadata.Value<string>(record, IPFSOASIS.CqlRecordCidKey))).Result
                    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.IPFSOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.IPFSOASIS, provider.QueryCapabilities) });
                Assert.False(plan.IsError, plan.Message);
                var rows = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: timeout.Token);
                Assert.False(rows.IsError, rows.Exception?.ToString());
                Assert.Equal(source.Records.Take(count).Select(record => record.Id), rows.Result.Select(record => record.Id));
                Assert.Equal(source.Records.Take(count).Select(record => record.Name), rows.Result.Select(record => record.Name));
                Assert.Equal(keys, rows.Result.Select(record => (string)record.MetaData[IPFSOASIS.CqlRecordCidKey]));
                Assert.All(rows.Result, record => Assert.True(record.IsPublic));
            }
            // Retained source, not disposable test evidence: the hosted demo will
            // depend on these exact encrypted blocks/catalogue. No cleanup here.
        }
        finally
        {
            if (provider != null) await provider.DeActivateProviderAsync();
            OASISDNAManager.OASISDNA = priorDna;
            OASISDNAManager.OASISDNAPath = priorPath;
        }
    }

    private static async Task WriteNew(string path, object value)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, value);
        stream.Flush(true);
    }

    public sealed record Source(Guid Owner, string CatalogueCid, Record[] Records);
    public sealed record Record(Guid Id, string Cid, string Name);
}
