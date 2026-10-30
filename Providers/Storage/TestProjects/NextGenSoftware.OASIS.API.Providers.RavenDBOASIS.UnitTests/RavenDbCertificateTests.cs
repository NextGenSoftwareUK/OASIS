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
        var provider = new RavenDBOASIS(new[] { "https://localhost:1" }, "oasis", certificate);
        var getStore = typeof(RavenDBOASIS).GetMethod("GetStore", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = (IDocumentStore)getStore.Invoke(provider, null)!;
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
}
