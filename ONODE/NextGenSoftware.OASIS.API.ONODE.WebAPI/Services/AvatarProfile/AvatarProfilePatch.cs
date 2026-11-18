#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;

public enum AvatarProfileField { Title, FirstName, LastName, Username, Email, Address }

/// <summary>Presence-aware user profile changes. It carries no game, credential or account-control state.</summary>
[JsonConverter(typeof(AvatarProfilePatchJsonConverter))]
public sealed class AvatarProfilePatch : IValidatableObject
{
    private readonly Dictionary<AvatarProfileField, string?> _values = new();
    public string? Title { get => Get(AvatarProfileField.Title); set => Set(AvatarProfileField.Title, value); }
    public string? FirstName { get => Get(AvatarProfileField.FirstName); set => Set(AvatarProfileField.FirstName, value); }
    public string? LastName { get => Get(AvatarProfileField.LastName); set => Set(AvatarProfileField.LastName, value); }
    public string? Username { get => Get(AvatarProfileField.Username); set => Set(AvatarProfileField.Username, value); }
    public string? Email { get => Get(AvatarProfileField.Email); set => Set(AvatarProfileField.Email, value); }
    public string? Address { get => Get(AvatarProfileField.Address); set => Set(AvatarProfileField.Address, value); }

    public bool IsSpecified(AvatarProfileField field) => _values.ContainsKey(field);
    private string? Get(AvatarProfileField field) => _values.TryGetValue(field, out var value) ? value : null;
    private void Set(AvatarProfileField field, string? value) => _values[field] = value?.Trim();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (_values.Count == 0)
            yield return new ValidationResult("At least one editable profile field is required.");
        if (IsSpecified(AvatarProfileField.Username) && string.IsNullOrWhiteSpace(Username))
            yield return new ValidationResult("Username cannot be cleared.", new[] { nameof(Username) });
        if (IsSpecified(AvatarProfileField.Email))
        {
            if (string.IsNullOrWhiteSpace(Email))
                yield return new ValidationResult("Email cannot be cleared.", new[] { nameof(Email) });
            else if (!new EmailAddressAttribute().IsValid(Email))
                yield return new ValidationResult("Email must be a valid email address.", new[] { nameof(Email) });
        }
    }
}

public sealed class AvatarProfilePatchJsonConverter : JsonConverter<AvatarProfilePatch>
{
    private static readonly Dictionary<string, AvatarProfileField> Fields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = AvatarProfileField.Title, ["firstName"] = AvatarProfileField.FirstName,
        ["lastName"] = AvatarProfileField.LastName, ["username"] = AvatarProfileField.Username,
        ["email"] = AvatarProfileField.Email, ["address"] = AvatarProfileField.Address
    };
    public override bool HandleNull => true;

    public override AvatarProfilePatch Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("A profile change object is required.");
        var patch = new AvatarProfilePatch();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!Fields.TryGetValue(property.Name, out var field))
                throw new JsonException("Only editable profile fields are accepted.");
            if (patch.IsSpecified(field))
                throw new JsonException("A profile field cannot be supplied more than once.");
            if (property.Value.ValueKind != JsonValueKind.String && property.Value.ValueKind != JsonValueKind.Null)
                throw new JsonException("Profile field values must be strings or explicit nulls.");
            var value = property.Value.GetString();
            switch (field)
            {
                case AvatarProfileField.Title: patch.Title = value; break;
                case AvatarProfileField.FirstName: patch.FirstName = value; break;
                case AvatarProfileField.LastName: patch.LastName = value; break;
                case AvatarProfileField.Username: patch.Username = value; break;
                case AvatarProfileField.Email: patch.Email = value; break;
                case AvatarProfileField.Address: patch.Address = value; break;
            }
        }
        var error = patch.Validate(new ValidationContext(patch)).FirstOrDefault();
        if (error != null) throw new JsonException(error.ErrorMessage);
        return patch;
    }

    public override void Write(Utf8JsonWriter writer, AvatarProfilePatch value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject();
        foreach (var field in Fields)
        {
            if (!value.IsSpecified(field.Value)) continue;
            var text = field.Value switch
            {
                AvatarProfileField.Title => value.Title, AvatarProfileField.FirstName => value.FirstName,
                AvatarProfileField.LastName => value.LastName, AvatarProfileField.Username => value.Username,
                AvatarProfileField.Email => value.Email, AvatarProfileField.Address => value.Address,
                _ => throw new InvalidOperationException("Unknown profile field.")
            };
            writer.WriteString(field.Key, text);
        }
        writer.WriteEndObject();
    }
}
