using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Some native key mutations acknowledged before a later step failed; do not replay automatically.</summary>
public sealed class KeyValuePartialMutationException:Exception,INativeMutationReconciliationRequired
{
    public ProviderType ProviderType {get;}
    public IReadOnlyList<string> AcknowledgedKeys {get;}
    public string OriginalTransactionId=>string.Empty;
    public KeyValuePartialMutationException(ProviderType provider,IEnumerable<string> keys,Exception cause)
        :base("Acknowledged key/value mutations require reconciliation before replay.",cause)
    {
        ProviderType=provider;AcknowledgedKeys=Array.AsReadOnly(keys.Distinct(StringComparer.Ordinal).ToArray());
    }
}
