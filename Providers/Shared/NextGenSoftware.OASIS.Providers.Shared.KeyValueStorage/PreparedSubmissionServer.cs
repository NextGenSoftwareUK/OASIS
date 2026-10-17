using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Ordered original preparation acknowledgements. Persistence/identity validation belongs to the supplied owner.</summary>
public sealed class PreparedSubmissionServer : IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly Func<JsonElement, CancellationToken, Task> _persist;
    private readonly string _expectedTransactionId;
    private readonly string[] _expectedStages;
    private bool _optionalFirstStage;
    private string[] _preparedIds = Array.Empty<string>();
    private int _started;
    private int _maximumRequests;
    public bool IsBoundedRequestSequence=>_maximumRequests>0;
    public bool SequenceCompleted {get;private set;}
    public string Endpoint { get; }
    public string PreparedTransactionId { get; private set; }
    public IReadOnlyList<string> PreparedTransactionIds => Array.AsReadOnly(Volatile.Read(ref _preparedIds));
    public bool HasOptionalApprovalSequence(string approvalId,string mutationId)
        => _optionalFirstStage && _expectedStages[0]==approvalId && _expectedStages[1]==mutationId;
    /// <summary>Unknown original IDs, bounded physical requests and an explicit count-matched completion frame.
    /// Each request is independently verified/persisted by its supplied execution owner.</summary>
    public static PreparedSubmissionServer ForBoundedRequests(Func<JsonElement,CancellationToken,Task> persist,int maximumRequests)
    {
        if(maximumRequests<1||maximumRequests>512)throw new ArgumentOutOfRangeException(nameof(maximumRequests));
        return new PreparedSubmissionServer(persist){_maximumRequests=maximumRequests};
    }
    /// <summary>Each stage uses a separate connection and is acknowledged only after its own durable persistence.</summary>
    public static PreparedSubmissionServer ForStages(Func<JsonElement,CancellationToken,Task> persist, params string[] expectedTransactionIds)
    {
        if(expectedTransactionIds == null || expectedTransactionIds.Length < 2 || expectedTransactionIds.Length > 16
            || expectedTransactionIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 256)
            || expectedTransactionIds.Distinct(StringComparer.Ordinal).Count() != expectedTransactionIds.Length)
            throw new ArgumentException("Two to sixteen distinct bounded original stage identities required.",nameof(expectedTransactionIds));
        return new PreparedSubmissionServer(persist,expectedTransactionIds);
    }
    /// <summary>Only the first stage may be omitted; receiving the final identity closes the sequence.</summary>
    public static PreparedSubmissionServer ForOptionalApproval(Func<JsonElement,CancellationToken,Task> persist,string approvalId,string mutationId)
    {
        var server=ForStages(persist,approvalId,mutationId);
        server._optionalFirstStage=true;
        return server;
    }
    private PreparedSubmissionServer(Func<JsonElement,CancellationToken,Task> persist,string[] stages)
        : this(persist,stages[0]) { _expectedStages = (string[])stages.Clone(); }
    public void ConfigureChild(System.Diagnostics.ProcessStartInfo child, string preparationModulePath)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (!Path.IsPathFullyQualified(preparationModulePath) || !File.Exists(preparationModulePath))
            throw new ArgumentException("Installed preparation transport module must exist at an absolute path.", nameof(preparationModulePath));
        if (child.UseShellExecute) throw new ArgumentException("Preparation requires a directly owned child process.", nameof(child));
        child.Environment["OASIS_NATIVE_PREPARATION_PIPE"] = Endpoint;
        child.Environment["OASIS_NATIVE_PREPARATION_MODULE"] = preparationModulePath;
    }
    public PreparedSubmissionServer(Func<JsonElement, CancellationToken, Task> persist) : this(persist,(string)null) { }
    public PreparedSubmissionServer(Func<JsonElement, CancellationToken, Task> persist, string expectedTransactionId)
    {
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        if(expectedTransactionId != null && (string.IsNullOrWhiteSpace(expectedTransactionId) || expectedTransactionId.Length > 256))
            throw new ArgumentException("Expected original transaction identity must be bounded and nonempty.",nameof(expectedTransactionId));
        _expectedTransactionId = expectedTransactionId;
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
            if(IsBoundedRequestSequence){
                while(!SequenceCompleted){
                    await ServeStageAsync(null,null,cancellationToken);
                    if(!SequenceCompleted)await DisconnectStageAsync(cancellationToken);
                }
                return;
            }
            for(var stage=0;stage<(_expectedStages?.Length ?? 1);stage++)
            {
                await ServeStageAsync(_expectedStages?[stage] ?? _expectedTransactionId,
                    _optionalFirstStage && stage==0 ? _expectedStages[1] : null,cancellationToken);
                if(_optionalFirstStage && stage==0 && PreparedTransactionId==_expectedStages[1])break;
                if(stage+1<(_expectedStages?.Length ?? 1))
                {
                    // The client closes only after reading its acknowledgement; do not discard buffered acknowledgement bytes.
                    await DisconnectStageAsync(cancellationToken);
                }
            }
        }
        finally { _pipe.Dispose(); }
    }
    private async Task DisconnectStageAsync(CancellationToken token)
    {
        if(await _pipe.ReadAsync(new byte[1],token)!=0)
            throw new InvalidDataException("Preparation stage requires exactly one frame per connection.");
        _pipe.Disconnect();
    }
    private async Task ServeStageAsync(string expectedId,string optionalFinalId,CancellationToken cancellationToken)
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
            if(root.TryGetProperty("completed",out var completed)){
                if(!IsBoundedRequestSequence||completed.ValueKind!=JsonValueKind.True
                    ||root.GetProperty("preparedCount").GetInt32()!=PreparedTransactionIds.Count||root.TryGetProperty("evidence",out _))
                    throw new InvalidDataException("Bounded preparation completion must match the exact retained request count.");
                var reply=JsonSerializer.SerializeToUtf8Bytes(new {protocol="oasis.native-preparation.v1",persisted=true,completed=true,preparedCount=PreparedTransactionIds.Count});
                await _pipe.WriteAsync(reply,cancellationToken);await _pipe.WriteAsync(new byte[]{10},cancellationToken);await _pipe.FlushAsync(cancellationToken);
                SequenceCompleted=true;return;
            }
            if(IsBoundedRequestSequence&&PreparedTransactionIds.Count>=_maximumRequests)
                throw new InvalidDataException("Bounded physical preparation request count exceeded.");
            var evidence = root.GetProperty("evidence");
            var hash = evidence.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash) || hash.Length > 256) throw new InvalidDataException("Original preparation identity is required.");
            if(IsBoundedRequestSequence&&PreparedTransactionIds.Contains(hash))
                throw new InvalidDataException("A physical request identity cannot be reused within its preparation sequence.");
            if(expectedId != null && hash != expectedId && hash != optionalFinalId)
                throw new InvalidDataException("Prepared identity does not match the expected original transaction.");
            var encoded = evidence.GetProperty("signedTransactionBase64").GetString();
            byte[] signedBytes;
            try { signedBytes = Convert.FromBase64String(encoded ?? ""); }
            catch (FormatException ex) { throw new InvalidDataException("Prepared signed bytes must be canonical base64.", ex); }
            if (signedBytes.Length == 0 || Convert.ToBase64String(signedBytes) != encoded)
                throw new InvalidDataException("Prepared signed bytes must be nonempty canonical base64.");
            await _persist(evidence.Clone(), cancellationToken);
            PreparedTransactionId = hash;
            Volatile.Write(ref _preparedIds,Volatile.Read(ref _preparedIds).Append(hash).ToArray());
            cancellationToken.ThrowIfCancellationRequested();
            var ack = JsonSerializer.SerializeToUtf8Bytes(new { protocol = "oasis.native-preparation.v1", persisted = true, transactionHash = hash });
            await _pipe.WriteAsync(ack, cancellationToken);
            await _pipe.WriteAsync(new byte[] { 10 }, cancellationToken);
            await _pipe.FlushAsync(cancellationToken);
    }
    public void Dispose() => _pipe.Dispose();
}
