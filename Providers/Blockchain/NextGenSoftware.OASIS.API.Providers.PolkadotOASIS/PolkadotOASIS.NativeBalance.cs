using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private async Task<double> ReadNativeBalanceAsync(string address)
    {
        var result = await InvokePolkadotSdkAsync(new { operation = "balance", endpoint = _rpcEndpoint, genesisHash = ReportedGenesisHash, address });
        return ParseNativeBalance(result);
    }

    private async Task<JsonElement> InvokePolkadotSdkAsync(object request, CancellationToken cancellationToken = default,
        PreparedSubmissionServer preparationServer = null)
    {
        var input = JsonSerializer.Serialize(request);
        using var requestDocument = JsonDocument.Parse(input);
        var submitsOriginal = requestDocument.RootElement.GetProperty("operation").GetString() is "submitNativePreparation" or "submitAssetPreparation";
        if (submitsOriginal && preparationServer == null)
            throw new InvalidOperationException("Original submission requires an owned durable preparation server.");
        var bridge = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "PolkadotSdkBridge", "bridge.mjs");
        if (!File.Exists(bridge)) throw new FileNotFoundException("Pinned Polkadot SDK bridge is missing; provision its locked dependencies once.", bridge);
        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("OASIS_POLKADOT_NODE") ?? "node",
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false, true),
            StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = new UTF8Encoding(false, true),
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(bridge);
        // Never inherit another operation's persistence channel.
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_PIPE");
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_MODULE");
        preparationServer?.ConfigureChild(start,Path.Combine(AppContext.BaseDirectory,"PolkadotSdkBridge","prepared-submission.mjs"));
        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Polkadot SDK bridge.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var serving=preparationServer?.ServeAsync(deadline.Token);
        var servingObserved=false;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(input.AsMemory(), deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token);
            var output = await stdout;
            _ = await stderr; // Drain diagnostics without exposing SDK request/credential material.
            using var envelope = JsonDocument.Parse(output);
            if (process.ExitCode != 0 || !envelope.RootElement.GetProperty("ok").GetBoolean()) {
                if(serving!=null){
                    // Join the owner, not a racy Task-status snapshot. Only our cancellation of an
                    // unused channel may yield to the SDK error; owner cancellation/failure survives.
                    deadline.Cancel();
                    try {await serving;}
                    catch(OperationCanceledException cancelled) when(cancelled.CancellationToken==deadline.Token&&!cancellationToken.IsCancellationRequested) { }
                    finally {servingObserved=true;}
                }
                throw new InvalidOperationException(envelope.RootElement.GetProperty("error").GetString());
            }
            var result=envelope.RootElement.GetProperty("result");
            if(serving!=null){
                await serving;servingObserved=true;
                if(preparationServer.PreparedTransactionId==null
                    ||result.GetProperty("transactionHash").GetString()!=preparationServer.PreparedTransactionId
                    ||!result.GetProperty("persisted").GetBoolean()||result.GetProperty("submitted").GetBoolean()!=submitsOriginal)
                    throw new InvalidDataException("SDK persistence reply does not bind the acknowledged original preparation.");
                if(submitsOriginal && (!result.GetProperty("poolAccepted").GetBoolean()
                    || result.GetProperty("finalized").GetBoolean() || !result.GetProperty("reconciliationRequired").GetBoolean()))
                    throw new InvalidDataException("Pool acceptance cannot be promoted to finalized completion.");
            }
            return result.Clone();
        }
        catch (OperationCanceledException cancelled) when(submitsOriginal && preparationServer.PreparedTransactionId != null)
        {
            throw new PolkadotNativeMutationCanceledException(preparationServer.PreparedTransactionId, cancelled, cancelled.CancellationToken);
        }
        catch (Exception failure) when(submitsOriginal && preparationServer.PreparedTransactionId != null)
        {
            throw new PolkadotNativeMutationReconciliationException(preparationServer.PreparedTransactionId, failure);
        }
        finally
        {
            deadline.Cancel();
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            // The primary failure remains authoritative; observe readers after the owned process exits.
            try { await Task.WhenAll(stdout, stderr); }
            catch(Exception error) when(error is IOException or DecoderFallbackException) { }
            if(serving!=null){
                try {await serving;}
                catch(OperationCanceledException) when(deadline.IsCancellationRequested) { }
                catch(Exception) when(servingObserved) { }
            }
        }
    }

    private static double ParseNativeBalance(JsonElement result)
    {
        if (!IsGenesisHash(result.GetProperty("blockHash").GetString()))
            throw new InvalidDataException("Polkadot finalized block identity is invalid.");
        return UnitsToDisplay(result.GetProperty("units").GetString(), result.GetProperty("decimals").GetInt32());
    }

    private static double UnitsToDisplay(string unitsText, int decimals)
    {
        if (string.IsNullOrEmpty(unitsText) || (unitsText.Length > 1 && unitsText[0] == '0')
            || !BigInteger.TryParse(unitsText, NumberStyles.None, CultureInfo.InvariantCulture, out var units)
            || units < 0 || units >= (BigInteger.One << 128))
            throw new InvalidDataException("Polkadot balance is not a canonical u128 value.");
        if (decimals < 0 || decimals > 38)
            throw new InvalidDataException("Polkadot balance precision is invalid.");
        // The existing wallet interface returns double, so display precision is necessarily approximate.
        // Preserve exact native units through SDK decoding; do not truncate via Int64 or assume DOT precision.
        return (double)units / Math.Pow(10, decimals);
    }
}
