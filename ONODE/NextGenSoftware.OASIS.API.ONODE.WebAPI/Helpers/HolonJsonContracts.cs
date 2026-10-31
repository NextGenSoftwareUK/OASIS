using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Bind the ordinary holon wire contract recursively, including child
/// interfaces. Payloads never select a CLR type. Keep the serializer's original
/// reference/depth handling rather than starting nested serializer operations.</summary>
public static class HolonJsonContracts
{
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(type =>
            {
                if (type.Type == typeof(IHolon) && type.Kind == JsonTypeInfoKind.Object)
                {
                    type.CreateObject = static () => new Holon();
                    // This persisted observation is declared on Holon rather
                    // than IHolon. Child interface serialization must retain it
                    // so a complete graph can bind the same conditional intent.
                    var name = options.PropertyNamingPolicy?.ConvertName(nameof(Holon.ChildRoleManifestJson))
                        ?? nameof(Holon.ChildRoleManifestJson);
                    var membership = type.CreateJsonPropertyInfo(typeof(string), name);
                    membership.Get = value => (value as Holon)?.ChildRoleManifestJson;
                    membership.Set = (value, json) => ((Holon)value).ChildRoleManifestJson = (string)json;
                    type.Properties.Add(membership);
                }
            });
    }
}
