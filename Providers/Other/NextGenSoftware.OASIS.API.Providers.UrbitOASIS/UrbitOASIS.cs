using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons on an Urbit ship, in the %oasis-kv Gall agent of the %oasis desk
    /// (sources in UrbitDesk/, installed by the integration fixture or with |install on a real ship).
    ///
    /// Everything goes through Eyre with the owner's session (login with the ship's +code):
    ///   read   GET  /~/scry/oasis-kv/entry/&lt;hex key&gt;.json, /~/scry/oasis-kv/keys/&lt;hex prefix&gt;.json
    ///   write  POST /spider/oasis/oasis-kv-action/oasis-kv-poke/json — the thread pokes %oasis-kv and only
    ///          completes on the agent's poke-ack; a nack fails the thread and Eyre answers 500.
    ///
    /// Records are stored in plaintext: agent state is private to the ship, only the owner's authenticated
    /// session can read or write it, %oasis-kv only accepts pokes from our own ship and exposes nothing over
    /// remote scry or subscriptions. Wrap the backend in <see cref="PrivateKeyValueBackend"/> if the data
    /// must also be hidden from other agents running on the same ship.
    /// </summary>
    public class UrbitOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        /// <param name="shipUrl">The ship's Eyre origin, e.g. http://localhost:8080.</param>
        /// <param name="loginCode">The ship's web login code (+code in the dojo). Required to activate.</param>
        /// <param name="store">Namespace inside %oasis-kv; separate OASIS stores on one ship use different names.</param>
        public UrbitOASIS(string shipUrl = "http://localhost", string? loginCode = null, string store = "oasis")
            : this(new UrbitKeyValueBackend(shipUrl, loginCode), store)
        {
        }

        internal UrbitOASIS(IKeyValueBackend backend, string store) : base(backend, ValidateStore(store) + "/")
        {
            ProviderName = "UrbitOASIS";
            ProviderDescription = "Urbit provider: OASIS avatars and holons in the %oasis-kv Gall agent on your own ship (Eyre HTTP).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.UrbitOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static string ValidateStore(string store)
        {
            if (string.IsNullOrWhiteSpace(store) || store.Contains('/'))
                throw new ArgumentException("Store must be a non-empty name without '/'.", nameof(store));
            return store;
        }

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
            => ToBool(await DeleteHolonRecordAsync(id, softDelete));

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
            => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string providerKey, bool softDelete = true)
            => ToBool(await DeleteHolonRecordAsync(providerKey, softDelete));

        public OASISResult<bool> DeleteHolonSoft(string providerKey, bool softDelete = true)
            => DeleteHolonSoftAsync(providerKey, softDelete).GetAwaiter().GetResult();

        private static OASISResult<bool> ToBool(OASISResult<IHolon> deleted)
        {
            var result = new OASISResult<bool>(!deleted.IsError) { Message = deleted.Message, IsDeleted = deleted.IsDeleted, DeletedCount = deleted.DeletedCount };
            if (deleted.IsError) OASISErrorHandling.HandleError(ref result, deleted.Message, deleted.Exception!);
            return result;
        }
    }

    /// <summary><see cref="IKeyValueBackend"/> over Eyre and the %oasis-kv agent. Every non-success answer throws.</summary>
    internal sealed class UrbitKeyValueBackend : IKeyValueBackend
    {
        internal const string Agent = "oasis-kv";
        internal const string PokePath = "/spider/oasis/oasis-kv-action/oasis-kv-poke/json";

        private readonly HttpClient _http;
        private readonly CookieContainer _cookies = new();
        private readonly Uri _origin;
        private readonly string? _loginCode;
        private readonly SemaphoreSlim _login = new(1, 1);
        private volatile bool _authenticated;

        public UrbitKeyValueBackend(string shipUrl, string? loginCode)
        {
            if (string.IsNullOrWhiteSpace(shipUrl)) throw new ArgumentException("The ship URL is required.", nameof(shipUrl));
            _origin = new Uri(shipUrl.TrimEnd('/') + "/");
            _loginCode = loginCode;
            _http = new HttpClient(new HttpClientHandler { CookieContainer = _cookies, UseCookies = true })
            {
                BaseAddress = _origin,
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        /// <summary>Lowercase hex of the UTF-8 bytes: one Eyre path segment for any key, decoded by the agent.</summary>
        internal static string Hex(string value) => Convert.ToHexString(Encoding.UTF8.GetBytes(value)).ToLowerInvariant();

        private async Task EnsureLoggedInAsync(CancellationToken ct)
        {
            if (_authenticated) return;
            await _login.WaitAsync(ct);
            try
            {
                if (_authenticated) return;
                if (string.IsNullOrEmpty(_loginCode))
                    throw new InvalidOperationException("UrbitOASIS needs the ship's +code to log in to Eyre.");
                using var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("password", _loginCode) });
                using var response = await _http.PostAsync("~/login", form, ct);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Eyre login failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                var session = false;
                foreach (Cookie cookie in _cookies.GetCookies(_origin))
                    if (cookie.Name.StartsWith("urbauth-", StringComparison.Ordinal)) session = true;
                if (!session) throw new HttpRequestException("Eyre login returned no urbauth session cookie; check the +code.");
                _authenticated = true;
            }
            finally { _login.Release(); }
        }

        private async Task<JsonElement> ScryAsync(string path, CancellationToken ct)
        {
            await EnsureLoggedInAsync(ct);
            using var response = await _http.GetAsync("~/scry/" + Agent + "/" + path + ".json", ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Urbit scry /{Agent}/{path} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        private async Task PokeAsync(object action, CancellationToken ct)
        {
            await EnsureLoggedInAsync(ct);
            using var content = new StringContent(JsonSerializer.Serialize(action), Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(PokePath.TrimStart('/'), content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Urbit poke of %{Agent} was not acknowledged: {(int)response.StatusCode} {response.ReasonPhrase}");
            if (body.Trim() != "\"ack\"")
                throw new HttpRequestException("Urbit poke thread returned an unexpected result instead of the poke-ack.");
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var value = await ScryAsync("entry/" + Hex(key), cancellationToken);
            return value.ValueKind switch
            {
                JsonValueKind.Null => null!,
                JsonValueKind.String => value.GetString()!,
                _ => throw new HttpRequestException($"Urbit entry scry returned {value.ValueKind}, expected a string or null.")
            };
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(value);
            return PokeAsync(new { put = new { key, value } }, cancellationToken);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => PokeAsync(new { del = new { key } }, cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = await ScryAsync(string.IsNullOrEmpty(prefix) ? "keys" : "keys/" + Hex(prefix), cancellationToken);
            if (keys.ValueKind != JsonValueKind.Array)
                throw new HttpRequestException($"Urbit keys scry returned {keys.ValueKind}, expected an array.");
            var result = new List<string>(keys.GetArrayLength());
            foreach (var key in keys.EnumerateArray())
                result.Add(key.GetString() ?? throw new HttpRequestException("Urbit keys scry returned a non-string key."));
            return result;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var ping = await ScryAsync("ping", cancellationToken);
            if (ping.ValueKind != JsonValueKind.String || ping.GetString() != Agent)
                throw new HttpRequestException($"The ship's %{Agent} agent did not answer its ping scry; install the %oasis desk.");
        }
    }
}
