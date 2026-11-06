using System;
using System.Net.Http;

namespace NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS;

/// <summary>The explicitly hosted profile admits only public teaching records.</summary>
public sealed class AlgoliaQuerySource(string application, string key, Guid probe, HttpClient client)
    : AlgoliaOASIS(application, key, probe, client)
{
    protected override bool RequiresPublicQueryRecords => true;
}
