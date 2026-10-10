using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/gifts")]
    public class GiftsController : OASISControllerBase
    {
        public GiftsController()
        {
        }

        /// <summary>
        /// Get all gifts for the current avatar
        /// </summary>
        /// <returns>List of gifts</returns>
        [Authorize]
        [HttpGet("my-gifts")]
        public async Task<OASISResult<List<Gift>>> GetMyGifts()
        {
            try
            {
                OASISResult<List<Gift>> result = null;
                try
                {
                    result = await GiftsManager.Instance.GetAllGiftsAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<Gift>>
                    {
                        Result = new List<Gift>(),
                        IsError = false,
                        Message = "Gifts retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<Gift>>
                    {
                        Result = new List<Gift>(),
                        IsError = false,
                        Message = "Gifts retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<Gift>>
                {
                    IsError = true,
                    Message = $"Error retrieving gifts: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Send a gift to another avatar
        /// </summary>
        /// <param name="toAvatarId">Recipient avatar ID</param>
        /// <param name="giftType">Type of gift</param>
        /// <param name="message">Optional message</param>
        /// <param name="metadata">Optional metadata</param>
        /// <returns>Sent gift details</returns>
        [Authorize]
        [HttpPost("send-gift/{toAvatarId}")]
        //public async Task<OASISResult<Gift>> SendGift(Guid toAvatarId, [FromBody] GiftType giftType, [FromBody] string message = null, [FromBody] Dictionary<string, object> metadata = null)
        public async Task<OASISResult<Gift>> SendGift(Guid toAvatarId, GiftType giftType, string message = null, Dictionary<string, object> metadata = null)
        {
            return await GiftsManager.Instance.SendGiftAsync(Avatar.Id, toAvatarId, giftType, message, metadata);
        }

        /// <summary>
        /// Receive a gift
        /// </summary>
        /// <param name="giftId">Gift ID</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("receive-gift/{giftId}")]
        public async Task<OASISResult<bool>> ReceiveGift(Guid giftId)
        {
            return await GiftsManager.Instance.ReceiveGiftAsync(Avatar.Id, giftId);
        }

        /// <summary>
        /// Open a received gift
        /// </summary>
        /// <param name="giftId">Gift ID</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("open-gift/{giftId}")]
        public async Task<OASISResult<bool>> OpenGift(Guid giftId)
        {
            return await GiftsManager.Instance.OpenGiftAsync(Avatar.Id, giftId);
        }

        /// <summary>
        /// Get gift history for the current avatar
        /// </summary>
        /// <param name="limit">Number of transactions to return</param>
        /// <param name="offset">Number of transactions to skip</param>
        /// <returns>Gift transaction history</returns>
        [Authorize]
        [HttpGet("history")]
        public async Task<OASISResult<List<GiftTransaction>>> GetGiftHistory([FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            try
            {
                OASISResult<List<GiftTransaction>> result = null;
                try
                {
                    result = await GiftsManager.Instance.GetGiftHistoryAsync(Avatar.Id, limit, offset);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<GiftTransaction>>
                    {
                        Result = new List<GiftTransaction>(),
                        IsError = false,
                        Message = "Gift history retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<GiftTransaction>>
                    {
                        Result = new List<GiftTransaction>(),
                        IsError = false,
                        Message = "Gift history retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<GiftTransaction>>
                {
                    IsError = true,
                    Message = $"Error retrieving gift history: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get gift statistics for the current avatar
        /// </summary>
        /// <returns>Gift statistics</returns>
        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetGiftStats()
        {
            try
            {
                OASISResult<Dictionary<string, object>> result = null;
                try
                {
                    result = await GiftsManager.Instance.GetGiftStatsAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Gift stats retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Gift stats retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving gift stats: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}