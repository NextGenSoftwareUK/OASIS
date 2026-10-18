using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private async Task<double> ReadNativeBalanceAsync(string address)
    {
        var result = await InvokePolkadotSdkAsync(new { operation = "balance", endpoint = _rpcEndpoint, genesisHash = ReportedGenesisHash, address });
        return ParseNativeBalance(result);
    }

    private async Task<JsonElement> InvokePolkadotSdkAsync(object request)
    {
        var input = JsonSerializer.Serialize(request);
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
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Polkadot SDK bridge.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(input.AsMemory(), deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token);
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            throw;
        }
        var output = await stdout;
        _ = await stderr; // SDK diagnostics are not echoed: upstream errors may contain endpoint credentials.
        using var envelope = JsonDocument.Parse(output);
        if (process.ExitCode != 0 || !envelope.RootElement.GetProperty("ok").GetBoolean())
            throw new InvalidOperationException(envelope.RootElement.GetProperty("error").GetString());
        return envelope.RootElement.GetProperty("result").Clone();
    }

    private static double ParseNativeBalance(JsonElement result)
    {
        var unitsText = result.GetProperty("units").GetString();
        if (string.IsNullOrEmpty(unitsText) || (unitsText.Length > 1 && unitsText[0] == '0')
            || !BigInteger.TryParse(unitsText, NumberStyles.None, CultureInfo.InvariantCulture, out var units)
            || units < 0 || units >= (BigInteger.One << 128))
            throw new InvalidDataException("Polkadot balance is not a canonical u128 value.");
        var decimals = result.GetProperty("decimals").GetInt32();
        if (decimals < 0 || decimals > 38 || !IsGenesisHash(result.GetProperty("blockHash").GetString()))
            throw new InvalidDataException("Polkadot balance precision or finalized block identity is invalid.");
        // The existing wallet interface returns double, so display precision is necessarily approximate.
        // Preserve exact native units through SDK decoding; do not truncate via Int64 or assume DOT precision.
        return (double)units / Math.Pow(10, decimals);
    }
}
