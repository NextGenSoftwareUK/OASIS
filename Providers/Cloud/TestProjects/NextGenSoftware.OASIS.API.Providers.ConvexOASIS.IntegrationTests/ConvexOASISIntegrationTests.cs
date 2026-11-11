using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.IntegrationTests;

[TestClass]
public class ConvexOASISIntegrationTests
{
    [TestMethod]
    public async Task ExistingProviderActivatesAfterCompanionIsDeployed()
    {
        if (Environment.GetEnvironmentVariable("OASIS_CONVEX_LOCAL_RECOVERY") != "1")
            Assert.Inconclusive("Enable only for a fresh disposable Convex engine.");

        var deployment = Environment.GetEnvironmentVariable("CONVEX_SELF_HOSTED_URL");
        var deployKey = Environment.GetEnvironmentVariable("CONVEX_SELF_HOSTED_ADMIN_KEY");
        var companionPath = Environment.GetEnvironmentVariable("OASIS_CONVEX_COMPANION_DIRECTORY");
        Assert.IsTrue(Uri.TryCreate(deployment, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback &&
            endpoint.Scheme == Uri.UriSchemeHttp && endpoint.AbsolutePath == "/" &&
            string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) && string.IsNullOrEmpty(endpoint.Fragment));
        Assert.IsFalse(string.IsNullOrWhiteSpace(deployKey));
        Assert.IsTrue(Directory.Exists(companionPath));

        using var provider = new ConvexOASIS(deployment!, deployKey!);
        var absent = await provider.ActivateProviderAsync();
        Assert.IsTrue(absent.IsError && !provider.IsProviderActivated, "The fresh engine must begin without the companion.");

        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = companionPath!,
            UseShellExecute = false
        };
        start.ArgumentList.Add(Path.Combine(companionPath!, "node_modules", "convex", "bin", "main.js"));
        start.ArgumentList.Add("deploy");
        start.ArgumentList.Add("--yes");
        using var deploy = Process.Start(start) ?? throw new InvalidOperationException("Could not start the pinned Convex CLI.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await deploy.WaitForExitAsync(deadline.Token);
        }
        finally
        {
            if (!deploy.HasExited)
                deploy.Kill(entireProcessTree: true);
        }
        Assert.AreEqual(0, deploy.ExitCode, "Companion deployment failed.");

        var active = await provider.ActivateProviderAsync();
        Assert.IsTrue(active.Result && !active.IsError && provider.IsProviderActivated, active.Exception?.GetType().FullName);
        var inactive = await provider.DeActivateProviderAsync();
        Assert.IsTrue(inactive.Result && !inactive.IsError && !provider.IsProviderActivated);
    }

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
            endpoint.Scheme == Uri.UriSchemeHttps && !endpoint.IsLoopback &&
            endpoint.AbsolutePath == "/" && string.IsNullOrEmpty(endpoint.UserInfo) &&
            string.IsNullOrEmpty(endpoint.Query) && string.IsNullOrEmpty(endpoint.Fragment),
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
        Console.WriteLine($"Convex hosted fixture ID: {id:D}");
        var acknowledged = false;
        try
        {
            var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(saved.IsSaved && !saved.IsError, saved.Exception?.GetType().FullName);
            acknowledged = true;
            using var reader = new ConvexOASIS(deployment!, deployKey!);
            var readerActivation = await reader.ActivateProviderAsync();
            Assert.IsTrue(readerActivation.Result, readerActivation.Exception?.GetType().FullName);
            var loaded = await reader.LoadHolonAsync(id, loadChildren: false);
            Assert.IsTrue(loaded.IsLoaded, loaded.Exception?.GetType().FullName);
            Assert.AreEqual(id, loaded.Result.Id);
            Assert.AreEqual(holon.Name, loaded.Result.Name);
        }
        finally
        {
            if (acknowledged)
            {
                var removed = await provider.DeleteHolonSoftAsync(id, softDelete: false);
                Assert.IsTrue(removed.Result && removed.IsDeleted && !removed.IsError,
                    removed.Exception?.GetType().FullName);
                using var observer = new ConvexOASIS(deployment!, deployKey!);
                var observerActivation = await observer.ActivateProviderAsync();
                Assert.IsTrue(observerActivation.Result && !observerActivation.IsError,
                    observerActivation.Exception?.GetType().FullName);
                var absent = await observer.LoadHolonAsync(id, loadChildren: false);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
                Assert.IsFalse(absent.IsLoaded);
                Assert.IsNull(absent.Result);
            }
        }
    }
}
