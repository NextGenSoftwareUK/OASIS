using System.Net.Http;
using System;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public sealed class SupabaseQuerySource : SupabaseOASIS
{
    public SupabaseQuerySource(string origin, string gatewayKey, string readerToken, HttpClient client, Guid probeId)
        : base(origin, gatewayKey, readerToken, client, probeId)
    {
    }
}
