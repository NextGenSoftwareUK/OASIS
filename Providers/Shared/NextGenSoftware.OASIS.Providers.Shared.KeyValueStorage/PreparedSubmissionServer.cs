using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>One original preparation acknowledgement. Persistence/identity validation belongs to the supplied owner.</summary>
public sealed class PreparedSubmissionServer : IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly Func<JsonElement, CancellationToken, Task> _persist;
    private int _started;
    public string Endpoint { get; }
    public void ConfigureChild(System.Diagnostics.ProcessStartInfo child, string preparationModulePath)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (!Path.IsPathFullyQualified(preparationModulePath) || !File.Exists(preparationModulePath))
            throw new ArgumentException("Installed preparation transport module must exist at an absolute path.", nameof(preparationModulePath));
        if (child.UseShellExecute) throw new ArgumentException("Preparation requires a directly owned child process.", nameof(child));
        child.Environment["OASIS_NATIVE_PREPARATION_PIPE"] = Endpoint;
        child.Environment["OASIS_NATIVE_PREPARATION_MODULE"] = preparationModulePath;
    }
    public PreparedSubmissionServer(Func<JsonElement, CancellationToken, Task> persist)
    {
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        var name = "oasis-native-" + Guid.NewGuid().ToString("N");
        _pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        Endpoint = OperatingSystem.IsWindows() ? @"\\.\pipe\" + name : Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + name);
    }
    public async Task ServeAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) throw new InvalidOperationException("Preparation server cannot be reused for a second submission.");
        try
        {
            await _pipe.WaitForConnectionAsync(cancellationToken);
            using var frame = new MemoryStream();
            var buffer = new byte[4096];
            while (true)
            {
                var count = await _pipe.ReadAsync(buffer, cancellationToken);
                if (count == 0) throw new EndOfStreamException("Preparation frame was not received.");
                var newline = Array.IndexOf(buffer, (byte)10, 0, count);
                if (newline >= 0 && newline != count - 1) throw new InvalidDataException("Preparation requires exactly one frame.");
                frame.Write(buffer, 0, newline >= 0 ? newline : count);
                if (frame.Length > 1048576) throw new InvalidDataException("Preparation frame exceeds limit.");
                if (newline >= 0) break;
            }
            var text = new UTF8Encoding(false, true).GetString(frame.ToArray());
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.GetProperty("protocol").GetString() != "oasis.native-preparation.v1") throw new InvalidDataException("Unknown preparation protocol.");
            var evidence = root.GetProperty("evidence");
            var hash = evidence.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash) || hash.Length > 256) throw new InvalidDataException("Original preparation identity is required.");
            var encoded = evidence.GetProperty("signedTransactionBase64").GetString();
            byte[] signedBytes;
            try { signedBytes = Convert.FromBase64String(encoded ?? ""); }
            catch (FormatException ex) { throw new InvalidDataException("Prepared signed bytes must be canonical base64.", ex); }
            if (signedBytes.Length == 0 || Convert.ToBase64String(signedBytes) != encoded)
                throw new InvalidDataException("Prepared signed bytes must be nonempty canonical base64.");
            await _persist(evidence.Clone(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var ack = JsonSerializer.SerializeToUtf8Bytes(new { protocol = "oasis.native-preparation.v1", persisted = true, transactionHash = hash });
            await _pipe.WriteAsync(ack, cancellationToken);
            await _pipe.WriteAsync(new byte[] { 10 }, cancellationToken);
            await _pipe.FlushAsync(cancellationToken);
        }
        finally { _pipe.Dispose(); }
    }
    public void Dispose() => _pipe.Dispose();
}
