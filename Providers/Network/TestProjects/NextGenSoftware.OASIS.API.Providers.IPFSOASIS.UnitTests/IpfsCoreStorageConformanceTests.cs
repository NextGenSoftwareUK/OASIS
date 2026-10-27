using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IPFSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

// Uses the retained offline Kubo environment, not a mock transport or replacement repository.
[Collection("Owned IPFS")]
public class IpfsCoreStorageConformanceTests
{
    [Fact,Trait("Category","IPFSCoreStorageCleanup")]
    public async Task ReconcileExactlyTheTwoIndependentlyObservedOriginalAvatars()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var state=Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))!;
        var inspection=Path.Combine(root!.FullName,"TestResults","IPFSCurrent","original-save-inspection.json");
        var bytes=await File.ReadAllBytesAsync(inspection);
        Assert.Equal("844247411279285D6318F6271EBE5BEBFE10160847C073546837E350453116DE",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        var pending=Path.Combine(state,"avatar-ack-cleanup.pending.json");
        var completed=Path.Combine(state,"avatar-ack-cleanup.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(completed),"Preserve an existing reconciliation; never replay cleanup.");
        using var observations=JsonDocument.Parse(bytes);
        Assert.Equal(2,observations.RootElement.GetArrayLength());
        var selected=new List<(Provider Provider,JsonElement Observation,string DnaPath,byte[] Original)>();
        var outcomes=new List<object>();
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        try
        {
            // Admit every exact original catalogue before the first mutation.
            foreach(var observation in observations.RootElement.EnumerateArray())
            {
                var mode=observation.GetProperty("mode").GetString()!;
                Assert.Contains(mode,new[]{"async","sync"});
                var dnaPath=Path.Combine(state,"conformance-"+mode+"-dna.json");
                var original=await File.ReadAllBytesAsync(dnaPath);
                Assert.Equal(observation.GetProperty("dnaSha256").GetString(),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)));
                var dna=Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(original))!;
                Assert.Equal(observation.GetProperty("rootCid").GetString(),dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                var provider=new Provider(dna,dnaPath);Assert.False((await provider.ActivateProviderAsync()).IsError);
                var entry=Assert.Single(await provider.LoadLookupToJson());
                Assert.Equal("avatar",entry.Value.RecordKind);
                Assert.Equal(observation.GetProperty("id").GetGuid(),entry.Value.Id);
                Assert.Equal(observation.GetProperty("recordCid").GetString(),entry.Key);
                selected.Add((provider,observation,dnaPath,original));
            }
            Assert.Equal(2,selected.Select(row=>row.Observation.GetProperty("mode").GetString()).Distinct().Count());
            var archive=Path.Combine(state,"Archive");Directory.CreateDirectory(archive);
            foreach(var item in selected)
            {
                var target=Path.Combine(archive,"avatar-ack-original-"+item.Observation.GetProperty("mode").GetString()+"-dna.json");
                await using var file=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                await file.WriteAsync(item.Original);file.Flush(true);
            }
            async Task Progress(bool complete)
            {
                await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                    await JsonSerializer.SerializeAsync(file,new{schema=1,complete,scope="Exact observed original avatar logical removal; immutable CIDs preserved",outcomes});file.Flush(true);
                }
                File.Move(pending+".next",pending,true);
            }
            await Progress(false);
            foreach(var item in selected)
            {
                Assert.Equal(item.Observation.GetProperty("dnaSha256").GetString(),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(item.DnaPath))));
                var id=item.Observation.GetProperty("id").GetGuid();
                var removed=await item.Provider.DeleteAvatarAsync(id,softDelete:false);
                Assert.False(removed.IsError,removed.Exception?.GetType().FullName);Assert.True(removed.Result);Assert.True(removed.IsSaved);
                Assert.Empty(await item.Provider.LoadLookupToJson());
                var absent=await item.Provider.LoadAvatarAsync(id);
                Assert.True(absent.IsError);Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,absent.ErrorCode);
                outcomes.Add(new{mode=item.Observation.GetProperty("mode").GetString(),id,acknowledged=true,catalogueEmpty=true,pointAbsent=true});
                await Progress(false);
            }
            await Progress(true);File.Move(pending,completed);
        }
        finally{OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
    }

    [Fact,Trait("Category","IPFSCoreStorageInspect")]
    public async Task OriginalFailedSaveRecordsRemainReadableWithoutReplay()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var state=Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))!;
        var output=Path.Combine(root!.FullName,"TestResults","IPFSCurrent","original-save-inspection.json");
        Assert.False(File.Exists(output),"Preserve the original read-only evidence.");
        var observations=new List<object>();
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        try
        {
            foreach(var mode in new[]{"async","sync"})
            {
                var dnaPath=Path.Combine(state,"conformance-"+mode+"-dna.json");
                var dnaBytes=await File.ReadAllBytesAsync(dnaPath);
                var digest=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(dnaBytes));
                using var report=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","IPFSOASIS",mode,"report.json")));
                Assert.Equal("avatar-save",report.RootElement.GetProperty("CurrentCase").GetString());
                var expectedId=Guid.Parse(mode=="async"?"5a0a8c3d-421a-41c3-89ea-98d562ef54d1":"09d44b59-bd95-4eb4-a6d7-42c752158320");
                Assert.Equal(expectedId,report.RootElement.GetProperty("OwnerId").GetGuid());
                var dna=Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(dnaBytes))!;
                var expectedRoot=mode=="async"?"QmbFyM1JpYXu6DehWkDuaXVYJRFxNvHrFU9sShLxnFgiGT":"QmXuu1N91m5dR2oh3FzLZWpPYgNgPQuCxp7EMHGtpAJD9D";
                Assert.Equal(expectedRoot,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                var provider=new Provider(dna,dnaPath);
                Assert.False((await provider.ActivateProviderAsync()).IsError);
                var loaded=await provider.LoadAvatarAsync(expectedId);
                Assert.False(loaded.IsError,loaded.Exception?.GetType().FullName);
                Assert.True(loaded.IsLoaded);Assert.Equal(1,loaded.LoadedCount);Assert.Equal(expectedId,loaded.Result.Id);
                Assert.Equal("conformance-"+expectedId.ToString("N"),loaded.Result.Username);
                Assert.Equal(digest,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(dnaPath))));
                observations.Add(new {mode,id=expectedId,rootCid=expectedRoot,recordCid=loaded.Result.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS],dnaSha256=digest});
            }
            await using var result=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            await JsonSerializer.SerializeAsync(result,observations);result.Flush(true);
        }
        finally{OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "IPFSCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedIpfs(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.NotNull(root);
        var work = Path.Combine(root!.FullName, "TestResults", "ProviderConformanceCurrent", "IPFSOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        var state = Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))
            ?? throw new InvalidOperationException("Use the owned persistent IPFS environment.");
        var journalPath = Path.Combine(state, "conformance-" + (sync ? "sync" : "async") + ".pending.json");
        Assert.False(File.Exists(journalPath) || File.Exists(journalPath + ".next"), "Preserve the original fixture intent before another dispatch.");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve prior fixture/report before rerunning.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        CoreStorageConformance.Report? activeFixture = null;
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            activeFixture = report;
            // Irreproducible fixture ownership survives cleanup of raw TestResults.
            await using (var journal = new FileStream(journalPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(journal, new { report.Provider, report.Synchronous,
                    report.OwnerId, report.RootId, report.ChildId, report.BatchIds, report.ImportId,
                    report.CurrentCase, report.DeletionKinds, report.HardCleanedFixtures, report.FixtureCleanupComplete });
                journal.Flush(true);
            }
            File.Move(journalPath + ".next", journalPath, overwrite: true);
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        Provider provider;
        try
        {
            const string endpoint = "http://127.0.0.1:18547";
            if (Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC") != endpoint)
                throw new InvalidOperationException("Retained offline loopback Kubo runner required.");
            var repo = Environment.GetEnvironmentVariable("IPFS_PATH");
            var dnaPath = Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH");
            if (string.IsNullOrWhiteSpace(repo) || !File.Exists(Path.Combine(repo, "config")) ||
                string.IsNullOrWhiteSpace(dnaPath) || Path.GetFileName(dnaPath) != "owned-dna.json" ||
                !string.Equals(Path.GetDirectoryName(Path.GetFullPath(dnaPath)), Path.GetDirectoryName(Path.GetFullPath(repo)), StringComparison.OrdinalIgnoreCase) ||
                File.Exists(dnaPath))
                throw new InvalidOperationException("Existing retained repository and unused owned fixture path required; no initialization or reset.");
            dnaPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dnaPath))!, "conformance-" + (sync ? "sync" : "async") + "-dna.json");
            if (File.Exists(dnaPath)) throw new InvalidOperationException("Preserve the prior mode-specific IPFS fixture before rerunning.");
            var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_IPFS_STORAGE_ENCRYPTION_KEY") ?? "");
            try { if (key.Length != 32) throw new InvalidOperationException("Original persisted storage key required."); }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { StorageProviders = new StorageProviderSettings {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            } } };
            provider = new Provider(dna, dnaPath);
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "IPFSOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.True(false, "IPFS configuration blocked; no native storage operation dispatched. Restore the original retained repository/key.");
            return;
        }
        var priorDna = OASISDNAManager.OASISDNA;
        var priorPath = OASISDNAManager.OASISDNAPath;
        try
        {
            var result = await CoreStorageConformance.RunAsync(provider, "IPFSOASIS", "native", sync, Checkpoint,
                cleanupSoftDeletedFixture: async (id, synchronous) =>
                {
                    // Test maintenance only, AFTER the shared suite verifies shipping soft deletion.
                    Assert.NotNull(activeFixture);
                    var allowed = new[] { activeFixture!.RootId, activeFixture.ChildId, activeFixture.ImportId }.Concat(activeFixture.BatchIds);
                    Assert.Contains(id, allowed);
                    var observed = await provider.LoadHolonAsync(id, loadChildren:false);
                    Assert.False(observed.IsError);Assert.True(observed.Result.IsDeleted);
                    Assert.Equal(activeFixture.OwnerId, observed.Result.CreatedByAvatarId);
                    var catalogue = await provider.LoadLookupToJson();
                    var entry = Assert.Single(catalogue.Where(pair => pair.Value.RecordKind == "holon" && pair.Value.Id == id));
                    catalogue.Remove(entry.Key);
                    await provider.SaveLookupToFile(catalogue);
                    return new NextGenSoftware.OASIS.Common.OASISResult<bool>(true) {IsSaved=true,SavedCount=1};
                }, expectedDefaultDeletion:"soft");
            Assert.True(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
            Assert.True(result.FixtureCleanupComplete);
            File.Delete(journalPath);
        }
        finally { OASISDNAManager.OASISDNA = priorDna; OASISDNAManager.OASISDNAPath = priorPath; }
    }
}
