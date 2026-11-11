using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

internal static class FirebaseConformanceCleanup
{
    internal static async Task<OASISResult<bool>> RemoveHolonTombstoneAsync(
        HttpClient http, Uri uri, Guid fixtureId)
    {
        using var lookup = new HttpRequestMessage(HttpMethod.Get, uri);
        lookup.Headers.Add("X-Firebase-ETag", "true");
        using var observed = await http.SendAsync(lookup);
        observed.EnsureSuccessStatusCode();
        var tag = NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.FirebaseOASIS.ReadStrongRevisionTag(observed);
        using var stored = JsonDocument.Parse(await observed.Content.ReadAsStringAsync());
        var record = stored.RootElement;
        if (record.ValueKind != JsonValueKind.Object
            || !record.TryGetProperty("id", out var storedId) || !storedId.TryGetGuid(out var actual) || actual != fixtureId
            || !record.TryGetProperty("deletedDate", out var deletedDate)
            || !deletedDate.TryGetDateTime(out var when) || when == DateTime.MinValue)
            throw new InvalidOperationException("Only the exact owned Firebase tombstone with a strong ETag may be removed.");

        using var delete = new HttpRequestMessage(HttpMethod.Delete, uri);
        if (!delete.Headers.TryAddWithoutValidation("If-Match", tag))
            throw new InvalidOperationException("Cannot attach the observed Firebase fixture revision.");
        using var deleted = await http.SendAsync(delete);
        deleted.EnsureSuccessStatusCode();
        using var remaining = JsonDocument.Parse(await http.GetStringAsync(uri));
        if (remaining.RootElement.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException("Firebase fixture tombstone remains after conditional deletion.");
        return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
    }
}
