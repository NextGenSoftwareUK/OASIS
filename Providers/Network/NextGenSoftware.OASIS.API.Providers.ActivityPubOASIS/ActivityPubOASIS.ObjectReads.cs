using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    /// <summary>Reads a public federated status by its exact object URI on the configured instance.</summary>
    public async Task<OASISResult<IHolon>> LoadPublicStatusByObjectUriAsync(
        string objectUri, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!Uri.TryCreate(objectUri, UriKind.Absolute, out var identity) ||
                identity.Scheme != Uri.UriSchemeHttps || identity.UserInfo.Length != 0 ||
                identity.Fragment.Length != 0)
                throw new ArgumentException("An absolute HTTPS ActivityPub object URI is required.", nameof(objectUri));
            if (string.IsNullOrWhiteSpace(_accessToken))
                throw new UnauthorizedAccessException("A Mastodon user token with read:search is required.");
            if (!_isActivated)
                throw new InvalidOperationException("Activate the configured Mastodon instance before resolving a status.");

            var path = "/api/v2/search?type=statuses&resolve=true&limit=2&q=" + Uri.EscapeDataString(objectUri);
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var document = await ReadCqlStatusDocumentAsync(response, cancellationToken).ConfigureAwait(false);
            var statuses = document.RootElement.GetProperty("statuses");
            if (statuses.ValueKind != JsonValueKind.Array || statuses.GetArrayLength() > 2)
                throw new InvalidDataException("Mastodon returned an invalid status resolution envelope.");

            IHolon match = null;
            foreach (var status in statuses.EnumerateArray())
            {
                if (!status.TryGetProperty("uri", out var uri) || uri.ValueKind != JsonValueKind.String ||
                    !string.Equals(uri.GetString(), objectUri, StringComparison.Ordinal))
                    continue;
                var projected = ProjectMastodonStatus(status);
                if (!projected.IsPublic || match != null)
                    throw new InvalidDataException("Status resolution requires one unambiguous public object.");
                match = projected;
            }
            if (match == null)
                return new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound,
                    Message = "The configured instance did not resolve the exact public object URI." };
            result.Result = match;
            result.IsLoaded = true;
            result.LoadedCount = 1;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            OASISErrorHandling.HandleError(ref result, "Mastodon public status resolution failed.", error);
        }
        return result;
    }
}
