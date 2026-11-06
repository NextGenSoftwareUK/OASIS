using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlRavenDbHostedConfigurationTests
{
    private const string Prefix = "CQL_RAVENDB_QUERY_";
    private const string Origin = "https://a.free.teaching.ravendb.cloud";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "ORIGIN"] = Origin,
        [Prefix + "CERTIFICATE_SHA256"] = new string('a', 64),
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Fact]
    public void RegistersOnlyWhenExplicitlyConfigured()
    {
        var services = new ServiceCollection();
        services.AddRavenDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddRavenDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        Assert.Contains(services, entry => entry.ServiceType == typeof(CqlReadSourceProfile));
        Assert.Contains(services, entry => entry.ServiceType == typeof(X509Certificate2) && entry.IsKeyedService);
    }

    [Theory]
    [InlineData("ORIGIN", "https://a.free.teaching.ravendb.cloud.invalid")]
    [InlineData("ORIGIN", "https://a.free.teaching.ravendb.cloud:443")]
    [InlineData("ORIGIN", "http://a.free.teaching.ravendb.cloud")]
    [InlineData("ORIGIN", "https://user@a.free.teaching.ravendb.cloud")]
    [InlineData("CERTIFICATE_SHA256", "not-a-digest")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsInvalidSettings(string key, string value)
    {
        var settings = Settings();
        settings[Prefix + key] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddRavenDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }

    [Fact]
    public void LoadsOnlyDigestMatchedPrivateKey()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=RavenDB CQL test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var original = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        const string password = "controlled";
        var bytes = original.Export(X509ContentType.Pkcs12, password);
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => CqlRavenDbReadSourceRegistration.LoadCertificate(path, new string('0', 64), password));
            using var observed = CqlRavenDbReadSourceRegistration.LoadCertificate(path, Convert.ToHexStringLower(SHA256.HashData(bytes)), password);
            Assert.True(observed.HasPrivateKey);
            Assert.Equal(original.Thumbprint, observed.Thumbprint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            System.IO.File.Delete(path);
        }
    }
}
