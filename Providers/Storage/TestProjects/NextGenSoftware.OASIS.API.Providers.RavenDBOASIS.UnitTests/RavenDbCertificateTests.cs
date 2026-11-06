using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Client.Documents;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.UnitTests;

[TestClass]
public class RavenDbCertificateTests
{
    private static X509Certificate2 Certificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=COSMIC ephemeral client test", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
    }

    [DataTestMethod]
    [DataRow("http://127.0.0.1:1")]
    [DataRow("not-a-url")]
    public void CredentialsCannotBeConfiguredForNonHttpsEndpoints(string endpoint)
    {
        using var certificate = Certificate();
        Assert.ThrowsException<ArgumentException>(() =>
            new RavenDBOASIS(new[] { endpoint }, "oasis", certificate));
    }

    [TestMethod]
    public void PublicCertificateWithoutPrivateKeyCannotAuthenticate()
    {
        using var certificate = Certificate();
        using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.RawData);
        Assert.ThrowsException<ArgumentException>(() =>
            new RavenDBOASIS(new[] { "https://localhost:1" }, "oasis", publicOnly));
    }

    [TestMethod]
    public async Task StoreRecreationKeepsCallerOwnedCertificateAndNormalValidation()
    {
        using var certificate = Certificate();
        var endpoints = new[] { "https://localhost:1" };
        var provider = new RavenDBOASIS(endpoints, "oasis", certificate);
        endpoints[0] = "http://localhost:1";
        var getStore = typeof(RavenDBOASIS).GetMethod("GetStore", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = (IDocumentStore)getStore.Invoke(provider, null)!;
        CollectionAssert.AreEqual(new[] { "https://localhost:1" }, first.Urls);
        Assert.AreSame(certificate, first.Certificate);
        Assert.IsFalse(first.Conventions.DisposeCertificate);

        await provider.DeActivateProviderAsync();
        Assert.IsTrue(certificate.HasPrivateKey, "The provider must not dispose caller-owned credentials.");
        var second = (IDocumentStore)getStore.Invoke(provider, null)!;
        Assert.AreNotSame(first, second);
        Assert.AreSame(certificate, second.Certificate);
        await provider.DeActivateProviderAsync();
        Assert.IsTrue(certificate.HasPrivateKey);
    }

    [TestMethod]
    public void HostedSourceDisablesTopologyDiscoveryAndCache()
    {
        using var certificate = Certificate();
        using var source = new RavenDbQuerySource(new Uri("https://raven.example.test"), Guid.NewGuid(), certificate);
        var getStore = typeof(RavenDBOASIS).GetMethod("GetStore", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (IDocumentStore)getStore.Invoke(source, null)!;
        CollectionAssert.AreEqual(new[] { "https://raven.example.test" }, store.Urls);
        Assert.IsTrue(store.Conventions.DisableTopologyUpdates);
        Assert.IsTrue(store.Conventions.DisableTopologyCache);
    }
}
