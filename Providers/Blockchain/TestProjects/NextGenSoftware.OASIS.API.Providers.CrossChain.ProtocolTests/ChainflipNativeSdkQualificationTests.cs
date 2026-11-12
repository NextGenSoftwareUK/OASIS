using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class ChainflipNativeSdkQualificationTests
{
    private sealed class Capture(string directory) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker transport = new(NativeConnectionGuard.CreateGuardedHttpHandler());
        public List<object> Observations { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Content);
            Assert.AreEqual("chainflip-swap.chainflip.io", request.RequestUri.Host);
            Assert.IsNull(request.Headers.Authorization); Assert.IsFalse(request.Headers.Contains("Cookie"));
            Assert.IsFalse(request.Headers.Contains("User-Agent"));
            var id = request.RequestUri.AbsolutePath.Split('/').Last();
            Assert.IsTrue(new[] { "500000", "500001", "500002" }.Contains(id));
            var response = await transport.SendAsync(request, token);
            await response.Content.LoadIntoBufferAsync(262144, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            using (var file = new FileStream(Path.Combine(directory, "sdk-swap-" + id + ".json"), FileMode.CreateNew))
                await file.WriteAsync(bytes, token);
            Observations.Add(new { swapId = id, httpStatus = (int)response.StatusCode, bytes = bytes.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() });
            return response;
        }
        protected override void Dispose(bool disposing) { if (disposing) transport.Dispose(); base.Dispose(disposing); }
    }

    private static string RepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/Sandbox/Probe-ChainflipPublicReadiness.py")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Qualification checkout root is missing.");
    }

    [TestMethod]
    public void EvidenceDirectoryResolvesToTheActualWorktreeBeforeNativeRequests()
    {
        string root = RepositoryRoot(AppContext.BaseDirectory);
        Assert.IsTrue(Directory.Exists(Path.Combine(root, "LocalState/COSMIC")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ChainflipOASIS/ChainflipOASIS.cs")));
        Assert.IsFalse(root.EndsWith(Path.Combine("Providers", "Blockchain")));
        Assert.ThrowsException<InvalidOperationException>(() => RepositoryRoot(Path.GetPathRoot(root)));
    }

    [TestMethod]
    public async Task OriginalShippingSdkReadsExplicitHistoricalSwapsWithoutCredentialsOrDispatch()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_CHAINFLIP_NATIVE") != "original-sdk-qualification")
            Assert.Inconclusive("Explicit original production SDK qualification; never an automatic native retry.");
        var root = RepositoryRoot(AppContext.BaseDirectory);
        var directory = Path.Combine(root, "TestResults/ChainflipPublicSourceCurrent");
        Directory.CreateDirectory(directory);
        var ids = new[] { "500000", "500001", "500002" };
        using (var file = new FileStream(Path.Combine(directory, "sdk-qualification.intent.json"), FileMode.CreateNew))
            await JsonSerializer.SerializeAsync(file, new { ids, maximumCalls = 3, nativeWrites = 0,
                transport = "shipping-sdk-with-standard-native-guard", userAgentOverride = false, retryAllowed = false });
        using var handler = new Capture(directory);
        var sdk = new ChainflipOASIS.ChainflipOASIS("https://chainflip-swap.chainflip.io", handler);
        bool passed = false;
        var rows = new List<object>();
        try
        {
            foreach (var id in ids)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var result = await sdk.GetTransfersBySourceTxAsync(id, deadline.Token);
                Assert.IsFalse(result.IsError, "Original SDK read failed; original response retained, no retry.");
                Assert.AreEqual(1, result.Result.Count);
                Assert.AreEqual(id, result.Result[0].Id);
                rows.Add(result.Result[0]);
            }
            passed = true;
        }
        finally
        {
            using var file = new FileStream(Path.Combine(directory, "sdk-qualification.json"), FileMode.CreateNew);
            await JsonSerializer.SerializeAsync(file, new { passed, rows, observations = handler.Observations,
                nativeWrites = 0, credentialsUsed = false, userAgentOverride = false, retryAllowed = false });
        }
    }
}
