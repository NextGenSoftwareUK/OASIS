using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.IntegrationTests;

[TestClass]
public class ConvexOASISIntegrationTests
{
    [TestMethod]
    public async Task HostedCompanionAcceptsAuthenticatedActivation()
    {
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
    }
}
