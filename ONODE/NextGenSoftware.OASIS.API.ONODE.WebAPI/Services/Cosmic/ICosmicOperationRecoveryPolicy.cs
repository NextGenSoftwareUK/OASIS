using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;

/// <summary>Application-owned authorization and key scope. Register explicitly;
/// HTTP input never supplies keys, resolvers, runtime contracts or another actor.</summary>
public interface ICosmicOperationRecoveryPolicy
{
    OASISResult<CosmicOperationRecoveryGrant> Authorize(Guid authenticatedActor, ProviderType authority);
}

/// <summary>Immutable grant for one actor/authority, with an explicit type
/// catalogue and authorized per-record protection contexts. Never returned over HTTP.</summary>
public sealed class CosmicOperationRecoveryGrant
{
    private readonly Func<Guid, HolonMetadataEncryptionPolicy> protection;
    private readonly IReadOnlyDictionary<(string, string), Type> contracts;
    public Guid ActorId { get; }
    public ProviderType Authority { get; }

    public CosmicOperationRecoveryGrant(Guid actor, ProviderType authority,
        Func<Guid, HolonMetadataEncryptionPolicy> authorizeRecord, IEnumerable<Type> allowedContracts)
    {
        if (actor == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actor));
        CosmicConcurrency.ValidateAuthority(authority);
        protection = authorizeRecord ?? throw new ArgumentNullException(nameof(authorizeRecord));
        var types = allowedContracts?.Take(257).ToArray() ?? throw new ArgumentNullException(nameof(allowedContracts));
        if (types.Length == 0 || types.Length > 256 || types.Any(type => type == null || type.IsAbstract
            || type.ContainsGenericParameters || !typeof(IHolon).IsAssignableFrom(type)))
            throw new ArgumentException("Admit a bounded nonempty catalogue of concrete holon contracts.", nameof(allowedContracts));
        contracts = types.ToDictionary(type => (type.FullName, type.Assembly.GetName().Name));
        ActorId = actor;
        Authority = authority;
    }

    public HolonMetadataEncryptionPolicy AuthorizeRecord(Guid record)
    {
        if (record == Guid.Empty) throw new ArgumentException("A selected record is required.", nameof(record));
        return protection(record) ?? throw new InvalidOperationException("The host did not authorize this record's protection context.");
    }

    public Type AdmitStoredType(string type, string assembly)
        => type != null && assembly != null && contracts.TryGetValue((type, assembly), out var admitted) ? admitted : null;
}
