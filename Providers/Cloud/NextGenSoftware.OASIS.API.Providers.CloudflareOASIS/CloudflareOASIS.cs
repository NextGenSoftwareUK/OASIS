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

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS;

/// <summary>Cloudflare Workers KV REST with shared encrypted records; native eventual consistency is preserved.</summary>
public class CloudflareOASIS : KeyValueStorageProviderBase, IDisposable
{
    public const string StorageKeyEnvironmentVariable = "OASIS_CLOUDFLARE_STORAGE_KEY";
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    internal CloudflareRestBackend NativeBackend { get; private set; } = null!;

    public CloudflareOASIS(string accountId, string namespaceId, string apiToken)
        : this(accountId, namespaceId, apiToken, CreateHttp(accountId, namespaceId, apiToken),
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable), true) { }
    public CloudflareOASIS(string accountId, string namespaceId, string apiToken, HttpClient httpClient, Func<byte[]> storageKeyResolver, bool ownsHttpClient = false)
        : this(new CloudflareRestBackend(accountId, namespaceId, apiToken, httpClient), httpClient, storageKeyResolver, ownsHttpClient) { }
    private CloudflareOASIS(CloudflareRestBackend native, HttpClient http, Func<byte[]> resolver, bool owns)
        : this(PersistedPrivateKeyValueBackend.Create(native, resolver), http, owns) => NativeBackend = native;
    private CloudflareOASIS(PersistedPrivateKeyValueBackend backend, HttpClient http, bool owns) : base(backend, "oasis/cloudflare/")
    {
        _private = backend; _http = http; _ownsHttp = owns;
        ProviderName = "CloudflareOASIS";
        ProviderDescription = "Cloudflare Workers KV REST with shared encrypted records and native eventual consistency";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CloudflareOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }
    private static HttpClient CreateHttp(string account, string space, string token)
    {
        CloudflareRestBackend.Validate(account, space, token);
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    }
    protected override Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if (version != 0) throw new NotSupportedException("Cloudflare credential authentication requires the latest record, not archived credentials.");
        return base.LoadAvatarsForCredentialSelectionAsync(version);
    }
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonRecordAsync(id, true);
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
    {
        var deleted = await DeleteHolonRecordAsync(id, softDelete).ConfigureAwait(false);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, bool>(deleted);
        result.Result = !deleted.IsError && deleted.IsDeleted;
        return result;
    }
    public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
        => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();
    public void Dispose()
    {
        IsProviderActivated = false;
        try { _private.Dispose(); }
        finally { if (_ownsHttp) _http.Dispose(); }
    }
}
