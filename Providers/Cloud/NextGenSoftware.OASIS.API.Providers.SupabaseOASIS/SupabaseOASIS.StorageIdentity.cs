using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS
{
    public partial class SupabaseOASIS : ICosmicStorageIdentityProvider
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

                var resource = new Uri(_baseUrl).GetLeftPart(UriPartial.Path).TrimEnd('/');
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
                return new(new CosmicStorageIdentity(Core.Enums.ProviderType.SupabaseOASIS, fingerprint, storeId));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                    Message = "Supabase could not read its enrolled storage identity through PostgREST.", Exception = cause };
            }
        }
    }
}
