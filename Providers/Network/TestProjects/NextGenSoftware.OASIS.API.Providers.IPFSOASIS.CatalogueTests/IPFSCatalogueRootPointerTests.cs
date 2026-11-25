using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.CatalogueTests;

/// <summary>
/// The catalogue root is mutable state. Hosts such as Railway regenerate the DNA file from OASIS_DNA_JSON on every boot,
/// so with <see cref="IPFSOASISSettings.CatalogueRootPointerPath"/> set the published root must survive that regeneration.
/// </summary>
public class IPFSCatalogueRootPointerTests
{
    private const string RecordCid = "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk";
    private const string RootCid = "QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP";
    private const string SeedCid = "QmXZ6Sh7XMVHyves9YqbGQk2LKSD92TjgjHdDRqtR9ehAP";

    [Fact]
    public async Task PublishedRootSurvivesABootThatRegeneratesTheDnaFile()
    {
        var folder = OwnedFolder("IPFSCatalogueRootPointer");
        var dnaPath = Path.Combine(folder, "owned-dna.json");
        var pointerPath = Path.Combine(folder, "volume", "catalogue-root");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var key = RandomNumberGenerator.GetBytes(32);
        var api = typeof(IpfsClient).GetField("api", BindingFlags.Static | BindingFlags.NonPublic)!;
        var prior = api.GetValue(null);
        var priorDna = OASISDNAManager.OASISDNA;
        var priorPath = OASISDNAManager.OASISDNAPath;
        using var http = new HttpClient(new Transport());
        try
        {
            api.SetValue(null, http);
            var dna = Dna(pointerPath, seed: null);
            var provider = new IPFSOASIS(dna, dnaPath);
            typeof(IPFSOASIS).GetField("_storageKeyResolver", flags)!.SetValue(provider, (Func<byte[]>)(() => (byte[])key.Clone()));
            typeof(IPFSOASIS).GetField("_ipfsClient", flags)!.SetValue(provider, new IpfsClient("http://127.0.0.1:1"));
            var saved = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Version = 1, Username = "pointer-owner", Email = "pointer@example.invalid" });
            Assert.False(saved.IsError, saved.Message);
            Assert.Equal(RootCid, File.ReadAllText(pointerPath).Trim());

            // Boot: the entrypoint rewrites the DNA file with the configured seed, discarding the published root.
            File.WriteAllText(dnaPath, JsonConvert.SerializeObject(Dna(pointerPath, SeedCid)));
            var rebooted = new IPFSOASIS(Dna(pointerPath, SeedCid), dnaPath);
            var read = (OASISDNA)typeof(IPFSOASIS).GetMethod("ReadPersistedCatalogueDna", flags)!.Invoke(rebooted, null)!;
            Assert.Equal(RootCid, read.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);

            File.WriteAllText(pointerPath, "not-a-cid");
            var corrupt = Assert.Throws<TargetInvocationException>(() =>
                typeof(IPFSOASIS).GetMethod("ReadPersistedCatalogueDna", flags)!.Invoke(rebooted, null));
            Assert.IsType<InvalidDataException>(corrupt.InnerException);
        }
        finally
        {
            api.SetValue(null, prior);
            CryptographicOperations.ZeroMemory(key);
            OASISDNAManager.OASISDNA = priorDna;
            OASISDNAManager.OASISDNAPath = priorPath;
            Directory.Delete(folder, recursive: true); // owned mock-only configuration; no native CID/data
        }
    }

    [Fact]
    public void WithoutAPointerPathTheDnaRootIsUnchanged()
    {
        var settings = new IPFSOASISSettings { LookUpIPFSAddress = SeedCid };
        IPFSCatalogueRootPointer.Apply(settings);
        IPFSCatalogueRootPointer.Write(settings, RootCid);
        Assert.Equal(SeedCid, settings.LookUpIPFSAddress);
    }

    private static OASISDNA Dna(string pointerPath, string seed) => new()
    {
        OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            StorageProviders = new StorageProviderSettings
            {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = "http://127.0.0.1:1", CatalogueRootPointerPath = pointerPath, LookUpIPFSAddress = seed }
            }
        }
    };

    private static string OwnedFolder(string name)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root!.FullName, "TestResults", name);
        Assert.False(Directory.Exists(folder));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private sealed class Transport : HttpMessageHandler
    {
        private int _adds;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.EndsWith("/add", request.RequestUri!.AbsolutePath);
            var cid = ++_adds == 1 ? RecordCid : RootCid;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"Name\":\"\",\"Hash\":\"" + cid + "\",\"Size\":\"32\"}\n") });
        }
    }
}
