using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Holon;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>Search actual provider records using explicit text, number or date groups.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableCors()]
    public class SearchController : OASISControllerBase
    {
        private SearchManager _SearchManager;
        private static readonly JsonSerializerOptions SearchJson = CreateSearchJson();

        private SearchManager SearchManager
        {
            get
            {
                if (_SearchManager == null)
                {
                    if (AdmittedStorageManagerAcquisition.TryCreate(
                        () => OASISBootLoader.OASISBootLoader.OASISDNA,
                        () => ProviderManager.Instance.CurrentStorageProvider,
                        (provider, dna) => new SearchManager(null, dna), out SearchManager admitted, admissionOnly: false))
                        return _SearchManager = admitted;

                    var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError || result.Result == null)
                        throw new InvalidOperationException("The search storage provider is unavailable.");
                    _SearchManager = new SearchManager(result.Result);
                }
                return _SearchManager;
            }
        }

        /// <summary>Search with a URL-encoded JSON request. Each group requires kind: text, number or date.</summary>
        [HttpGet("{searchParams}")]
        [ProducesResponseType(typeof(OASISResult<ISearchResults>), StatusCodes.Status200OK)]
        public async Task<OASISResult<ISearchResults>> Get(string searchParams)
            => await ExecuteSearchAsync(searchParams, ProviderType.Default);

        [HttpGet("{searchParams}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<ISearchResults>> Get(string searchParams, ProviderType providerType, bool setGlobally = false)
        {
            if (setGlobally)
                return RejectSearch("SEARCH_GLOBAL_PROVIDER_CHANGE_REFUSED",
                    "Search selects its provider per request; use provider administration to change global settings.", StatusCodes.Status400BadRequest);
            return await ExecuteSearchAsync(searchParams, providerType);
        }

        /// <summary>Search using JSON with explicit group kinds and concrete field-selection objects.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<ISearchResults>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISearchResults>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<ISearchResults>> Search([FromBody] JsonElement searchParams)
            => await ExecuteSearchAsync(searchParams.GetRawText(), ProviderType.Default);

        private async Task<OASISResult<ISearchResults>> ExecuteSearchAsync(string json, ProviderType provider)
        {
            SearchParams parameters;
            try
            {
                parameters = JsonSerializer.Deserialize<SearchParams>(json, SearchJson);
                if (parameters == null)
                    return RejectSearch("SEARCH_PARAMS_REQUIRED", "Search parameters are required.", StatusCodes.Status400BadRequest);
                if (parameters.SearchOnlyForCurrentAvatar)
                    parameters.AvatarId = AvatarId;
                if (parameters.SearchOnlyForCurrentAvatar && parameters.AvatarId == Guid.Empty)
                    return RejectSearch("SEARCH_AVATAR_REQUIRED", "Sign in before searching your records.", StatusCodes.Status401Unauthorized);
            }
            catch (JsonException error)
            {
                return RejectSearch("SEARCH_PARAMS_INVALID", error.Message, StatusCodes.Status400BadRequest);
            }
            try
            {
                var result = await SearchManager.SearchAsync(parameters, provider,
                    loadChildren: false, recursive: parameters.Recursive);
                if (result == null || !result.IsError && result.Result == null)
                    return RejectSearch("SEARCH_RESULT_MISSING", "The search provider returned no result.", StatusCodes.Status503ServiceUnavailable);
                return result;
            }
            catch (Exception error)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return new OASISResult<ISearchResults>
                {
                    IsError = true, ErrorCode = "SEARCH_PROVIDER_FAILED",
                    Message = "The provider could not complete the search.", Exception = error
                };
            }
        }

        private OASISResult<ISearchResults> RejectSearch(string code, string message, int status)
        {
            Response.StatusCode = status;
            return new OASISResult<ISearchResults> { IsError = true, ErrorCode = code, Message = message };
        }

        private static JsonSerializerOptions CreateSearchJson()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new SearchGroupConverter());
            options.Converters.Add(new SearchInterfaceConverter<ISearchHolonParams, SearchHolonParams>());
            options.Converters.Add(new SearchInterfaceConverter<ISearchAvatarParams, SearchAvatarParams>());
            options.Converters.Add(new SearchInterfaceConverter<ISearchAvatarWalletParams, SearchAvatarWalletParams>());
            options.Converters.Add(new SearchInterfaceConverter<ISearchAvatarWalletTransactionParams, SearchAvatarWalletTransactionParams>());
            return options;
        }

        private sealed class SearchInterfaceConverter<TInterface, TConcrete> : JsonConverter<TInterface>
            where TConcrete : class, TInterface
        {
            public override TInterface Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
                => JsonSerializer.Deserialize<TConcrete>(ref reader, options);
            public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
                => JsonSerializer.Serialize(writer, (TConcrete)(object)value, options);
        }

        private sealed class SearchGroupConverter : JsonConverter<ISearchGroupBase>
        {
            public override ISearchGroupBase Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                var value = document.RootElement;
                if (value.ValueKind != JsonValueKind.Object ||
                    !value.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String)
                    throw new JsonException("Each search group requires kind: text, number or date.");
                return kind.GetString() switch
                {
                    "text" => value.Deserialize<SearchTextGroup>(options),
                    "number" => value.Deserialize<SearchNumberGroup>(options),
                    "date" => value.Deserialize<SearchDateGroup>(options),
                    _ => throw new JsonException("Search group kind must be text, number or date.")
                };
            }
            public override void Write(Utf8JsonWriter writer, ISearchGroupBase value, JsonSerializerOptions options)
                => throw new NotSupportedException("The search request converter is read-only.");
        }
    }
}
