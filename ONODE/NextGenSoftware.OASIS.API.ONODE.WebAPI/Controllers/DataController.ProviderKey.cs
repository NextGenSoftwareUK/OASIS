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

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        // ─────────────────────────────────────────────────────────────────────────
        // LoadHolonByProviderKey
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a holon by its provider-specific storage key.
        /// </summary>
        [Authorize]
        [HttpGet("load-holon-by-providerkey/{providerKey}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<Holon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolonByProviderKey(string providerKey,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false,
            string childHolonType = "All", int version = 0)
        {
            var response = new OASISHttpResponseMessage<Holon>();
            OASISHttpResponseMessage<Holon> validatedResponse;
            (validatedResponse, HolonType childHolonTypeEnum) = ValidateHolonType<Holon>(childHolonType);
            if (validatedResponse.Result.IsError)
                return validatedResponse;

            try
            {
                var result = await HolonManager.LoadHolonAsync(providerKey, loadChildren, recursive,
                    maxChildDepth, continueOnError, loadChildrenFromProvider, childHolonTypeEnum, version);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                var holon = (Holon)result.Result;

                if (holon != null && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                {
                    // Whole holon, or only the fields this avatar may read (per-holon and per-field permissions).
                    holon = await HolonManager.ProjectForAvatarAsync(holon, AvatarId) as Holon;
                    if (holon == null)
                        return TestDataHelper.CreateErrorResponse<Holon>(
                            "Forbidden. You do not have permission to access this holon.", null, System.Net.HttpStatusCode.Forbidden);
                }

                response.Result.Result = holon;
                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<Holon>($"Error loading holon by providerKey: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }


        // ─────────────────────────────────────────────────────────────────────────
        // LoadHolonsForParentByProviderKey
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all child holons for the parent identified by the given provider key.
        /// </summary>
        [Authorize]
        [HttpGet("load-holons-for-parent-by-providerkey/{providerKey}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<Holon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParentByProviderKey(string providerKey,
            string holonType = "All", bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
            string childHolonType = "All", int version = 0,
            bool searchOnlyForCurrentAvatar = true, bool includePublic = true)
        {
            var response = new OASISHttpResponseMessage<IEnumerable<Holon>>();
            OASISHttpResponseMessage<IEnumerable<Holon>> validatedResponse;
            (validatedResponse, HolonType holonTypeEnum) = ValidateHolonType<IEnumerable<Holon>>(holonType);
            if (validatedResponse.Result.IsError)
                return validatedResponse;
            (validatedResponse, HolonType childHolonTypeEnum) = ValidateHolonType<IEnumerable<Holon>>(childHolonType);
            if (validatedResponse.Result.IsError)
                return validatedResponse;

            try
            {
                var result = await HolonManager.LoadHolonsForParentAsync(providerKey, holonTypeEnum,
                    loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider,
                    0, childHolonTypeEnum, version);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                var holons = Mapper.Convert<IHolon, Holon>(result.Result)?.ToList() ?? new List<Holon>();

                if (Avatar?.AvatarType?.Value != AvatarType.Wizard)
                {
                    if (searchOnlyForCurrentAvatar)
                        holons = holons.Where(h => h.CreatedByAvatarId == AvatarId).ToList();
                    else if (includePublic)
                        holons = (await HolonManager.ProjectForAvatarAsync(holons, AvatarId)).OfType<Holon>().ToList();
                    else
                        return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>(
                            "Forbidden. Returning all holons requires a Wizard avatar.", null, System.Net.HttpStatusCode.Forbidden);
                }

                response.Result.Result = holons;
                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error loading holons for parent by providerKey: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Loads all child holons for the parent identified by the given provider key, filtered by holon type.
        /// </summary>
        [Authorize]
        [HttpGet("load-holons-for-parent-by-providerkey/{providerKey}/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParentByProviderKey(string providerKey, string holonType,
            bool searchOnlyForCurrentAvatar = true, bool includePublic = true)
        {
            return await LoadHolonsForParentByProviderKey(providerKey, holonType,
                loadChildren: true, recursive: true, maxChildDepth: 0, continueOnError: true,
                loadChildrenFromProvider: false, childHolonType: "All", version: 0,
                searchOnlyForCurrentAvatar: searchOnlyForCurrentAvatar, includePublic: includePublic);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // DeleteHolonByProviderKey
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes a holon identified by its provider-specific storage key. Defaults to soft-delete.
        /// </summary>
        [Authorize]
        [HttpDelete("delete-holon-by-providerkey/{providerKey}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<Holon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<Holon>> DeleteHolonByProviderKey(string providerKey, bool softDelete = true)
        {
            var engineMode = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.HyperDriveMode;
            var mode = HolonWriteEndpoint.ValidateEngineMode<Holon>(engineMode);
            if (mode.IsError)
            {
                var status = HolonWriteEndpoint.Status(mode);
                Response.StatusCode = (int)status;
                return HttpResponseHelper.FormatResponse(mode, status, false);
            }
            if (engineMode == NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2)
            {
                // Keep this alias on the canonical request-scoped conditional
                // delete boundary. A key never substitutes for logical intent.
                var idHeader = Request.Headers[HolonWriteEndpoint.HolonIdHeader].ToString();
                if (!Guid.TryParseExact(idHeader, "D", out var id) || id == Guid.Empty ||
                    string.IsNullOrWhiteSpace(providerKey) || providerKey.Length > 4096)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return TestDataHelper.CreateErrorResponse<Holon>("A canonical nonempty holon ID header and bounded native key are required.",
                        null, System.Net.HttpStatusCode.BadRequest);
                }
                var deleted = await DeleteHolon(new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.DeleteHolonRequest
                    { Id = id, ProviderKey = providerKey, SoftDelete = softDelete,
                        ProviderType = Request.Query.TryGetValue("providerType", out var selectedProvider)
                            ? selectedProvider.ToString() : "Default" });
                var converted = new OASISHttpResponseMessage<Holon>();
                converted.Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, Holon>(deleted.Result);
                converted.Result.Result = deleted.Result.Result == null ? null : (Holon)deleted.Result.Result;
                return deleted.WithResult(converted.Result);
            }
            var response = new OASISHttpResponseMessage<Holon>();
            try
            {
                // Ownership check before delete
                if (Avatar?.AvatarType?.Value != AvatarType.Wizard)
                {
                    var existing = await HolonManager.LoadHolonAsync(providerKey);
                    if (existing == null || existing.IsError || existing.Result == null)
                        return TestDataHelper.CreateErrorResponse<Holon>("Holon not found.", null, System.Net.HttpStatusCode.NotFound);
                    if (existing.Result.CreatedByAvatarId != AvatarId)
                        return TestDataHelper.CreateErrorResponse<Holon>("Forbidden. You do not have permission to delete this holon.", null, System.Net.HttpStatusCode.Forbidden);
                }

                var result = await HolonManager.DeleteHolonAsync(providerKey, AvatarId, softDelete);

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                response.Result.Result = (Holon)result.Result;
                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<Holon>($"Error deleting holon by providerKey: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}
