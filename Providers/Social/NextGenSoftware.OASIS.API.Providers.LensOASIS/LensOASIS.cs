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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
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

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "LensOASIS: Use LoadHolonsForParentAsync(providerKey) with a Lens profile address to load that profile's posts.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version).Result;

        /// <summary>Loads all posts by a Lens profile. providerKey = EVM address or profile ID.</summary>
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
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

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0)
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

        public override async Task<OASISResult<IAvatar>> SaveAvatarDetailAsync(IAvatarDetail avatar)
            => await Task.FromResult(WriteNotSupported<IAvatar>("avatar detail"));
        public override OASISResult<IAvatar> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => await Task.FromResult(WriteNotSupported<IHolon>("holon (post)"));
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => await Task.FromResult(WriteNotSupported<IEnumerable<IHolon>>("holons"));
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("avatar"));
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("holon"));
        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
            => await Task.FromResult(WriteNotSupported<bool>("holon"));
        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true) => DeleteHolonAsync(providerKey, softDelete).Result;

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

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var query = searchParams?.SearchQuery ?? string.Empty;
                const string gql = @"
query Search($query: String!) {
  searchProfiles(request: { query: $query, limit: TEN }) {
    items {
      id
      handle { fullHandle localName }
      metadata { displayName bio }
      stats { followers following posts }
      ownedBy { address }
    }
  }
}";
                var payload = JsonSerializer.Serialize(new
                {
                    query = gql,
                    variables = new { query }
                });

                var doc = await PostGraphQlAsync(payload);
                var searchResults = new SearchResults();

                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("searchProfiles", out var search)
                    && search.TryGetProperty("items", out var items))
                {
                    foreach (var profileEl in items.EnumerateArray())
                    {
                        var avatar = MapProfileToAvatar(profileEl);
                        searchResults.Avatars.Add(new SearchResult { Avatar = avatar });
                    }
                }

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Lens search for '{query}' returned {searchResults.Avatars.Count} profiles.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"LensOASIS.SearchAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

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
                avatar.ProviderKey = idEl.GetString() ?? string.Empty;

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
                    avatar.AvatarImageUrl = uri.GetString() ?? string.Empty;
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

            avatar.MetaData["LensProfileId"] = avatar.ProviderKey;
            avatar.MetaData["Provider"] = "LensOASIS";
            return avatar;
        }

        private static Holon MapPublicationToHolon(JsonElement pub)
        {
            var holon = new Holon();

            if (pub.TryGetProperty("id", out var id))
                holon.ProviderKey = id.GetString() ?? string.Empty;

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

            holon.MetaData["LensPublicationId"] = holon.ProviderKey;
            holon.MetaData["Provider"] = "LensOASIS";
            return holon;
        }

        private static string EscapeGql(string gql)
            => gql.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }
}
