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

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS;

/// <summary>Native Upstash REST transport with shared encrypted storage, history and queries.</summary>
public class UpstashOASIS : KeyValueStorageProviderBase, IDisposable
{
    public const string StorageKeyEnvironmentVariable = "OASIS_UPSTASH_STORAGE_KEY";
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    internal UpstashRestBackend NativeBackend { get; private set; } = null!;

    public UpstashOASIS(string restUrl, string token)
        : this(restUrl, token, CreateHttp(restUrl, token),
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable), true) { }
    public UpstashOASIS(string restUrl, string token, HttpClient httpClient, Func<byte[]> storageKeyResolver, bool ownsHttpClient = false)
        : this(new UpstashRestBackend(restUrl, token, httpClient), httpClient, storageKeyResolver, ownsHttpClient) { }
    private UpstashOASIS(UpstashRestBackend native, HttpClient http, Func<byte[]> resolver, bool owns)
        : this(PersistedPrivateKeyValueBackend.Create(native, resolver), http, owns) => NativeBackend = native;
    private UpstashOASIS(PersistedPrivateKeyValueBackend backend, HttpClient http, bool owns) : base(backend, "oasis/upstash/")
    {
        _private = backend; _http = http; _ownsHttp = owns;
        ProviderName = "UpstashOASIS";
        ProviderDescription = "Upstash REST with shared encrypted records and per-client causal reads";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.UpstashOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }
    private static HttpClient CreateHttp(string origin, string token)
    {
        UpstashRestBackend.Validate(origin, token);
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    }
    protected override Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if (version != 0) throw new NotSupportedException("Upstash credential authentication requires the latest record, not archived credentials.");
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
    public string GetProviderVersion() => "2.0.0";
    public Task<string> GetProviderVersionAsync() => Task.FromResult(GetProviderVersion());
    public void Dispose()
    {
        IsProviderActivated = false;
        try { _private.Dispose(); }
        finally { if (_ownsHttp) _http.Dispose(); }
    }
}
