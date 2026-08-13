using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public partial class GamesController : STARControllerBase
    {

        /// <summary>
        /// Gets shared inventory items (keycards, items, etc.) that can be used across games, apps, websites, services
        /// This uses the AvatarDetail.Inventory property - the avatar's actual owned inventory
        /// </summary>
        [HttpGet("shared-inventory")]
        [ProducesResponseType(typeof(OASISResult<List<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<List<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSharedInventory()
        {
            try
            {
                var result = await _starAPI.Game.GetSharedAssetsAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error getting shared inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Adds an item to the avatar's shared inventory (can be used across all games, apps, websites, services)
        /// </summary>
        [HttpPost("shared-inventory/add")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItemToInventory([FromBody] InventoryItem item)
        {
            try
            {
                var result = await _starAPI.Game.AddItemToInventoryAsync(AvatarId, item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<IInventoryItem>(ex, "adding item to inventory");
            }
        }

        /// <summary>
        /// Removes an item from the avatar's shared inventory
        /// </summary>
        [HttpDelete("shared-inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveItemFromInventory(Guid itemId)
        {
            try
            {
                var result = await _starAPI.Game.RemoveItemFromInventoryAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "removing item from inventory");
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their shared inventory
        /// </summary>
        [HttpGet("shared-inventory/{itemId}/has")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItem(Guid itemId)
        {
            try
            {
                var result = await _starAPI.Game.HasItemAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "checking for item");
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their shared inventory
        /// </summary>
        [HttpGet("shared-inventory/has-by-name")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItemByName([FromQuery] string itemName)
        {
            try
            {
                var result = await _starAPI.Game.HasItemByNameAsync(AvatarId, itemName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "checking for item by name");
            }
        }

        /// <summary>
        /// Gets active quests that span multiple games
        /// </summary>
        [HttpGet("cross-game-quests")]
        [ProducesResponseType(typeof(OASISResult<List<IQuestBase>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<List<IQuestBase>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCrossGameQuests()
        {
            try
            {
                var result = await _starAPI.Game.GetCrossGameQuestsAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<IQuestBase>>
                {
                    IsError = true,
                    Message = $"Error getting cross-game quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets avatar's karma score (shared across all games)
        /// </summary>
        [HttpGet("karma")]
        [ProducesResponseType(typeof(OASISResult<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvatarKarma()
        {
            try
            {
                var result = await _starAPI.Game.GetAvatarKarmaAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "getting karma");
            }
        }

    }


}
