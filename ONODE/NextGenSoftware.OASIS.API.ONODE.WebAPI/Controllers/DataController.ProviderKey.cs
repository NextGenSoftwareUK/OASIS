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
                response.Result.Result = (Holon)result.Result;
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
            string childHolonType = "All", int version = 0)
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
                response.Result.Result = Mapper.Convert<IHolon, Holon>(result.Result)?.ToList();
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
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParentByProviderKey(string providerKey, string holonType)
        {
            return await LoadHolonsForParentByProviderKey(providerKey, holonType,
                loadChildren: true, recursive: true, maxChildDepth: 0, continueOnError: true,
                loadChildrenFromProvider: false, childHolonType: "All", version: 0);
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
            var response = new OASISHttpResponseMessage<Holon>();
            try
            {
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
