#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Optional NATIVE per-key preconditions. This is not a transaction across keys,
/// an operation receipt, or permission to retry an uncertain mutation.</summary>
public interface IConditionalKeyValueBackend : IKeyValueBackend
{
    /// <summary>Returns value and opaque revision from one native observation; null means explicit absence.
    /// Authorization, corrupt responses and transport failures must throw, not become absence.</summary>
    Task<KeyValueObservation?> ReadObservedAsync(string key,CancellationToken cancellationToken=default);

    /// <summary>Native create-if-absent; an existing key is a known conflict.</summary>
    Task CreateOnlyAsync(string key,string value,CancellationToken cancellationToken=default);

    /// <summary>Native replacement of precisely the observed revision; disappearance is a conflict.</summary>
    Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken cancellationToken=default);

    /// <summary>Native deletion of precisely the observed revision; disappearance is a conflict.</summary>
    Task DeleteUnchangedAsync(string key,string revision,CancellationToken cancellationToken=default);
}

/// <summary>Immutable value and service-owned revision observed together, never a generated token.</summary>
public sealed class KeyValueObservation
{
    public string Value {get;}
    public string Revision {get;}
    public KeyValueObservation(string value,string revision)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        Value=value;Revision=revision;
    }
}
