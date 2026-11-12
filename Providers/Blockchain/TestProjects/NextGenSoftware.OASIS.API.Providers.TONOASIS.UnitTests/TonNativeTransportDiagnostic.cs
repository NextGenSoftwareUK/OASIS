using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

/// <summary>Explicit diagnostic, not a CQL readiness or shipping acceptance test.</summary>
[TestClass]
[DoNotParallelize]
public sealed class TonNativeTransportDiagnostic
{
    private sealed class Capture(string directory) : DelegatingHandler(NativeConnectionGuard.CreateGuardedHttpHandler())
    {
        public int Count;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.IsNull(request.Headers.Authorization);
            var response = await base.SendAsync(request, token);
            int index = Count++;
            string? errorFile = null;
            int capturedBytes = 0;
            bool truncated = false;
            if (!response.IsSuccessStatusCode)
            {
                // Successful bodies remain untouched. The source throws on an
                // error status, so capture its otherwise discarded error body.
                // This handler changes neither the request nor its HTTP status.
                using var stream = await response.Content.ReadAsStreamAsync(token);
                using var body = new MemoryStream();
                var buffer = new byte[4096];
                while (true)
                {
                    int count = await stream.ReadAsync(buffer, token);
                    if (count == 0) break;
                    int retain = Math.Min(count, 16384 - (int)body.Length);
                    body.Write(buffer, 0, retain);
                    if (retain != count || body.Length == 16384) { truncated = true; break; }
                }
                errorFile = $"request-{index:D2}.error-body.bin";
                var bytes = body.ToArray();
                capturedBytes = bytes.Length;
                await File.WriteAllBytesAsync(Path.Combine(directory, errorFile), bytes, token);
            }
            await File.WriteAllTextAsync(Path.Combine(directory, $"request-{index:D2}.json"),
                JsonSerializer.Serialize(new {
                    uri = request.RequestUri!.AbsoluteUri,
                    originalUri = request.RequestUri.OriginalString,
                    method = request.Method.Method,
                    requestVersion = request.Version.ToString(),
                    responseVersion = response.Version.ToString(),
                    status = (int)response.StatusCode,
                    retryAfter = response.Headers.RetryAfter?.ToString(),
                    errorFile, capturedBytes, truncated
                }), token);
            return response;
        }
    }

    [TestMethod]
    [TestCategory("NativeTonTransportDiagnostic")]
    public async Task CaptureOneActivationWithoutReplayingTheQueryFlow()
    {
        string? sourcePath = Environment.GetEnvironmentVariable("COSMIC_TON_PUBLIC_SOURCE");
        string? directory = Environment.GetEnvironmentVariable("COSMIC_TON_TRACE_DIRECTORY");
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(directory))
            Assert.Inconclusive("Explicit retained catalogue and diagnostic output required.");
        Assert.IsTrue(Directory.Exists(directory));
        Assert.AreEqual(0, Directory.GetFiles(directory!).Length, "Retain the original diagnostic; do not replay.");

        byte[] sourceBytes = await File.ReadAllBytesAsync(sourcePath!);
        using var document = JsonDocument.Parse(sourceBytes);
        var root = document.RootElement;
        var blocks = root.GetProperty("blocks").EnumerateArray().Select(value =>
            new TonPublicHeaderReference(value.GetProperty("seqno").GetUInt32(),
                value.GetProperty("rootHash").GetString()!, value.GetProperty("fileHash").GetString()!)).ToArray();
        await File.WriteAllTextAsync(Path.Combine(directory!, "diagnostic.intent.json"),
            JsonSerializer.Serialize(new {
                sourceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(sourceBytes)).ToLowerInvariant(),
                purpose = "One activation transport observation; no query flow or writes"
            }));
        var capture = new Capture(directory!);
        using var client = new HttpClient(capture) { Timeout = TimeSpan.FromSeconds(15) };
        using var source = new TonPublicHeaderMetadataSource(client,
            new Uri(root.GetProperty("restUrl").GetString()!), blocks, root.GetProperty("sourceOwner").GetGuid());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var outcome = await source.ActivateQuerySourceAsync(deadline.Token);

        await File.WriteAllTextAsync(Path.Combine(directory!, "activation-observation.json"),
            JsonSerializer.Serialize(new {
                outcome.IsError, outcome.ErrorCode, activationSucceeded = !outcome.IsError,
                requests = capture.Count, diagnosticOnly = true, cqlReady = false,
                queryFlowExecuted = false, nativeWrites = 0
            }));
        Assert.IsTrue(capture.Count > 0, "Capture must observe an actual request.");
    }
}
