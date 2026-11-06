using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests;

[TestClass]
public class GarnetWindowsPublicCqlNativeTests
{
    [TestMethod]
    [TestCategory("GarnetWindowsPublicCqlNative")]
    public async Task DedicatedTlsReaderQueriesRealEncryptedTeachingRecords()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("Explicit disposable fixture setting required: " + name);
        Assert.IsTrue(OperatingSystem.IsWindows(), "This test owns a disposable Windows fixture only.");
        Assert.IsTrue(int.TryParse(Required("COSMIC_GARNET_WINDOWS_PORT"), out var port) && port is >= 49152 and <= 65535);
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(Required("COSMIC_GARNET_WINDOWS_CERT"));
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        ConfigurationOptions Options(string user, string password) => new()
        {
            EndPoints = { endpoint }, Ssl = true, SslHost = "localhost", User = user, Password = password,
            AllowAdmin = user == "fixture-admin", DefaultDatabase = 0, TieBreaker = "",
            AbortOnConnectFail = true, ConnectRetry = 0, BacklogPolicy = BacklogPolicy.FailFast,
            BeforeSocketConnect = (destination, _, _) =>
            {
                if (!endpoint.Equals(destination)) throw new InvalidOperationException("Unexpected native endpoint.");
            },
            SslClientAuthenticationOptions = host => new SslClientAuthenticationOptions
            {
                TargetHost = host,
                CertificateChainPolicy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    CustomTrustStore = { certificate }
                }
            }
        };
        var admin = Options("fixture-admin", Required("COSMIC_GARNET_WINDOWS_ADMIN"));
        var reader = Options("teaching-reader", Required("COSMIC_GARNET_WINDOWS_READER"));
        using var native = await ConnectionMultiplexer.ConnectAsync(admin);
        var server = native.GetServer(endpoint);
        var version = (await server.InfoAsync("server")).SelectMany(group => group)
            .Single(pair => pair.Key == "garnet_version").Value;
        StringAssert.Contains(version, "2.2.5");
        Assert.AreEqual(0L, await server.DatabaseSizeAsync(0), "Only a new empty test store may be seeded.");
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid())
            .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var actor = Guid.NewGuid();
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var writer = new GarnetOASIS(admin, () => (byte[])key.Clone(), ids[0]);
            var activated = await writer.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            for (var index = 0; index < 4; index++)
            {
                var saved = await writer.SaveHolonAsync(new Holon
                {
                    Id = ids[index], Name = "Garnet Windows teaching record " + index,
                    IsPublic = true, IsNewHolon = true, CreatedByAvatarId = actor
                }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.AreEqual(1, saved.SavedCount);
            }
            var deleted = await writer.DeleteHolonSoftAsync(ids[3]);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result);

            async Task<string[]> Snapshot()
            {
                var database = native.GetDatabase(0);
                var records = new List<string>();
                await foreach (var item in server.KeysAsync(0, pageSize: 32))
                {
                    Assert.IsTrue(records.Count < 64, "The disposable source must remain bounded.");
                    var stored = (byte[]?)await database.StringGetAsync(item);
                    Assert.IsNotNull(stored);
                    records.Add(item + ":" + Convert.ToHexString(SHA256.HashData(stored)));
                }
                return records.OrderBy(item => item, StringComparer.Ordinal).ToArray();
            }

            var before = await Snapshot();
            Assert.IsTrue(before.Length >= 4);
            using var source = new GarnetOASIS(reader, () => (byte[])key.Clone(), ids[0]);
            var verified = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(verified.IsError, verified.Exception?.ToString() ?? verified.Message);
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GarnetOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.GarnetOASIS, ids).Result).Result;

            async Task<Guid[]> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.GarnetOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.GarnetOASIS, source.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await source.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                Assert.IsTrue(result.Result.All(row => row.CreatedByAvatarId == actor && row.IsPublic && !row.Children.Any()));
                return result.Result.Select(row => row.Id).ToArray();
            }

            CollectionAssert.AreEqual(ids[..3], await Read(query));
            var subset = ids[..2];
            CollectionAssert.AreEqual(subset, await Read(query.Where(row => subset.Contains(row.Id)).Result));
            var excluded = ids[3..];
            Assert.AreEqual(0, (await Read(query.Where(row => excluded.Contains(row.Id)).Result)).Length);
            CollectionAssert.AreEqual(new[] { ids[1] }, await Read(query.Skip(1).Result.Take(1).Result));
            CollectionAssert.AreEqual(before, await Snapshot());
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
