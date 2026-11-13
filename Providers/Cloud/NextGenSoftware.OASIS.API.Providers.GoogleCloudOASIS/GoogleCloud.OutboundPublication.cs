using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Native = Google.Cloud.Firestore.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private const string FirestoreOutboxCollection = "cosmicOutboundDeliveries";
    private const string FirestoreOutboxSequenceCollection = "cosmicOutboundState";

    private static DocumentReference FirestoreOutboxDelivery(FirestoreDb database, long sequence)
        => database.Collection(FirestoreOutboxCollection).Document(sequence.ToString("D19", CultureInfo.InvariantCulture));

    private static async Task StageFirestoreOutboundDeliveriesAsync(FirestoreDb database,
        ByteString transaction, List<Native.Write> writes,
        ConditionalHolonPlan plan, IEnumerable<IHolon> acknowledged, CancellationToken cancellationToken)
    {
        var enrollment = plan.ReplicationEnrollment;
        var counter = database.Collection(FirestoreOutboxSequenceCollection).Document("sequence");
        var observed = await ReadFirestoreTransactionDocumentAsync(counter, transaction, new[] { "sequence" }, cancellationToken).ConfigureAwait(false);
        long sequence = 0;
        if (observed != null && (!observed.Fields.TryGetValue("sequence", out var value)
            || value.ValueTypeCase != Native.Value.ValueTypeOneofCase.IntegerValue
            || (sequence = value.IntegerValue) <= 0))
            throw new InvalidDataException("The Firestore outbox sequence is invalid.");

        var payload = CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(
            plan.OperationId, enrollment.SourceId, plan.Authority, plan.ActorId, plan.Operation,
            acknowledged, plan.SaveChildren, plan.Recursive, plan.MaxChildDepth, enrollment.NativeRoutes));
        var digest = HolonRecoveryEvidenceJson.Digest(payload);

        foreach (var route in enrollment.NativeRoutes)
        {
            sequence = checked(sequence + 1);
            var reference = FirestoreOutboxDelivery(database, sequence);
            var fields = new Dictionary<string, object>
            {
                ["sequence"] = sequence,
                ["operationId"] = plan.OperationId.ToString("D"),
                ["targetId"] = route.TargetId,
                ["payload"] = payload,
                ["digest"] = digest,
                ["state"] = nameof(HolonDeliveryState.Pending),
                ["attemptCount"] = 0L
            };
            writes.Add(FirestoreReplicaDocumentWrite(reference, fields, null));
        }

        writes.Add(FirestoreReplicaDocumentWrite(counter, new() { ["sequence"] = sequence }, observed));
    }
}
