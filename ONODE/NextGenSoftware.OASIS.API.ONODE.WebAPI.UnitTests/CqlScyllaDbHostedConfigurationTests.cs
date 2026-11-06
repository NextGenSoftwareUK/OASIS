using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlScyllaDbHostedConfigurationTests
{
    private const string Prefix = "CQL_SCYLLADB_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "IP"] = "8.8.8.8",
        [Prefix + "HOST"] = "node-1.aws-eu-west-1.teaching.clusters.scylla.cloud",
        [Prefix + "CA_DER_BASE64"] = "YQ==",
        [Prefix + "CA_SHA256"] = new string('a', 64),
        [Prefix + "READER_PASSWORD"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Fact]
    public async Task RegistersDigestBoundCaAndHonoursCancellationBeforeConnecting()
    {
        var services = new ServiceCollection();
        services.AddScyllaDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=ScyllaDB test CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var settings = Settings();
        settings[Prefix + "CA_DER_BASE64"] = Convert.ToBase64String(root.RawData);
        settings[Prefix + "CA_SHA256"] = Convert.ToHexStringLower(SHA256.HashData(root.RawData));
        Assert.Throws<InvalidDataException>(() =>
            CqlScyllaDbReadSourceRegistration.LoadCa(settings[Prefix + "CA_DER_BASE64"]!, new string('0', 64)));
        services.AddScyllaDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.ScyllaDBOASIS, profile.Storage.ProviderType.Value);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(profile.Storage)
            .ActivateQuerySourceAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("IP", "127.0.0.1")]
    [InlineData("IP", "169.254.169.254")]
    [InlineData("HOST", "node.clusters.scylla.cloud.invalid")]
    [InlineData("HOST", "node.clusters.scylla.cloud:9142")]
    [InlineData("CA_SHA256", "not-a-digest")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsUnreviewedSettings(string key, string value)
    {
        var settings = Settings();
        settings[Prefix + key] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddScyllaDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
