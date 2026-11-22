using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Omitted choices are preserved; explicit null restores inheritance.</summary>
public sealed class AppUsageSettingsPatch
{
    private readonly Dictionary<string, JsonElement> _fields = new(StringComparer.Ordinal);

    public AppUsageSettingsPatch(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("App settings must be a JSON object.");
        foreach (var field in request.EnumerateObject())
        {
            if (field.Name is not ("ownerViewEnabled" or "warningsEnabled" or "accessLogEnabled" or
                "sealedHistoryEnabled" or "versionPurgeEnabled" or "versionPurgeDays"))
                throw new ArgumentException("An unrecognized app setting was supplied.");
            if (!_fields.TryAdd(field.Name, field.Value.Clone()))
                throw new ArgumentException("An app setting was supplied more than once.");
            if (field.Name == "versionPurgeDays")
            {
                if (field.Value.ValueKind != JsonValueKind.Null &&
                    (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetInt32(out _)))
                    throw new ArgumentException("Version purge days must be a whole number or null.");
                var error = Purge.PurgeChoice.Validate(Days());
                if (error != null) throw new ArgumentException(error);
            }
            else if (field.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null))
                throw new ArgumentException("App switches must be booleans or null.");
        }
        if (_fields.Count == 0) throw new ArgumentException("At least one app setting is required.");
    }

    private int? Days() => _fields["versionPurgeDays"].ValueKind == JsonValueKind.Null
        ? null : _fields["versionPurgeDays"].GetInt32();

    private bool? Choice(string name, bool? current) => !_fields.TryGetValue(name, out var value)
        ? current : value.ValueKind == JsonValueKind.Null ? null : value.GetBoolean();

    public AppUsageOverrides Apply(AppUsageOverrides current) => new()
    {
        OwnerViewEnabled = Choice("ownerViewEnabled", current?.OwnerViewEnabled),
        WarningsEnabled = Choice("warningsEnabled", current?.WarningsEnabled),
        AccessLogEnabled = Choice("accessLogEnabled", current?.AccessLogEnabled),
        SealedHistoryEnabled = Choice("sealedHistoryEnabled", current?.SealedHistoryEnabled),
        VersionPurgeEnabled = Choice("versionPurgeEnabled", current?.VersionPurgeEnabled),
        VersionPurgeDays = _fields.ContainsKey("versionPurgeDays") ? Days() : current?.VersionPurgeDays
    };
}
