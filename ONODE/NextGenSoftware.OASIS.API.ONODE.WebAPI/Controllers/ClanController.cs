using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Clan;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Clan management: create, update, load, list, delete, add/remove members, get members.
    /// Send item to clan is available at POST api/avatar/inventory/send-to-clan (Target = clan name).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ClanController : OASISControllerBase
    {
        private static ClanManager ClanManager => ClanManager.Instance;

        /// <summary>Create a new clan. The authenticated avatar becomes the owner and first member.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IClan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IClan>> Create([FromBody] CreateClanRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                    {
                        IsError = true,
                        Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                    {
                        IsError = true,
                        Message = "Clan name is required."
                    }, HttpStatusCode.BadRequest);
                }
                var result = await ClanManager.CreateClanAsync(AvatarId, request.Name.Trim(), request.Description?.Trim());
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                {
                    IsError = true,
                    Message = $"Error creating clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Update clan name/description. Caller should be owner (enforcement can be added).</summary>
        [HttpPut("{clanId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IClan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IClan>> Update(Guid clanId, [FromBody] UpdateClanRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                    {
                        IsError = true,
                        Message = "Clan name is required."
                    }, HttpStatusCode.BadRequest);
                }
                var loadResult = await ClanManager.LoadClanAsync(clanId);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                    {
                        IsError = true,
                        Message = loadResult.Message ?? "Clan not found."
                    }, HttpStatusCode.NotFound);
                }
                var clan = loadResult.Result;
                clan.Name = request.Name.Trim();
                clan.Description = request.Description?.Trim() ?? "";
                var result = await ClanManager.UpdateClanAsync(clan);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                {
                    IsError = true,
                    Message = $"Error updating clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Load a clan by Id.</summary>
        [HttpGet("{clanId:guid}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IClan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status404NotFound)]
        public async Task<OASISHttpResponseMessage<IClan>> Load(Guid clanId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.LoadClanAsync(clanId, providerType ?? ProviderType.Default);
                if (result.IsError && result.Result == null)
                    return HttpResponseHelper.FormatResponse(result, HttpStatusCode.NotFound);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                {
                    IsError = true,
                    Message = $"Error loading clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Load a clan by name (case-insensitive).</summary>
        [HttpGet("by-name")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IClan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status404NotFound)]
        public async Task<OASISHttpResponseMessage<IClan>> LoadByName([FromQuery] string name, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                    {
                        IsError = true,
                        Message = "Clan name (query 'name') is required."
                    }, HttpStatusCode.BadRequest);
                }
                var result = await ClanManager.LoadClanByNameAsync(name.Trim(), providerType ?? ProviderType.Default);
                if (result.IsError && result.Result == null)
                    return HttpResponseHelper.FormatResponse(result, HttpStatusCode.NotFound);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IClan>
                {
                    IsError = true,
                    Message = $"Error loading clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>List all clans, optionally filtered by owner avatar Id.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IClan>>), StatusCodes.Status200OK)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IClan>>> List([FromQuery] Guid? ownerAvatarId = null, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.ListClansAsync(ownerAvatarId, providerType ?? ProviderType.Default);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IClan>>
                {
                    IsError = true,
                    Message = $"Error listing clans: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Delete a clan. Typically only the owner may delete (idempotent if not found).</summary>
        [HttpDelete("{clanId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status404NotFound)]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid clanId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.DeleteClanAsync(clanId, providerType ?? ProviderType.Default);
                if (result.IsError && !result.Result)
                    return HttpResponseHelper.FormatResponse(result, HttpStatusCode.NotFound);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Add an avatar to a clan as a member.</summary>
        [HttpPost("{clanId:guid}/members/{avatarId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AddAvatarToClan(Guid clanId, Guid avatarId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.AddAvatarToClanAsync(clanId, avatarId, providerType ?? ProviderType.Default);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error adding avatar to clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Remove an avatar from a clan. Owner cannot be removed.</summary>
        [HttpDelete("{clanId:guid}/members/{avatarId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> RemoveAvatarFromClan(Guid clanId, Guid avatarId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.RemoveAvatarFromClanAsync(clanId, avatarId, providerType ?? ProviderType.Default);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error removing avatar from clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Get clan member avatar Ids.</summary>
        [HttpGet("{clanId:guid}/members")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<Guid>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status404NotFound)]
        public async Task<OASISHttpResponseMessage<IEnumerable<Guid>>> GetMembers(Guid clanId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var result = await ClanManager.GetClanMembersAsync(clanId, providerType ?? ProviderType.Default);
                if (result.IsError && result.Result == null)
                    return HttpResponseHelper.FormatResponse(result, HttpStatusCode.NotFound);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<Guid>>
                {
                    IsError = true,
                    Message = $"Error loading clan members: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>Get clan treasury (inventory).</summary>
        [HttpGet("{clanId:guid}/inventory")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status404NotFound)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> GetClanInventory(Guid clanId, [FromQuery] ProviderType? providerType = null)
        {
            try
            {
                var loadResult = await ClanManager.LoadClanAsync(clanId, providerType ?? ProviderType.Default);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = loadResult.Message ?? "Clan not found."
                    }, HttpStatusCode.NotFound);
                }
                var list = loadResult.Result.Inventory ?? new List<IInventoryItem>();
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    Result = list,
                    Message = $"Clan inventory: {list.Count} items."
                });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading clan inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }
    }
}
