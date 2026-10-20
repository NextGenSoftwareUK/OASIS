using System;
using System.IO;
using System.Threading;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoAtomicHolonWrite
{
    internal IAtomicHolonWritePlan Plan { get; }
    internal Guid ActorId { get; }
    internal Func<string> CaptureReceipt { get; }
    internal Action<string> ReplayReceipt { get; }
    internal CancellationToken CancellationToken { get; }
    internal Func<string> CaptureReplicationPayload {get;}
    internal MongoAtomicHolonWrite(IAtomicHolonWritePlan plan, Guid actorId, Func<string> capture, Action<string> replay, CancellationToken cancellationToken,
        Func<string> captureReplicationPayload=null)
    { Plan = plan; ActorId = actorId; CaptureReceipt = capture; ReplayReceipt = replay; CancellationToken = cancellationToken;CaptureReplicationPayload=captureReplicationPayload; }
}

internal sealed class MongoConditionalReceiptException(string message, Exception cause) : IOException(message, cause);
internal sealed class MongoConditionalIntentException() : InvalidOperationException("This operation ID is bound to different content or actor.");
