using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.IntegrationTests;

[TestClass]
public class ConvexOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HostedCompanionAcceptsAuthenticatedStorage(bool verifyWrites)
    {
        if (verifyWrites && Environment.GetEnvironmentVariable("OASIS_CONVEX_HOSTED_TEST_WRITES") != "1")
            Assert.Inconclusive("Enable writes only for the dedicated disposable hosted Convex deployment.");

        var deployment = Environment.GetEnvironmentVariable("OASIS_CONVEX_HOSTED_TEST_URL");
        if (string.IsNullOrWhiteSpace(deployment))
            Assert.Inconclusive("Configure the dedicated hosted Convex test deployment.");

        Assert.IsTrue(Uri.TryCreate(deployment, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps && !endpoint.IsLoopback,
            "Hosted acceptance requires an explicit HTTPS deployment origin.");
        var deployKey = Environment.GetEnvironmentVariable("OASIS_CONVEX_HOSTED_TEST_DEPLOY_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(deployKey), "Configure the protected test deploy key.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_CONVEX_STORAGE_KEY")),
            "Configure the protected storage encryption key for this test deployment.");

        using var provider = new ConvexOASIS(deployment!, deployKey!);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.GetType().FullName);
        Assert.IsTrue(activation.Result);
        Assert.IsTrue(provider.IsProviderActivated);
        if (!verifyWrites)
            return;

        var id = Guid.NewGuid();
        var holon = new Holon { Id = id, Name = $"convex-hosted-{id:N}" };
        try
        {
            var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(saved.IsSaved, saved.Exception?.GetType().FullName);
            var loaded = await provider.LoadHolonAsync(id, loadChildren: false);
            Assert.IsTrue(loaded.IsLoaded, loaded.Exception?.GetType().FullName);
            Assert.AreEqual(id, loaded.Result.Id);
            Assert.AreEqual(holon.Name, loaded.Result.Name);
        }
        finally
        {
            var removed = await provider.DeleteHolonAsync(id);
            Assert.IsFalse(removed.IsError, removed.Exception?.GetType().FullName);
            var absent = await provider.LoadHolonAsync(id, loadChildren: false);
            Assert.IsFalse(absent.IsLoaded);
            Assert.IsNull(absent.Result);
        }
    }
}
