using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LensOASIS
{
    /// <summary>
    /// OASIS provider for the Lens Protocol decentralised social graph (https://lens.xyz).
    /// Lens is an EVM-based social graph on Polygon (and Lens Network L2 from v2 onwards).
    ///
    /// OASIS mapping:
    ///   Lens Profile  → OASIS Avatar  (provider key = profile address or handle, e.g. "lens/stani")
    ///   Post (kind=1) → OASIS Holon
    ///
    /// Reading is fully public via the Lens v2 GraphQL API (no authentication required).
    /// Writing (creating posts/profiles) requires an EVM wallet signature and is exposed via
    /// CreatePostAsync (returns the typed-data hash for the caller to sign and broadcast).
    ///
    /// API endpoint: https://api.lens.xyz/graphql (Lens Protocol v2 mainnet)
    /// Docs: https://docs.lens.xyz/docs/sdk-overview
    /// </summary>
    public class LensOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        /// <summary>
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly HttpClient _httpClient;
        private const string GraphQlEndpoint = "https://api.lens.xyz/graphql";
        private bool _isActivated;

        /// <param name="accessToken">
        /// Optional Lens Protocol JWT access token for write operations.
        /// Obtain via Challenge + Authenticate mutation flow with an EVM wallet.
        /// If null, only read operations work.
        /// </param>
        public LensOASIS(string accessToken = null)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!string.IsNullOrEmpty(accessToken))
                _httpClient.DefaultRequestHeaders.Add("x-access-token", accessToken);

            ProviderName = "LensOASIS";
            ProviderDescription = "Lens Protocol decentralised social graph provider (GraphQL v2)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LensOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Ping the GraphQL API with the stats query — no auth required
                const string query = @"{ ""query"": ""{ globalProtocolStats { totalProfiles } }"" }";
                var response = await PostGraphQlAsync(query);

                if (response.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("globalProtocolStats", out _))
                {
                    _isActivated = true;
                    result.Result = true;
                    result.Message = "LensOASIS provider activated — Lens Protocol v2 API reachable.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LensOASIS: Unexpected response from Lens GraphQL health check.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"LensOASIS: Error activating provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            return await Task.FromResult(new OASISResult<bool>
            {
                Result = true,
                Message = "LensOASIS provider deactivated."
            });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var keysResult = KeyManager.Instance.GetProviderPublicKeysForAvatarById(
                    id, Core.Enums.ProviderType.LensOASIS);

                if (keysResult.IsError || keysResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LensOASIS: No Lens handle or address registered for this avatar GUID. " +
                        "Use LoadAvatarByUsernameAsync(handle) or LoadAvatarByProviderKeyAsync(address) instead.");
                    return result;
                }

                string key = System.Linq.Enumerable.FirstOrDefault(keysResult.Result) ?? string.Empty;
                return await (key.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? LoadAvatarByProviderKeyAsync(key, version)
                    : LoadAvatarByUsernameAsync(key, version));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.LoadAvatarAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        /// <param name="avatarUsername">Lens handle, e.g. "stani" or full namespaced "lens/stani".</param>
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // Normalise: Lens v2 handles use "namespace/localname" e.g. "lens/stani"
                var handle = avatarUsername.Contains('/') ? avatarUsername : $"lens/{avatarUsername}";

                const string gql = @"
query Profile($handle: Handle!) {
  profile(request: { forHandle: $handle }) {
    id
    handle { fullHandle localName }
    metadata { displayName bio picture { ... on ImageSet { optimized { uri } } } }
    stats { followers following posts }
    ownedBy { address }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = gql,
                    variables = new { handle }
                });

                var doc = await PostGraphQlAsync(payload);

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("profile", out var profileEl)
                    && profileEl.ValueKind != JsonValueKind.Null)
                {
                    result.Result = MapProfileToAvatar(profileEl);
                    result.IsError = false;
                    result.Message = $"Avatar loaded from Lens for handle '{handle}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"LensOASIS: Lens handle '{handle}' not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"LensOASIS.LoadAvatarByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
            => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "LensOASIS: Email lookup is not supported by Lens Protocol. " +
                "Use LoadAvatarByUsernameAsync(handle) or LoadAvatarByProviderKeyAsync(walletAddress) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        /// <param name="providerKey">EVM wallet address (0x...) owning the profile, or profile ID.</param>
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // Query by owner address — returns the default profile for that address
                const string gql = @"
query DefaultProfile($address: EvmAddress!) {
  defaultProfile(request: { for: $address }) {
    id
    handle { fullHandle localName }
    metadata { displayName bio picture { ... on ImageSet { optimized { uri } } } }
    stats { followers following posts }
    ownedBy { address }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = gql,
                    variables = new { address = providerKey }
                });

                var doc = await PostGraphQlAsync(payload);

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("defaultProfile", out var profileEl)
                    && profileEl.ValueKind != JsonValueKind.Null)
                {
                    result.Result = MapProfileToAvatar(profileEl);
                    result.IsError = false;
                    result.Message = $"Avatar loaded from Lens for address '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"LensOASIS: No default Lens profile found for address '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"LensOASIS.LoadAvatarByProviderKeyAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // Lens doesn't expose a global profile list; return most followed as a useful proxy
                const string gql = @"
query ExploreProfiles {
  exploreProfiles(request: { orderBy: MOST_FOLLOWERS, limit: TEN }) {
    items {
      id
      handle { fullHandle localName }
      metadata { displayName bio picture { ... on ImageSet { optimized { uri } } } }
      stats { followers following posts }
      ownedBy { address }
    }
  }
}";
                var doc = await PostGraphQlAsync(@"{ ""query"": """ + gql.Replace("\"", "\\\"").Replace("\n", "\\n") + @""" }");

                var avatars = new List<IAvatar>();
                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("exploreProfiles", out var explore)
                    && explore.TryGetProperty("items", out var items))
                {
                    foreach (var profileEl in items.EnumerateArray())
                        avatars.Add(MapProfileToAvatar(profileEl));
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Loaded {avatars.Count} Lens profiles (top by followers).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.LoadAllAvatarsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var pub = id.ToString("N");
                // Lens publication IDs look like 0x01-0x01; try MetaData if GUID was stored there
                OASISErrorHandling.HandleError(ref result,
                    "LensOASIS: LoadHolonAsync by GUID requires the Lens publication ID to be registered. " +
                    "Use LoadHolonAsync(providerKey) with the Lens publication ID (e.g. \"0x01-0x01\") instead.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.LoadHolonAsync: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                const string gql = @"
query Publication($id: PublicationId!) {
  publication(request: { forId: $id }) {
    ... on Post {
      id
      by { handle { fullHandle } }
      metadata { ... on TextOnlyMetadataV3 { content } ... on ArticleMetadataV3 { content } }
      stats { reactions comments mirrors }
      createdAt
    }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = gql,
                    variables = new { id = providerKey }
                });

                var doc = await PostGraphQlAsync(payload);

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("publication", out var pubEl)
                    && pubEl.ValueKind != JsonValueKind.Null)
                {
                    result.Result = MapPublicationToHolon(pubEl);
                    result.IsError = false;
                    result.Message = $"Holon loaded from Lens for publication '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"LensOASIS: Publication '{providerKey}' not found on Lens.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.LoadHolonAsync(key): {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Explore trending publications on Lens
                const string gql = @"
query ExplorePublications {
  explorePublications(request: {
    orderBy: LATEST,
    limit: TWENTY_FIVE,
    where: { publicationTypes: [POST] }
  }) {
    items {
      ... on Post {
        id
        by { handle { fullHandle } }
        metadata { ... on TextOnlyMetadataV3 { content } ... on ArticleMetadataV3 { content } }
        stats { reactions comments mirrors }
        createdAt
      }
    }
  }
}";
                var doc = await PostGraphQlAsync(@"{ ""query"": """ + EscapeGql(gql) + @""" }");

                var holons = new List<IHolon>();
                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("explorePublications", out var explore)
                    && explore.TryGetProperty("items", out var items))
                {
                    foreach (var pubEl in items.EnumerateArray())
                        holons.Add(MapPublicationToHolon(pubEl));
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Loaded {holons.Count} publications from Lens explore feed.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.LoadAllHolonsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "LensOASIS: Use LoadHolonsForParentAsync(providerKey) with a Lens profile address to load that profile's posts.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version).Result;

        /// <summary>Loads all posts by a Lens profile. providerKey = EVM address or profile ID.</summary>
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                const string gql = @"
query Publications($profileId: ProfileId!) {
  publications(request: {
    where: { from: [$profileId], publicationTypes: [POST] },
    limit: FIFTY
  }) {
    items {
      ... on Post {
        id
        by { handle { fullHandle } }
        metadata { ... on TextOnlyMetadataV3 { content } ... on ArticleMetadataV3 { content } }
        stats { reactions comments mirrors }
        createdAt
      }
    }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = gql,
                    variables = new { profileId = providerKey }
                });

                var doc = await PostGraphQlAsync(payload);
                var holons = new List<IHolon>();

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("publications", out var pubs)
                    && pubs.TryGetProperty("items", out var items))
                {
                    foreach (var pubEl in items.EnumerateArray())
                        holons.Add(MapPublicationToHolon(pubEl));
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Loaded {holons.Count} posts for Lens profile '{providerKey}'.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"LensOASIS.LoadHolonsForParentAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version).Result;

        // ─── Save / Delete (write — requires wallet signature via EVM) ────────────

        private OASISResult<T> WriteNotSupported<T>(string entity)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result,
                $"LensOASIS: Writing {entity} to Lens Protocol requires an EVM wallet signature. " +
                "Use CreatePostAsync(profileId, content) to get typed-data for the caller to sign and broadcast, " +
                "or supply an x-access-token JWT (obtained via Challenge+Authenticate) in the constructor.");
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => await Task.FromResult(WriteNotSupported<IAvatar>("avatar (profile)"));
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
            => await Task.FromResult(WriteNotSupported<IAvatarDetail>("avatar detail"));
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await Task.FromResult(WriteNotSupported<IHolon>("holon (post)"));
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await Task.FromResult(WriteNotSupported<IEnumerable<IHolon>>("holons"));
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("holon"));
        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true) => DeleteHolonSoftAsync(id, softDelete).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string providerKey, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("holon"));
        public OASISResult<bool> DeleteHolonSoft(string providerKey, bool softDelete = true) => DeleteHolonSoftAsync(providerKey, softDelete).Result;

        // ─── Lens-specific write operations ──────────────────────────────────────

        /// <summary>
        /// Creates a Lens Protocol post. Returns the typed-data JSON that the caller must sign
        /// with their EVM wallet and then broadcast using the BroadcastOnchainRequest mutation.
        /// Requires the constructor access token (x-access-token) to be set.
        /// </summary>
        /// <param name="profileId">Lens profile ID (e.g. "0x01") owning the post.</param>
        /// <param name="content">Text content of the post.</param>
        public async Task<OASISResult<string>> CreatePostAsync(string profileId, string content)
        {
            var result = new OASISResult<string>();
            try
            {
                // Lens v2 post creation: OnchainPostRequest with TextOnlyMetadataV3
                const string mutation = @"
mutation Post($request: OnchainPostRequest!) {
  post(request: $request) {
    ... on RelaySuccess { txHash txId }
    ... on LensProfileManagerRelayError { reason }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = mutation,
                    variables = new
                    {
                        request = new
                        {
                            contentURI = $"data:application/json,{{\"$schema\":\"https://json-schemas.lens.dev/publications/text-only/3.0.0.json\",\"lens\":{{\"id\":\"{Guid.NewGuid()}\",\"content\":\"{EscapeJson(content)}\",\"appId\":\"OASISNetwork\",\"locale\":\"en\",\"mainContentFocus\":\"TEXT_ONLY\"}}}}",
                        }
                    }
                });

                var doc = await PostGraphQlAsync(payload);

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("post", out var postEl))
                {
                    if (postEl.TryGetProperty("txHash", out var txHash))
                    {
                        result.Result = txHash.GetString();
                        result.IsError = false;
                        result.Message = $"Lens post created. TxHash={result.Result}";
                    }
                    else if (postEl.TryGetProperty("reason", out var reason))
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"LensOASIS: Post creation failed — {reason.GetString()}");
                    }
                    else
                    {
                        result.Result = doc.RootElement.GetRawText();
                        result.IsError = false;
                        result.Message = "Lens post queued. Check response for typed-data to sign.";
                    }
                }
                else if (doc.RootElement.TryGetProperty("errors", out var errors))
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"LensOASIS: GraphQL error creating post: {errors.GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.CreatePostAsync: {ex.Message}", ex);
            }
            return result;
        }

        // ─── Search ──────────────────────────────────────────────────────────────



        // ─── Export ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var keysResult = KeyManager.Instance.GetProviderPublicKeysForAvatarById(id, Core.Enums.ProviderType.LensOASIS);
            var key = (!keysResult.IsError && keysResult.Result != null)
                ? System.Linq.Enumerable.FirstOrDefault(keysResult.Result) ?? string.Empty
                : string.Empty;
            return await LoadHolonsForParentAsync(key);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
            => ExportAllDataForAvatarByIdAsync(id, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var err = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref err, avatarResult.Message);
                return err;
            }
            var provKey = avatarResult.Result.MetaData.ContainsKey("LensProfileId")
                ? avatarResult.Result.MetaData["LensProfileId"]?.ToString() ?? string.Empty
                : string.Empty;
            return await LoadHolonsForParentAsync(provKey);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LensOASIS: Email lookup not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmail, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).Result;

        // ─── Private helpers ──────────────────────────────────────────────────────

        private async Task<JsonDocument> PostGraphQlAsync(string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(GraphQlEndpoint, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }

        private static Avatar MapProfileToAvatar(JsonElement profile)
        {
            var avatar = new Avatar();

            if (profile.TryGetProperty("id", out var idEl))
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensOASIS] = idEl.GetString() ?? string.Empty;

            if (profile.TryGetProperty("handle", out var handle))
            {
                if (handle.TryGetProperty("localName", out var localName))
                    avatar.Username = localName.GetString() ?? string.Empty;
                if (handle.TryGetProperty("fullHandle", out var fullHandle))
                    avatar.MetaData["LensHandle"] = fullHandle.GetString() ?? string.Empty;
            }

            if (profile.TryGetProperty("metadata", out var meta) && meta.ValueKind != JsonValueKind.Null)
            {
                if (meta.TryGetProperty("displayName", out var dn) && dn.ValueKind != JsonValueKind.Null)
                    avatar.FirstName = dn.GetString() ?? string.Empty;
                if (meta.TryGetProperty("bio", out var bio) && bio.ValueKind != JsonValueKind.Null)
                    avatar.Description = bio.GetString() ?? string.Empty;
                if (meta.TryGetProperty("picture", out var pic)
                    && pic.ValueKind != JsonValueKind.Null
                    && pic.TryGetProperty("optimized", out var opt)
                    && opt.TryGetProperty("uri", out var uri))
                    avatar.MetaData["LensPictureUrl"] = uri.GetString() ?? string.Empty;
            }

            if (profile.TryGetProperty("stats", out var stats))
            {
                if (stats.TryGetProperty("followers", out var followers))
                    avatar.MetaData["Followers"] = followers.GetInt64();
                if (stats.TryGetProperty("following", out var following))
                    avatar.MetaData["Following"] = following.GetInt64();
                if (stats.TryGetProperty("posts", out var posts))
                    avatar.MetaData["Posts"] = posts.GetInt64();
            }

            if (profile.TryGetProperty("ownedBy", out var owned)
                && owned.TryGetProperty("address", out var addr))
                avatar.MetaData["OwnerAddress"] = addr.GetString() ?? string.Empty;

            avatar.MetaData["LensProfileId"] = (avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.LensOASIS, out var __pk) ? __pk : string.Empty);
            avatar.MetaData["Provider"] = "LensOASIS";
            return avatar;
        }

        private static Holon MapPublicationToHolon(JsonElement pub)
        {
            var holon = new Holon();

            if (pub.TryGetProperty("id", out var id))
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensOASIS] = id.GetString() ?? string.Empty;

            string content = string.Empty;
            if (pub.TryGetProperty("metadata", out var meta) && meta.ValueKind != JsonValueKind.Null
                && meta.TryGetProperty("content", out var contentEl))
                content = contentEl.GetString() ?? string.Empty;

            holon.Name = content.Length > 80 ? content[..80] : content;
            holon.Description = content;

            if (pub.TryGetProperty("by", out var by)
                && by.TryGetProperty("handle", out var handle)
                && handle.TryGetProperty("fullHandle", out var fullHandle))
                holon.MetaData["AuthorHandle"] = fullHandle.GetString() ?? string.Empty;

            if (pub.TryGetProperty("createdAt", out var createdAt))
                holon.MetaData["CreatedAt"] = createdAt.GetString() ?? string.Empty;

            if (pub.TryGetProperty("stats", out var stats))
            {
                if (stats.TryGetProperty("reactions", out var reactions))
                    holon.MetaData["Reactions"] = reactions.GetInt64();
                if (stats.TryGetProperty("comments", out var comments))
                    holon.MetaData["Comments"] = comments.GetInt64();
                if (stats.TryGetProperty("mirrors", out var mirrors))
                    holon.MetaData["Mirrors"] = mirrors.GetInt64();
            }

            holon.MetaData["LensPublicationId"] = (holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.LensOASIS, out var __pk) ? __pk : string.Empty);
            holon.MetaData["Provider"] = "LensOASIS";
            return holon;
        }

        private static string EscapeGql(string gql)
            => gql.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            var avatar = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message);
            else
                result.Result = loaded.Result;

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData != null
                    && holon.MetaData.TryGetValue(metaKey, out var value)
                    && value?.ToString() == metaValue)
                    matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
            {
                result.Result = new List<IHolon>(all.Result);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData == null) continue;

                var matched = 0;
                foreach (var pair in metaKeyValuePairs)
                {
                    if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                        matched++;
                }

                var isMatch = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? matched == metaKeyValuePairs.Count
                    : matched > 0;

                if (isMatch) matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = true;
            return result;
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }


        // ─── Avatar details ──────────────────────────────────────────────────────
        // A Lens profile carries the same data an OASIS AvatarDetail holds, so the
        // detail loaders reuse the avatar fetch paths and re-project the profile.

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            var avatar = await LoadAvatarAsync(id, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            result.Result = MapAvatarToDetail(avatar.Result);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            result.Result = MapAvatarToDetail(avatar.Result);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
            => LoadAvatarDetailByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmail, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            result.Result = MapAvatarToDetail(avatar.Result);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
            => LoadAvatarDetailByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            var avatars = await LoadAllAvatarsAsync(version);
            if (avatars.IsError || avatars.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatars.Message);
                return result;
            }

            var details = new List<IAvatarDetail>();
            foreach (var avatar in avatars.Result)
            {
                if (avatar != null)
                    details.Add(MapAvatarToDetail(avatar));
            }

            result.Result = details;
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        /// <summary>
        /// Projects a Lens-sourced Avatar onto an AvatarDetail, carrying across the
        /// profile fields Lens exposes (display name, bio, image, follower stats).
        /// </summary>
        private static AvatarDetail MapAvatarToDetail(IAvatar avatar)
        {
            var detail = new AvatarDetail
            {
                Id = avatar.Id,
                Username = avatar.Username,
                Email = avatar.Email,
                Portrait = avatar.MetaData != null && avatar.MetaData.TryGetValue("LensPictureUrl", out var __pic) ? __pic?.ToString() ?? string.Empty : string.Empty,
            };

            if (avatar.MetaData != null)
            {
                foreach (var kvp in avatar.MetaData)
                    detail.MetaData[kvp.Key] = kvp.Value;
            }

            return detail;
        }

        // ─── Delete holon by provider key ────────────────────────────────────────
        // The Lens provider key is the publication id; resolve it then delegate to
        // the id-based delete so the same write-policy check applies.

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            var holon = await LoadHolonAsync(providerKey);
            if (holon.IsError || holon.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, holon.Message);
                return result;
            }
            return await DeleteHolonAsync(holon.Result.Id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();


        // ─── IOASISNETProvider ───────────────────────────────────────────────────
        // Lens Protocol profiles and publications carry no geolocation data, so
        // there is nothing to resolve a proximity query against.

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "LensOASIS: Geolocation is not supported - Lens Protocol profiles carry no location data.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "LensOASIS: Geolocation is not supported - Lens Protocol publications carry no location data.");
            return result;
        }

    }
}
