using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS
{
    public partial class SupabaseOASIS : ICosmicStorageIdentityProvider, ICommittedHolonReplicaReceiptStorageProvider
    {
        public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                    Message = "Activate Supabase before enrolling it for replication." };

            try
            {
                using var request = CreatePostgrestRequest(HttpMethod.Get, "oasis_storage_identity?select=slot,store_id&limit=2");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
                var rows = document.RootElement;
                if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1
                    || !rows[0].TryGetProperty("slot", out var slot) || !slot.TryGetInt32(out var value) || value != 1
                    || !rows[0].TryGetProperty("store_id", out var stored) || !stored.TryGetGuid(out var storeId)
                    || storeId == Guid.Empty)
                    throw new InvalidDataException("Supabase returned an invalid persisted storage identity.");

                return new(CreateSupabaseStorageIdentity(storeId));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                    Message = "Supabase could not read its enrolled storage identity through PostgREST.", Exception = cause };
            }
        }

        private CosmicStorageIdentity CreateSupabaseStorageIdentity(Guid storeId)
        {
            var resource = new Uri(_baseUrl).GetLeftPart(UriPartial.Path).TrimEnd('/');
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
            return new(Core.Enums.ProviderType.SupabaseOASIS, fingerprint, storeId);
        }

        public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
            CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request == null || request.ExpectedStorage.Authority != Core.Enums.ProviderType.SupabaseOASIS
                || !Guid.TryParse(request.ExpectedStorage.StoreId, out var storeId) || storeId == Guid.Empty)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                    Message = "An exact Supabase replica receipt intent and binding are required." };

            try
            {
                if (!IsProviderActivated)
                    throw new InvalidOperationException("Activate Supabase before reading replica receipts.");
                var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
                if (CreateSupabaseStorageIdentity(storeId).BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                    return SupabaseReceiptBindingMismatch();

                using var message = CreatePostgrestRequest(HttpMethod.Post, "rpc/oasis_read_replica_receipt");
                message.Content = JsonContent(new { expected_store = storeId, requested_operation = plan.OperationId });
                using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
                var rows = document.RootElement;
                if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1)
                    throw new InvalidDataException("Supabase did not return exactly one bound receipt observation.");
                var row = rows[0];
                if (!row.GetProperty("store_matches").GetBoolean())
                    return SupabaseReceiptBindingMismatch();

                var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                    row.GetProperty("request_hash").GetString(), row.GetProperty("result_json").GetString(),
                    HolonReplicaHeadReadConsistency.NativeSnapshot);
                return new(evidence) { IsLoaded = true };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_UNVERIFIED",
                    Message = "The Supabase replica receipt could not be verified.", Exception = cause };
            }
        }

        private static OASISResult<CommittedHolonReplicaReceiptEvidence> SupabaseReceiptBindingMismatch()
            => new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH",
                Message = "The Supabase database differs from the enrolled replica store." };
    }
}
