using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

// Cloud-only native qualification. Input is piped in memory, including the
// owned fake ship's login code. Never print request input or session cookies.
if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
    throw new InvalidOperationException("This native fixture runs only on the owned cloud Linux runner.");
var input = await Console.In.ReadToEndAsync();
if (System.Text.Encoding.UTF8.GetByteCount(input) > 8192)
    throw new InvalidDataException("Bounded native fixture input required.");
using var configuration = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 4 });
var config = configuration.RootElement;
var fields = config.EnumerateObject().Select(p => p.Name).ToArray();
if (fields.Length != 4 || fields.Distinct(StringComparer.Ordinal).Count() != 4
    || fields.Except(new[] { "origin", "loginCode", "fixturePath", "phase" }, StringComparer.Ordinal).Any())
    throw new InvalidDataException("Exact native fixture input required.");
var origin = new Uri(config.GetProperty("origin").GetString()!, UriKind.Absolute);
if (origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.AbsolutePath != "/"
    || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
    throw new InvalidDataException("Only the owned published IPv4 loopback endpoint is admitted.");
var phase = config.GetProperty("phase").GetString();
if (phase is not ("beforeRestart" or "afterRestart"))
    throw new InvalidDataException("Explicit native lifecycle phase required.");
var loginCode = config.GetProperty("loginCode").GetString();
if (loginCode is not { Length: >= 8 and <= 128 } || loginCode.Any(c => !(c is >= 'a' and <= 'z' or '-')))
    throw new InvalidDataException("Owned fake ship login code has an invalid spelling.");
var fixtureBytes = await File.ReadAllBytesAsync(config.GetProperty("fixturePath").GetString()!);
if (Convert.ToHexString(SHA256.HashData(fixtureBytes)).ToLowerInvariant() != "75ac02bb8c0ad875ad227248cfe810441aa9f6550de39031e7381062b17293cb")
    throw new InvalidDataException("Native teaching source fixture differs.");
using var fixture = JsonDocument.Parse(fixtureBytes);
if (fixture.RootElement.GetProperty("visibility").GetString() != "Private")
    throw new InvalidDataException("Private native source contract required.");

using var anonymous = UrbitContactsV1Client.FromSession(origin, new CookieContainer());
var denied = await anonymous.ReadBookPageAsync("0v1");
if (!denied.IsError || denied.Exception is not HttpRequestException { StatusCode: not null } nativeDenial)
    throw new InvalidDataException("Native private page was not denied to an unauthenticated HTTP reader.");
if (nativeDenial.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
    throw new InvalidDataException("Anonymous native response did not prove authentication denial.");

var cookies = new CookieContainer();
using (var login = new HttpClient(new HttpClientHandler {
    AllowAutoRedirect = false, UseProxy = false, UseCookies = true, CookieContainer = cookies
}) { Timeout = TimeSpan.FromSeconds(15) })
{
    using var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("password", loginCode) });
    using var response = await login.PostAsync(new Uri(origin, "/~/login"), form);
    response.EnsureSuccessStatusCode();
    if (cookies.GetCookies(origin).Count == 0)
        throw new InvalidDataException("Native login did not establish a session.");
}
using var client = UrbitContactsV1Client.FromSession(origin, cookies);
var pages = fixture.RootElement.GetProperty("pages").EnumerateArray().ToArray();
var ids = pages.Select(page => page.GetProperty("id").GetString()!).ToArray();
var subset = fixture.RootElement.GetProperty("subsetIds").EnumerateArray().Select(id => id.GetString()!).ToArray();
var fullRead = new List<UrbitContactsV1Client.BookPage>();
foreach (var selected in new[] { ids, subset })
{
    foreach (var id in selected)
    {
        var result = await client.ReadBookPageAsync(id);
        if (result.IsError || result.Result is null || result.LoadedCount != 1 || result.Result.RequestedId != id)
            throw new InvalidDataException("Native HTTP page read failed: " + result.ErrorCode);
        var expected = pages.Single(page => page.GetProperty("id").GetString() == id).GetProperty("overlay");
        var page = result.Result;
        if (page.Contact.Nickname is not null || page.Contact.Bio is not null || page.Contact.Status is not null
            || page.Overlay.Nickname != expected.GetProperty("nickname").GetString()
            || page.Overlay.Bio != expected.GetProperty("bio").GetString()
            || page.Overlay.Status != expected.GetProperty("status").GetString())
            throw new InvalidDataException("Native contact/overlay projection differs from the owned source.");
        if (ReferenceEquals(selected, ids)) fullRead.Add(page);
    }
}
if (fullRead.Count != 3 || subset.Length != 2)
    throw new InvalidDataException("Native three/two catalogue incomplete.");
var sourceHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(fullRead))).ToLowerInvariant();
var owner = fixture.RootElement.GetProperty("sourceOwner").GetGuid();
// Explicit server fixture identity, independent of the ephemeral HTTP port.
var connection = Guid.Parse("eaa5679e-9250-4abb-bb5e-d415dcf1f117");
using var source = new UrbitContactBookQuerySource(client, owner, connection, ids);
var activated = await source.ActivateQuerySourceAsync();
if (activated.IsError) throw new InvalidDataException("Native CQL source activation failed: " + activated.ErrorCode);
CosmicQueryPlan<Holon> Plan(Guid actor, Guid[] selected)
{
    var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.UrbitOASIS, selected);
    if (access.IsError) throw new InvalidDataException("Native CQL read authority could not be constructed.");
    var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.UrbitOASIS).Result)
        .WithReadAccess(access.Result).Result.Where(row => selected.Contains(row.Id)).Result;
    var planned = CosmicQueryPlanner.Plan(query, ProviderType.UrbitOASIS,
        new[] { new CosmicQueryTarget(ProviderType.UrbitOASIS, source.QueryCapabilities) });
    if (planned.IsError) throw new InvalidDataException("Native CQL planning failed: " + planned.ErrorCode);
    return planned.Result;
}
var logical = ids.Select(id => UrbitContactBookQuerySource.LogicalId(owner, connection, id)).ToArray();
var subsetLogical = subset.Select(id => UrbitContactBookQuerySource.LogicalId(owner, connection, id)).ToArray();
var cqlFull = new List<IHolon>();
foreach (var selected in new[] { logical, subsetLogical })
{
    var queried = await source.ExecuteCosmicQueryAsync(Plan(owner, selected));
    if (queried.IsError || queried.Result is null || !selected.Order().SequenceEqual(queried.Result.Select(row => row.Id).Order()))
        throw new InvalidDataException("Native private CQL three/two selection failed: " + queried.ErrorCode);
    foreach (var row in queried.Result)
    {
        var cid = row.MetaData["UrbitBookPageId"].ToString()!;
        var expected = pages.Single(page => page.GetProperty("id").GetString() == cid).GetProperty("overlay");
        if (row.IsPublic || row.CreatedByAvatarId != owner || row.Children.Any()
            || row.Id != UrbitContactBookQuerySource.LogicalId(owner, connection, cid)
            || row.MetaData["ContactNickname"] is not null || row.MetaData["ContactBio"] is not null || row.MetaData["ContactStatus"] is not null
            || row.MetaData["OverlayNickname"]?.ToString() != expected.GetProperty("nickname").GetString()
            || row.MetaData["OverlayBio"]?.ToString() != expected.GetProperty("bio").GetString()
            || row.MetaData["OverlayStatus"]?.ToString() != expected.GetProperty("status").GetString())
            throw new InvalidDataException("Native private CQL projection or ownership differs.");
        if (ReferenceEquals(selected, logical)) cqlFull.Add(row);
    }
}
var foreign = await source.ExecuteCosmicQueryAsync(Plan(Guid.Parse("372a7b25-32cf-4316-88bc-ea1b56a24b37"), logical));
if (foreign.IsError || foreign.Result is null || foreign.Result.Count != 0)
    throw new InvalidDataException("Native private CQL did not isolate another actor.");
// Exercise the shared V2 typed materializer over the same native session and
// trusted catalogue, without creating another fixture or enabling replica writes.
var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA {
    OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
        HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2
    }
};
var registry = new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna) {
    IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
};
var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna, registry);
foreach (var selected in new[] { logical, subsetLogical })
{
    var managed = await manager.ExecuteQueryAsync(Plan(owner, selected),
        new ICosmicQueryStorageProvider[] { source });
    if (managed.IsError || managed.Result is null || managed.Result.TotalRows != selected.Length
        || managed.Result.Providers.Count != 1 || managed.Result.Providers[0].Provider != ProviderType.UrbitOASIS)
        throw new InvalidDataException("Native shared-manager query failed: " + managed.ErrorCode);
    var rows = managed.Result.Providers[0].Rows;
    if (!selected.Order().SequenceEqual(rows.Select(row => row.Id).Order()))
        throw new InvalidDataException("Native shared-manager identities differ.");
    foreach (var row in rows)
    {
        var direct = cqlFull.Single(value => value.Id == row.Id);
        if (row.IsPublic || row.CreatedByAvatarId != owner || row.Children.Any()
            || direct.MetaData.Count != row.MetaData.Count
            || direct.MetaData.Any(entry => !row.MetaData.TryGetValue(entry.Key, out var actual)
                || JsonSerializer.Serialize(entry.Value) != JsonSerializer.Serialize(actual)))
            throw new InvalidDataException("Native shared-manager private projection differs.");
    }
}
var managedForeign = await manager.ExecuteQueryAsync(
    Plan(Guid.Parse("372a7b25-32cf-4316-88bc-ea1b56a24b37"), logical),
    new ICosmicQueryStorageProvider[] { source });
if (managedForeign.IsError || managedForeign.Result is null || managedForeign.Result.TotalRows != 0)
    throw new InvalidDataException("Native shared-manager foreign actor was not isolated.");
var projected = cqlFull.OrderBy(row => row.Id).Select(row => new {
    row.Id, row.CreatedByAvatarId, row.IsPublic,
    cid = row.MetaData["UrbitBookPageId"],
    contactNickname = row.MetaData["ContactNickname"], contactBio = row.MetaData["ContactBio"], contactStatus = row.MetaData["ContactStatus"],
    overlayNickname = row.MetaData["OverlayNickname"], overlayBio = row.MetaData["OverlayBio"], overlayStatus = row.MetaData["OverlayStatus"]
}).ToArray();
var cqlHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(projected))).ToLowerInvariant();
Console.WriteLine(JsonSerializer.Serialize(new {
    phase, fullCount = 3, subsetCount = 2, anonymousDeniedStatus = (int)nativeDenial.StatusCode!.Value,
    projectedSourceSha256 = sourceHash, nativeHttpClientQualified = true,
    providerCqlQualified = true, cqlOwnerFullCount = 3, cqlOwnerSubsetCount = 2, cqlForeignPrivateCount = 0,
    privateCqlProjectionSha256 = cqlHash, sharedManagerQualified = true,
    managerOwnerFullCount = 3, managerOwnerSubsetCount = 2, managerForeignPrivateCount = 0,
    cqlQualified = false, liveDeployment = false
}));
