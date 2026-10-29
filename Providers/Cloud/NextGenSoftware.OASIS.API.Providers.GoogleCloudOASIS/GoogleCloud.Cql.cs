using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.GoogleCloudOASIS;
    public CosmicQueryCapabilities QueryCapabilities => FirestoreCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            // Capability and access admission precede activation or native I/O.
            // Only selected native document keys are fetched; no collection scan.
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Google Firestore target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = FirestoreCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated || _firestoreDb == null)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Firestore provider before querying.");

            var rows = new List<IHolon>();
            if (selection.Result.Count != 0)
            {
                var references = selection.Result.Select(id => _firestoreDb.Collection("holons").Document(id.ToString("D"))).ToArray();
                var snapshots = await _firestoreDb.GetAllSnapshotsAsync(references, cancellationToken).ConfigureAwait(false);
                // SDK contract includes a snapshot for absent records and preserves
                // request order. Reject incomplete or foreign acknowledgements.
                if (snapshots == null || snapshots.Count != references.Length)
                    throw new InvalidDataException("Native document batch acknowledgement is incomplete.");
                var payloadBytes = 0;
                for (var index = 0; index < references.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var snapshot = snapshots[index];
                    if (snapshot == null || snapshot.Reference.Path != references[index].Path)
                        throw new InvalidDataException("Native document batch returned an unexpected identity.");
                    if (!snapshot.Exists) continue;
                    var fields = snapshot.ToDictionary();
                    if (!fields.TryGetValue("data_json", out var value) || value is not string json)
                        throw new InvalidDataException("Native canonical holon payload is absent.");
                    payloadBytes = checked(payloadBytes + Encoding.UTF8.GetByteCount(json));
                    if (payloadBytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "The complete canonical response exceeds 4 MiB.");
                    var row = DecodeHolon(fields, snapshot.Id);
                    if (row.Id != selection.Result[index] || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native payload differs from the authorized identity.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }

            // No managed business predicate remains; only visibility and paging.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause)
        { return Error("CQL_QUERY_CANCELLED", "Firestore query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Firestore key query failed; no partial result published.", cause); }
    }
}
