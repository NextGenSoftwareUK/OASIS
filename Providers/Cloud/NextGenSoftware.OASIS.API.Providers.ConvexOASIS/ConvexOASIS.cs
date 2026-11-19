using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS;

/// <summary>Encrypted OASIS record policy over the bundled internal Convex companion.</summary>
public class ConvexOASIS : KeyValueStorageProviderBase, IDisposable
{
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public ConvexOASIS(string deploymentUrl, string deployKey)
        : this(deploymentUrl, deployKey, CreateHttp(deploymentUrl, deployKey),
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_CONVEX_STORAGE_KEY"), ownsHttpClient: true) { }
    public ConvexOASIS(string deploymentUrl, string deployKey, HttpClient httpClient)
        : this(deploymentUrl, deployKey, httpClient,
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_CONVEX_STORAGE_KEY")) { }
    public ConvexOASIS(string deploymentUrl, string deployKey, HttpClient httpClient, Func<byte[]> storageKeyResolver, bool ownsHttpClient = false)
        : this(PersistedPrivateKeyValueBackend.Create(new ConvexRecordBackend(deploymentUrl, deployKey, httpClient), storageKeyResolver), httpClient, ownsHttpClient) { }
    private ConvexOASIS(PersistedPrivateKeyValueBackend backend, HttpClient http, bool owns) : base(backend)
    {
        _private = backend; _http = http; _ownsHttp = owns;
        ProviderName = "ConvexOASIS";
        ProviderDescription = "Convex native transactional companion with shared encrypted OASIS record policy";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ConvexOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }
    private static HttpClient CreateHttp(string origin, string key)
    {
        ConvexRecordBackend.Validate(origin, key);
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    }
    protected override Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if (version != 0) throw new NotSupportedException("Convex credentials must select the current record, not archived versions.");
        return base.LoadAvatarsForCredentialSelectionAsync(version);
    }
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonRecordAsync(id, true);
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
    {
        var deleted = await DeleteHolonRecordAsync(id, softDelete);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, bool>(deleted);
        result.Result = !deleted.IsError && deleted.IsDeleted;
        return result;
    }
    public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
        => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();
    public void Dispose()
    {
        IsProviderActivated = false; _private.Dispose();
        if (_ownsHttp) _http.Dispose();
    }
}
