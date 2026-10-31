using System;
using System.Globalization;
using System.Threading;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>One admitted operation's bounded, unique per-physical-write outbox correlation.</summary>
public sealed class NativeStorageCorrelation
{
    private const string Suffix=".storage.";
    public static readonly int MaxPrefixLength=128-Suffix.Length-int.MaxValue.ToString(CultureInfo.InvariantCulture).Length;
    private readonly string _prefix;
    private long _sequence;
    public NativeStorageCorrelation(string prefix)
    {
        if(string.IsNullOrWhiteSpace(prefix)||prefix.Length>MaxPrefixLength)
            throw new ArgumentException("Storage correlation must reserve the full bounded write-sequence suffix.",nameof(prefix));
        _prefix=prefix;
    }
    public string Next()
    {
        while(true){
            var current=Volatile.Read(ref _sequence);
            if(current<0||current>=int.MaxValue)throw new InvalidOperationException("Admitted storage write sequence exhausted.");
            var sequence=current+1;
            if(Interlocked.CompareExchange(ref _sequence,sequence,current)==current)
                return _prefix+Suffix+sequence.ToString(CultureInfo.InvariantCulture);
        }
    }
}
