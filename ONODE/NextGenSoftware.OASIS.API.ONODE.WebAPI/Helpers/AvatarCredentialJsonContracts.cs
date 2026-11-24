using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Never write an avatar's password hash or reset/verification tokens to JSON
/// output. Only serialization is suppressed: request binding still reads these members.</summary>
public static class AvatarCredentialJsonContracts
{
    private static readonly string[] CredentialMembers = { "Password", "ResetToken", "VerificationToken" };

    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(type =>
            {
                if (type.Kind != JsonTypeInfoKind.Object || !typeof(IAvatar).IsAssignableFrom(type.Type)) return;
                foreach (var property in type.Properties.Where(property =>
                    CredentialMembers.Any(name => string.Equals(name, (property.AttributeProvider as System.Reflection.MemberInfo)?.Name ?? property.Name,
                        StringComparison.OrdinalIgnoreCase))))
                    property.ShouldSerialize = static (_, _) => false;
            });
    }
}
