using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>Explicit local TVM configuration; no public-network fallback.</summary>
public sealed record TonSandboxStorageOptions(string NodeExecutable, string WorkerScript,
    string CompiledBinding, string SourceDirectory, Guid SourceId, string ExpectedCodeHash, bool ReadOnly = true);

public sealed class TonStorageException : Exception
{
    public string Code { get; }
    public Guid? OperationId { get; }
    public Guid? SourceId { get; }
    public TonStorageException(string code, string message, Guid? operationId = null, Guid? sourceId = null) : base(message)
    { Code = code; OperationId = operationId; SourceId = sourceId; }
}

public sealed class TonMutationOutcomeUnknownException : Exception
{
    public Guid OperationId { get; }
    public Guid SourceId { get; }
    public TonMutationOutcomeUnknownException(Guid operationId, Guid sourceId, Exception cause)
        : base("Native TVM mutation outcome is uncertain; inspect its exact operation before another submission.", cause)
    { OperationId = operationId; SourceId = sourceId; }
}

/// <summary>
/// Process-isolated native TVM backend. The shared private-storage owner must
/// encrypt logical keys/values before this backend is used for holons.
/// </summary>
public sealed class TonSandboxStorageBackend : IKeyValueBackend
{
    private const string Mode = "LocalTonTvmSandbox";
    private readonly TonSandboxStorageOptions _options;
    private readonly SemaphoreSlim _execution = new(1, 1);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public TonSandboxStorageBackend(TonSandboxStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SourceId == Guid.Empty || options.ExpectedCodeHash == null || options.ExpectedCodeHash.Length != 64
            || options.ExpectedCodeHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Explicit native source and lowercase code identity required.", nameof(options));
        foreach (var value in new[] { options.NodeExecutable, options.WorkerScript, options.CompiledBinding, options.SourceDirectory })
            if (string.IsNullOrEmpty(value) || !Path.IsPathFullyQualified(value))
                throw new ArgumentException("All native runtime and source paths must be absolute.", nameof(options));
        _options = options;
    }

    public Task<JsonElement> CreateSourceAsync(CancellationToken token = default)
        => RunAsync("create", mutation: true, token: token);
    public Task<JsonElement> InspectOperationAsync(Guid operationId, CancellationToken token = default)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("Exact operation identity required.", nameof(operationId));
        return RunAsync("inspect", operationId: operationId, token: token);
    }
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync("get", key: key, token: cancellationToken).ConfigureAwait(false);
        return result.ValueKind == JsonValueKind.Null ? null : result.GetString();
    }
    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => _ = await RunAsync("put", key: key, value: value, mutation: true, token: cancellationToken).ConfigureAwait(false);
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _ = await RunAsync("delete", key: key, mutation: true, token: cancellationToken).ConfigureAwait(false);
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
        => _ = await RunAsync("verify", token: cancellationToken).ConfigureAwait(false);
    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var result = await RunAsync("list", prefix: prefix, token: cancellationToken).ConfigureAwait(false);
        var keys = result.EnumerateArray().Select(item => item.GetString()).ToArray();
        if (keys.Length > 1000 || keys.Any(key => key == null || !key.StartsWith(prefix, StringComparison.Ordinal))
            || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new InvalidDataException("Native key enumeration returned inconsistent identities.");
        return Array.AsReadOnly(keys);
    }

    private async Task<JsonElement> RunAsync(string operation, string key = null, string value = null,
        string prefix = null, bool mutation = false, Guid? operationId = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (mutation && _options.ReadOnly) throw new TonStorageException("TON_READ_ONLY", "This source is admitted for reads only.");
        if (key != null) StrictUtf8.GetByteCount(key);
        if (value != null) StrictUtf8.GetByteCount(value);
        operationId ??= mutation ? Guid.NewGuid() : null;
        await _execution.WaitAsync(token).ConfigureAwait(false);
        try
        {
            // Share.None is an OS-owned cross-client lease, released when this
            // process/handle exits. SDK pending intent remains the write fence.
            using var ownership = new FileStream(Path.Combine(_options.SourceDirectory, "source.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var start = new ProcessStartInfo(_options.NodeExecutable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardInputEncoding = StrictUtf8, StandardOutputEncoding = StrictUtf8, StandardErrorEncoding = StrictUtf8
            };
            foreach (var argument in new[] { _options.WorkerScript, _options.CompiledBinding, _options.SourceDirectory,
                _options.SourceId.ToString("D"), _options.ExpectedCodeHash }) start.ArgumentList.Add(argument);
            start.Environment["NODE_PATH"] = Path.Combine(Path.GetDirectoryName(_options.WorkerScript), "node_modules");
            using var process = new Process { StartInfo = start };
            var dispatched = false;
            try
            {
                if (!process.Start()) throw new InvalidOperationException("Could not start native TVM worker.");
                dispatched = true;
                var stdout = ReadBoundedAsync(process.StandardOutput, 4 * 1024 * 1024, deadline.Token);
                var stderr = ReadBoundedAsync(process.StandardError, 16 * 1024, deadline.Token);
                var request = JsonSerializer.Serialize(new { mode = Mode, sourceId = _options.SourceId.ToString("D"),
                    operation, operationId = operationId?.ToString("D"), key, value, prefix });
                await process.StandardInput.WriteAsync(request.AsMemory(), deadline.Token).ConfigureAwait(false);
                process.StandardInput.Close();
                await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(deadline.Token)).ConfigureAwait(false);
                using var response = JsonDocument.Parse(await stdout.ConfigureAwait(false));
                var root = response.RootElement;
                if (!root.GetProperty("ok").GetBoolean())
                {
                    var error = root.GetProperty("error");
                    throw new TonStorageException(error.GetProperty("code").GetString(), error.GetProperty("message").GetString(),
                        operationId, _options.SourceId);
                }
                if (process.ExitCode != 0 || root.GetProperty("mode").GetString() != Mode
                    || root.GetProperty("sourceId").GetString() != _options.SourceId.ToString("D"))
                    throw new InvalidDataException("Native worker response has a different source or execution outcome.");
                var result = root.GetProperty("result");
                if (mutation && (result.GetProperty("operationId").GetString() != operationId.Value.ToString("D")
                    || result.GetProperty("sourceId").GetString() != _options.SourceId.ToString("D")
                    || result.GetProperty("codeHash").GetString() != _options.ExpectedCodeHash))
                    throw new InvalidDataException("Native acknowledgement belongs to another physical write.");
                return result.Clone();
            }
            catch (Exception error)
            {
                if (dispatched && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                if (mutation && dispatched && error is not TonStorageException)
                    throw new TonMutationOutcomeUnknownException(operationId.Value, _options.SourceId, error);
                if (error is OperationCanceledException && token.IsCancellationRequested)
                    throw new OperationCanceledException(token);
                throw;
            }
        }
        finally { _execution.Release(); }
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, int maximum, CancellationToken token)
    {
        var output = new StringBuilder();
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
        {
            if (output.Length + count > maximum) throw new InvalidDataException("Native worker response budget exceeded.");
            output.Append(buffer, 0, count);
        }
        return output.ToString();
    }
}
