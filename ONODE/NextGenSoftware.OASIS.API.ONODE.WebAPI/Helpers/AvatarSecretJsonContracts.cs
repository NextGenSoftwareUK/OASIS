using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Never write an avatar's stored password hash or its reset and
/// verification tokens into a JSON response. Only serialization is suppressed:
/// request binding, storage and authentication still see the full avatar.</summary>
public static class AvatarSecretJsonContracts
{
    private static readonly string[] SecretProperties = { "Password", "ResetToken", "VerificationToken" };

    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(type =>
            {
                if (type.Kind != JsonTypeInfoKind.Object || !typeof(IAvatar).IsAssignableFrom(type.Type)) return;
                foreach (var property in type.Properties)
                {
                    foreach (var secret in SecretProperties)
                    {
                        if (string.Equals(property.Name, secret, StringComparison.OrdinalIgnoreCase))
                            property.ShouldSerialize = static (_, _) => false;
                    }
                }
            });
    }
}
