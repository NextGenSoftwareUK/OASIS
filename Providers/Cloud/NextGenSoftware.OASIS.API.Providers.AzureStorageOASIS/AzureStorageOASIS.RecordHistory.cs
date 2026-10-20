using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS
{
    public bool IsVersionControlEnabled { get; set; }
    private static readonly EmbeddedRecordHistory BlobHistory = new();

    private static void HandleBlobSaveError<T>(ref NextGenSoftware.OASIS.Common.OASISResult<T> result, Exception cause)
    {
        NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result, cause.Message, cause);
        if (cause is RequestFailedException rejection && (rejection.Status == 412 || rejection.Status == 409 && rejection.ErrorCode == "BlobAlreadyExists"))
            result.ErrorCode = NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode;
    }

    private static async Task<T> ReadBlobHistoryAsync<T>(string json, int version) where T : class, IHolonBase
    {
        ValidateDetailVersion(version);
        var record = Deserialise<T>(json) ?? throw new InvalidDataException("Canonical blob record is null.");
        if (record.Id == Guid.Empty) throw new InvalidDataException("Canonical blob record has no identity.");
        var document = JObject.Parse(json);
        if (document.Property("id") != null && (document["id"]?.Type != JTokenType.String || document["id"]!.Value<string>() != record.Id.ToString()))
            throw new InvalidDataException("Blob history identity differs from the canonical record.");
        document["id"] = record.Id.ToString();
        return await BlobHistory.ReadAsync(record.Id, document, version, Deserialise<T>);
    }

    private static void PublishBlobVersion(IHolonBase source, IHolonBase committed)
    {
        source.Version = committed.Version;
        source.VersionId = committed.VersionId;
        source.PreviousVersionId = committed.PreviousVersionId;
    }

    // Staging never mutates the caller. The native writer must condition the complete
    // current+archive document on the exact revision read, not upload an archive separately.
    private static async Task<JObject> StageBlobHistoryAsync<T>(T proposed, string previousJson, bool createHistory,PlannedHolonWrite? plannedHolon=null)
        where T : class, IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);
        if (proposed.Id == Guid.Empty) throw new ArgumentException("An explicit staged blob identity is required.");
        JObject previous = null;T? prior=null;
        if (previousJson != null)
        {
            // Use the canonical decoder first: it turns malformed canonical records into
            // typed integrity failures rather than trusting an unvalidated archive.
            prior = await ReadBlobHistoryAsync<T>(previousJson, 0);
            if (prior == null || prior.Id != proposed.Id)
                throw new InvalidDataException("Existing blob identity differs from the proposed record.");
            previous = JObject.Parse(previousJson);
            previous["id"] = prior.Id.ToString();
        }
        if(typeof(T)==typeof(Holon) && proposed is Holon holon)
            holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(plannedHolon??new PlannedHolonWrite(holon,holon),
                HolonChildRoleWriteKind.SourceSave,previousJson!=null,(prior as Holon)?.ChildRoleManifestJson);
        return createHistory || previous?.Property(EmbeddedRecordHistory.PropertyName) != null
            ? await BlobHistory.StageAsync(proposed, previous, Deserialise<T>)
            : JObject.Parse(Serialise(proposed));
    }

    // The production constructor disables SDK retries: never replay an unknown Commit
    // and misreport its subsequent precondition rejection as the original outcome.
    private static async Task<T> WriteVersionedBlobAsync<T>(BlobClient blob, T proposed, bool createHistory,Action validatePublication=null,PlannedHolonWrite? plannedHolon=null)
        where T : class, IHolonBase
    {
        ArgumentNullException.ThrowIfNull(blob);
        ArgumentNullException.ThrowIfNull(proposed);
        if (proposed.Id == Guid.Empty) throw new ArgumentException("An explicit staged blob identity is required.");
        BlobDownloadResult observed = null;
        try { observed = (await blob.DownloadContentAsync()).Value; }
        catch (RequestFailedException cause) when (cause.Status == 404 && cause.ErrorCode == "BlobNotFound") { }
        if (observed != null && string.IsNullOrWhiteSpace(observed.Details.ETag.ToString()))
            throw new InvalidDataException("Observed blob has no authoritative revision.");
        var staged = await StageBlobHistoryAsync(proposed, observed?.Content.ToString(), createHistory,plannedHolon);
        var payload = BinaryData.FromString(staged.ToString(Newtonsoft.Json.Formatting.None));
        var committed = Deserialise<T>(payload.ToString())
            ?? throw new InvalidDataException("Staged blob record is null.");
        var condition = observed == null
            ? new BlobRequestConditions { IfNoneMatch = ETag.All }
            : new BlobRequestConditions { IfMatch = observed.Details.ETag };
        validatePublication?.Invoke();
        await ObserveBlobMutationAsync(async () =>
        {
            var response = await blob.UploadAsync(payload, new BlobUploadOptions { Conditions = condition });
            if (response.GetRawResponse().Status is < 200 or >= 300
                || string.IsNullOrWhiteSpace(response.Value.ETag.ToString()))
                throw new InvalidDataException("Conditional blob upload lacks an authoritative acknowledgement.");
            validatePublication?.Invoke();
            return response;
        }, blob);
        return committed;
    }
}
