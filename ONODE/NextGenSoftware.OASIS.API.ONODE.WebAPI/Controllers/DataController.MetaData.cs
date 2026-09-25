using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        // ─────────────────────────────────────────────────────────────────────────
        // LoadHolonByMetaData  (single holon)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a single holon whose metadata contains the given key/value pair.
        /// For a multi-key match, supply MetaKeyValuePairs and MetaKeyValuePairMatchMode instead of MetaKey/MetaValue.
        /// </summary>
        [Authorize]
        [HttpPost("load-holon-by-metadata")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<Holon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolonByMetaData(LoadHolonByMetaDataRequest request)
        {
            OASISHttpResponseMessage<Holon> response;
            (response, HolonType holonType) = ValidateHolonType<Holon>(request.HolonType);
            (response, HolonType childHolonType) = ValidateHolonType<Holon>(request.ChildHolonType);

            if (response.Result.IsError)
                return response;

            OASISConfigResult<Holon> configResult = await ConfigureOASISEngineAsync<Holon>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            try
            {
                OASISResult<IHolon> result;

                if (request.MetaKeyValuePairs != null && request.MetaKeyValuePairs.Count > 0)
                {
                    if (!Enum.TryParse<MetaKeyValuePairMatchMode>(request.MetaKeyValuePairMatchMode, true, out var matchMode))
                        matchMode = MetaKeyValuePairMatchMode.All;

                    result = await HolonManager.LoadHolonByMetaDataAsync(request.MetaKeyValuePairs, matchMode, holonType,
                        request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError,
                        request.LoadChildrenFromProvider, childHolonType, request.Version);
                }
                else
                {
                    result = await HolonManager.LoadHolonByMetaDataAsync(request.MetaKey, request.MetaValue, holonType,
                        request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError,
                        request.LoadChildrenFromProvider, childHolonType, request.Version);
                }

                ResetOASISSettings(request, configResult);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                var holon = (Holon)result.Result;

                if (holon != null && Avatar?.AvatarType?.Value != AvatarType.Wizard
                    && holon.CreatedByAvatarId != AvatarId && !holon.IsPublic)
                    return TestDataHelper.CreateErrorResponse<Holon>(
                        "Forbidden. You do not have permission to access this holon.", null, System.Net.HttpStatusCode.Forbidden);

                response.Result.Result = holon;

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                ResetOASISSettings(request, configResult);
                return TestDataHelper.CreateErrorResponse<Holon>($"Error loading holon by metadata: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Loads a single holon whose metadata contains the given key/value pair.
        /// </summary>
        [Authorize]
        [HttpGet("load-holon-by-metadata/{metaKey}/{metaValue}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolonByMetaData(string metaKey, string metaValue)
        {
            return await LoadHolonByMetaData(new LoadHolonByMetaDataRequest { MetaKey = metaKey, MetaValue = metaValue });
        }

        /// <summary>
        /// Loads a single holon whose metadata contains the given key/value pair, scoped to a HolonType.
        /// </summary>
        [Authorize]
        [HttpGet("load-holon-by-metadata/{metaKey}/{metaValue}/{holonType}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolonByMetaData(string metaKey, string metaValue, string holonType)
        {
            return await LoadHolonByMetaData(new LoadHolonByMetaDataRequest { MetaKey = metaKey, MetaValue = metaValue, HolonType = holonType });
        }

        /// <summary>
        /// Loads a single holon whose metadata contains the given key/value pair, with full child-load options.
        /// </summary>
        [Authorize]
        [HttpGet("load-holon-by-metadata/{metaKey}/{metaValue}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolonByMetaData(string metaKey, string metaValue, string holonType,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolonByMetaData(new LoadHolonByMetaDataRequest
            {
                MetaKey = metaKey, MetaValue = metaValue, HolonType = holonType,
                LoadChildren = loadChildren, Recursive = recursive, MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError, Version = version
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // LoadHolonsByMetaData  (collection)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all holons whose metadata contains the given key/value pair.
        /// For a multi-key match, supply MetaKeyValuePairs and MetaKeyValuePairMatchMode instead of MetaKey/MetaValue.
        /// </summary>
        [Authorize]
        [HttpPost("load-holons-by-metadata")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<Holon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsByMetaData(LoadHolonsByMetaDataRequest request)
        {
            OASISHttpResponseMessage<IEnumerable<Holon>> response;
            (response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);
            (response, HolonType childHolonType) = ValidateHolonType<IEnumerable<Holon>>(request.ChildHolonType);

            if (response.Result.IsError)
                return response;

            OASISConfigResult<IEnumerable<Holon>> configResult = ConfigureOASISEngine<IEnumerable<Holon>>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            try
            {
                OASISResult<IEnumerable<IHolon>> result;

                if (request.MetaKeyValuePairs != null && request.MetaKeyValuePairs.Count > 0)
                {
                    if (!Enum.TryParse<MetaKeyValuePairMatchMode>(request.MetaKeyValuePairMatchMode, true, out var matchMode))
                        matchMode = MetaKeyValuePairMatchMode.All;

                    result = await HolonManager.LoadHolonsByMetaDataAsync(request.MetaKeyValuePairs, matchMode, holonType,
                        request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError,
                        request.LoadChildrenFromProvider, 0, childHolonType, request.Version, avatarId: AvatarId, includePublic: request.IncludePublic);
                }
                else
                {
                    result = await HolonManager.LoadHolonsByMetaDataAsync(request.MetaKey, request.MetaValue, holonType,
                        request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError,
                        request.LoadChildrenFromProvider, 0, childHolonType, request.Version, avatarId: AvatarId, includePublic: request.IncludePublic);
                }


                ResetOASISSettings(request, configResult);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                var holons = Mapper.Convert<IHolon, Holon>(result.Result)?.ToList() ?? new List<Holon>();
                response.Result.Result = holons;

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                ResetOASISSettings(request, configResult);
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error loading holons by metadata: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Loads all holons whose metadata contains the given key/value pair.
        /// </summary>
        [Authorize]
        [HttpGet("load-holons-by-metadata/{metaKey}/{metaValue}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsByMetaData(string metaKey, string metaValue)
        {
            return await LoadHolonsByMetaData(new LoadHolonsByMetaDataRequest { MetaKey = metaKey, MetaValue = metaValue });
        }

        /// <summary>
        /// Loads all holons whose metadata contains the given key/value pair, scoped to a HolonType.
        /// </summary>
        [Authorize]
        [HttpGet("load-holons-by-metadata/{metaKey}/{metaValue}/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsByMetaData(string metaKey, string metaValue, string holonType)
        {
            return await LoadHolonsByMetaData(new LoadHolonsByMetaDataRequest { MetaKey = metaKey, MetaValue = metaValue, HolonType = holonType });
        }

        /// <summary>
        /// Loads all holons whose metadata contains the given key/value pair, with full child-load options.
        /// </summary>
        [Authorize]
        [HttpGet("load-holons-by-metadata/{metaKey}/{metaValue}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsByMetaData(string metaKey, string metaValue, string holonType,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolonsByMetaData(new LoadHolonsByMetaDataRequest
            {
                MetaKey = metaKey, MetaValue = metaValue, HolonType = holonType,
                LoadChildren = loadChildren, Recursive = recursive, MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError, Version = version
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // SearchHolons
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Searches holons by a text term, optionally filtering by HolonType, parent, and metadata key/value pairs.
        /// The avatarId is derived from the authenticated JWT; searchOnlyForCurrentAvatar defaults to true.
        /// </summary>
        [Authorize]
        [HttpPost("search-holons")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<Holon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> SearchHolons(SearchHolonsRequest request)
        {
            OASISHttpResponseMessage<IEnumerable<Holon>> response;
            (response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);

            if (response.Result.IsError)
                return response;

            if (!Enum.TryParse<MetaKeyValuePairMatchMode>(request.MetaKeyValuePairMatchMode, true, out var matchMode))
                matchMode = MetaKeyValuePairMatchMode.All;

            try
            {
                // IncludePublic=false → search own only; IncludePublic=true → search all then filter
                var result = await HolonManager.SearchHolonsAsync(
                    request.SearchTerm, AvatarId, request.ParentId,
                    request.FilterByMetaData, matchMode, !request.IncludePublic,
                    holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth,
                    request.ContinueOnError, request.LoadChildrenFromProvider);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                var searchHolons = Mapper.Convert<IHolon, Holon>(result.Result)?.ToList() ?? new List<Holon>();

                // When IncludePublic=true, SearchManager returned all holons — apply visibility filter unless Wizard
                if (request.IncludePublic && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                    searchHolons = searchHolons.Where(h => h.CreatedByAvatarId == AvatarId || h.IsPublic).ToList();

                response.Result.Result = searchHolons;

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error searching holons: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Searches holons by a text term across all holon types for the current avatar.
        /// </summary>
        [Authorize]
        [HttpGet("search-holons/{searchTerm}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> SearchHolons(string searchTerm)
        {
            return await SearchHolons(new SearchHolonsRequest { SearchTerm = searchTerm });
        }

        /// <summary>
        /// Searches holons by a text term, scoped to a HolonType.
        /// </summary>
        [Authorize]
        [HttpGet("search-holons/{searchTerm}/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> SearchHolons(string searchTerm, string holonType)
        {
            return await SearchHolons(new SearchHolonsRequest { SearchTerm = searchTerm, HolonType = holonType });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // SaveHolons  (bulk)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves a collection of holons in a single call.
        /// </summary>
        [Authorize]
        [HttpPost("save-holons")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<Holon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> SaveHolons(SaveHolonsRequest request)
        {
            var response = new OASISHttpResponseMessage<IEnumerable<Holon>>();

            OASISConfigResult<IEnumerable<Holon>> configResult = ConfigureOASISEngine<IEnumerable<Holon>>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            // For non-Wizards, verify ownership of any holon being updated (non-empty Id)
            if (Avatar?.AvatarType?.Value != AvatarType.Wizard)
            {
                foreach (var holon in request.Holons.Where(h => h.Id != Guid.Empty))
                {
                    var existing = await HolonManager.LoadHolonAsync(holon.Id);
                    if (existing != null && !existing.IsError && existing.Result != null
                        && existing.Result.CreatedByAvatarId != AvatarId)
                        return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>(
                            $"Forbidden. You do not have permission to update holon {holon.Id}.", null, System.Net.HttpStatusCode.Forbidden);
                }
            }

            try
            {
                var result = await HolonManager.SaveHolonsAsync(
                    request.Holons.Cast<IHolon>(), AvatarId,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError);

                ResetOASISSettings(request, configResult);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                response.Result.Result = Mapper.Convert<IHolon, Holon>(result.Result)?.ToList();

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                ResetOASISSettings(request, configResult);
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error saving holons: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}
