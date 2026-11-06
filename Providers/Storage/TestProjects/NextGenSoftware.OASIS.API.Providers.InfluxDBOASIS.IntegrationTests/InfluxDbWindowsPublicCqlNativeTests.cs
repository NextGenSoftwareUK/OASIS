using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests;

[TestClass]
public class InfluxDbWindowsPublicCqlNativeTests
{
    private sealed record Catalogue(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId);

    [TestMethod]
    [TestCategory("InfluxDbWindowsPublicCqlNative")]
    public async Task OfficialSdkQueriesThePinnedServerThroughTheWeb4Guard()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("Explicit Windows fixture setting required: " + name);
        Assert.IsTrue(OperatingSystem.IsWindows());
        var port = int.Parse(Required("COSMIC_INFLUX_WINDOWS_PORT"), System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsTrue(port is >= 49152 and <= 65535);
        var origin = new Uri($"https://localhost:{port}");
        var catalogue = JsonSerializer.Deserialize<Catalogue>(await File.ReadAllBytesAsync(
            Required("COSMIC_INFLUX_WINDOWS_CATALOGUE")))!;
        Assert.AreEqual(3, catalogue.LiveIds.Length);
        var ids = catalogue.LiveIds.Append(catalogue.DeletedId).Append(catalogue.MissingId).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty) || catalogue.Actor == Guid.Empty);
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(Required("COSMIC_INFLUX_WINDOWS_CERT"));
        var transport = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false, UseCookies = false };
        transport.SslOptions.CertificateChainPolicy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            CustomTrustStore = { certificate }
        };
        transport.ConnectCallback = async (context, cancellationToken) =>
        {
            Assert.AreEqual("localhost", context.DnsEndPoint.Host);
            Assert.AreEqual(port, context.DnsEndPoint.Port);
            var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };
        const string organization = "cosmic-teaching";
        const string bucket = "cosmic-public";
        using var client = new HttpClient(new CqlInfluxDbReadSourceRegistration.ExactReadHandler(
            origin, organization, bucket, transport));
        var provider = new InfluxDBOASIS(origin.AbsoluteUri, Required("COSMIC_INFLUX_WINDOWS_READER"),
            organization, bucket, client, ids[0]);
        try
        {
            var active = await provider.ActivateQuerySourceAsync(default);
            Assert.IsFalse(active.IsError, active.Exception?.ToString() ?? active.Message);
            const ProviderType authority = ProviderType.InfluxDBOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(catalogue.Actor, authority, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, authority,
                    new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted &&
                    row.CreatedByAvatarId == catalogue.Actor && !row.Children.Any()));
                return result.Result.Select(row => row.Id).ToArray();
            }

            var ordered = catalogue.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered, await Read(query));
            var subset = new[] { ordered[0], ordered[2] };
            CollectionAssert.AreEqual(subset, await Read(query.Where(row => subset.Contains(row.Id)).Result));
            CollectionAssert.AreEqual(new[] { ordered[1] }, await Read(query.Skip(1).Result.Take(1).Result));
            var excluded = ids[3..];
            Assert.AreEqual(0, (await Read(query.Where(row => excluded.Contains(row.Id)).Result)).Length);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
