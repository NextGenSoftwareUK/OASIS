using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Self-only editable profile boundary. Never accepts or returns account/game snapshots.</summary>
[ApiController]
[Authorize]
[Route("api/avatar/profile")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SelfAvatarProfileController : OASISControllerBase
{
    private readonly AvatarManager _manager;
    private AvatarManager Manager => _manager ?? Program.AvatarManager;
    public SelfAvatarProfileController() { }
    internal SelfAvatarProfileController(AvatarManager manager) =>
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));

    [HttpGet]
    public async Task<IActionResult> GetProfile([FromQuery] ProviderType providerType = ProviderType.Default)
    {
        if (AvatarId == Guid.Empty) return Failure(401, AvatarProfileErrors.Forbidden);
        if (!ValidProvider(providerType)) return Failure(400, AvatarProfileErrors.Invalid);
        HttpContext.RequestAborted.ThrowIfCancellationRequested();
        var loaded = await Manager.CreateProfileRequestContextAsync(AvatarId, providerType);
        if (loaded.IsError || loaded.Result == null)
            return Failure(Status(loaded.ErrorCode), loaded.ErrorCode ?? AvatarProfileErrors.StorageUnavailable);
        var context = loaded.Result;
        var avatar = context.Avatar;
        var detail = context.Detail;
        // This projection is the public contract; credentials, policies, clients,
        // unrelated detail collections and private native evidence never leave it.
        var snapshot = new AvatarProfileSnapshot(avatar.Id, avatar.Title, avatar.FirstName, avatar.LastName,
            avatar.Username, avatar.Email, detail.Address,
            new AvatarProfileRevision(avatar.Version, avatar.VersionId),
            new AvatarProfileRevision(detail.Version, detail.VersionId));
        return Ok(new OASISResult<ProfileDocument>(new ProfileDocument(snapshot, context.ProviderType.ToString())));
    }

    [HttpPatch]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> ApplyProfile([FromBody] JsonElement body)
    {
        if (AvatarId == Guid.Empty) return Failure(401, AvatarProfileErrors.Forbidden);
        AvatarProfileMutation mutation;
        ProviderType provider;
        try { (mutation, provider) = Parse(body, AvatarId); }
        catch (Exception error) when (error is JsonException || error is ArgumentException ||
                                      error is InvalidOperationException || error is FormatException)
        { return Failure(400, AvatarProfileErrors.Invalid); }
        HttpContext.RequestAborted.ThrowIfCancellationRequested();
        // An installed paid admission owner must have selected this exact route.
        // A missing deployment route cannot silently become an unmetered write.
        if (HttpContext.RequestServices.GetService<IPreparedStorageAdmissionFactory>() != null &&
            PreparedStorageExecution.Current == null)
            return Failure(503, "AVATAR_PROFILE_ADMISSION_REQUIRED");
        var saved = await Manager.ApplyAvatarProfileAsync(AvatarId, mutation, provider, HttpContext.RequestAborted);
        if (saved.IsError || saved.Result == null)
            return Failure(Status(saved.ErrorCode), saved.ErrorCode ?? AvatarProfileErrors.StorageUnavailable);
        var serving = ServingProvider(saved.MetaData);
        if (!serving.HasValue)
            return Failure(503, AvatarProfileErrors.CommitUnknown);
        return Ok(new OASISResult<ProfileDocument>(new ProfileDocument(saved.Result, serving.Value.ToString()))
        {
            IsSaved = saved.IsSaved, SavedCount = saved.SavedCount, IsWarning = saved.IsWarning,
            Message = saved.IsWarning
                ? "The primary profile was saved; replication needs attention."
                : "The confirmed profile is returned."
        });
    }

    private static (AvatarProfileMutation, ProviderType) Parse(JsonElement body, Guid actor)
    {
        var fields = Object(body, "avatarRevision", "detailRevision", "changes", "providerType");
        if (fields.Count != 4) throw new JsonException("All profile preconditions are required.");
        var providerName = fields["providerType"].GetString();
        if (!Enum.TryParse<ProviderType>(providerName, false, out var provider) ||
            provider.ToString() != providerName || !ValidProvider(provider) ||
            provider == ProviderType.Default)
            throw new JsonException("The concrete serving provider is required.");
        var patch = JsonSerializer.Deserialize<AvatarProfilePatch>(fields["changes"].GetRawText())
            ?? throw new JsonException("Profile changes required.");
        var changes = new List<KeyValuePair<AvatarProfileProperty, string>>();
        foreach (var field in Enum.GetValues<AvatarProfileField>())
        {
            if (!patch.IsSpecified(field)) continue;
            var value = field switch
            {
                AvatarProfileField.Title => patch.Title, AvatarProfileField.FirstName => patch.FirstName,
                AvatarProfileField.LastName => patch.LastName, AvatarProfileField.Username => patch.Username,
                AvatarProfileField.Email => patch.Email, AvatarProfileField.Address => patch.Address,
                _ => throw new JsonException("Unknown profile field.")
            };
            changes.Add(new KeyValuePair<AvatarProfileProperty, string>(
                Enum.Parse<AvatarProfileProperty>(field.ToString()), value));
        }
        return (new AvatarProfileMutation(actor, Revision(fields["avatarRevision"]),
            Revision(fields["detailRevision"]), changes), provider);
    }

    private static Dictionary<string, JsonElement> Object(JsonElement value, params string[] allowed)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new JsonException("Object required.");
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!allowed.Contains(property.Name, StringComparer.Ordinal) ||
                !values.TryAdd(property.Name, property.Value))
                throw new JsonException("Unknown or duplicate profile property.");
        return values;
    }

    private static AvatarProfileRevision Revision(JsonElement value)
    {
        var fields = Object(value, "version", "versionId");
        if (fields.Count != 2 || !fields["version"].TryGetInt32(out var version) ||
            !fields["versionId"].TryGetGuid(out var id))
            throw new JsonException("Persisted profile revision required.");
        return new AvatarProfileRevision(version, id);
    }

    private static ProviderType? ServingProvider(IDictionary<string, string> metadata)
    {
        if (metadata == null || !metadata.TryGetValue("hyperDriveRoute", out var json)) return null;
        try
        {
            var route = JObject.Parse(json);
            var name = route.Value<string>("ServingProvider");
            return route.Value<bool?>("Succeeded") == true &&
                Enum.TryParse<ProviderType>(name, out var provider) && provider.ToString() == name &&
                ValidProvider(provider) && provider != ProviderType.Default ? provider : null;
        }
        catch (Newtonsoft.Json.JsonException) { return null; }
    }

    private static bool ValidProvider(ProviderType provider) => Enum.IsDefined(provider) &&
        provider != ProviderType.None && provider != ProviderType.All;
    private static int Status(string code) => code switch
    {
        AvatarProfileErrors.Invalid => 400, AvatarProfileErrors.Forbidden => 403,
        AvatarProfileErrors.NotFound => 404,
        AvatarProfileErrors.Conflict or AvatarProfileErrors.IdentityConflict => 409,
        _ => 503
    };
    private IActionResult Failure(int status, string code) => StatusCode(status,
        new OASISResult<ProfileDocument>
        {
            IsError = true, ErrorCount = 1, ErrorCode = code,
            Message = code == AvatarProfileErrors.CommitUnknown
                ? "The save outcome is uncertain. Reload before making another edit."
                : "The profile operation could not be completed."
        });

    public sealed record ProfileDocument(AvatarProfileSnapshot Profile, string ProviderType);
}
