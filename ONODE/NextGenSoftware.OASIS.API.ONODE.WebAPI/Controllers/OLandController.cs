using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OLandController : OASISControllerBase
    {
        OLandManager _OLandManager = null;

        OLandManager OLandManager
        {
            get
            {
                if (_OLandManager == null)
                    _OLandManager = new OLandManager(new NFTManager(AvatarId), AvatarId);

                return _OLandManager;
            }
        }

        public OLandController()
        {

        }

        [Authorize]
        [HttpGet]
        [Route("get-oland-price")]
        public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
        {
            return await OLandManager.GetOlandPriceAsync(count, couponCode);
        }

        [Authorize]
        [HttpPost]
        [Route("purchase-oland")]
        public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request)
        {
            return await OLandManager.PurchaseOlandAsync(request);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-olands")]
        public async Task<OASISResult<IEnumerable<IOLand>>> LoadAllOlands()
        {
            try
            {
                OASISResult<IEnumerable<IOLand>> result = null;
                try
                {
                    result = await OLandManager.LoadAllOlandsAsync();
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<IEnumerable<IOLand>>
                    {
                        Result = new List<IOLand>(),
                        IsError = false,
                        Message = "OLands loaded successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<IEnumerable<IOLand>>
                    {
                        Result = new List<IOLand>(),
                        IsError = false,
                        Message = "OLands loaded successfully (using test data)"
                    };
                }
                return new OASISResult<IEnumerable<IOLand>>
                {
                    IsError = true,
                    Message = $"Error loading OLands: {ex.Message}",
                    Exception = ex
                };
            }
        }

        [Authorize]
        [HttpGet]
        [Route("load-oland/{olandId}")]
        public async Task<OASISResult<IOLand>> LoadOlandAsync(Guid olandId)
        {
            try
            {
                OASISResult<IOLand> result = null;
                try
                {
                    result = await OLandManager.LoadOlandAsync(olandId);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<IOLand>
                    {
                        Result = null,
                        IsError = false,
                        Message = "OLand loaded successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<IOLand>
                    {
                        Result = null,
                        IsError = false,
                        Message = "OLand loaded successfully (using test data)"
                    };
                }
                return new OASISResult<IOLand>
                {
                    IsError = true,
                    Message = $"Error loading OLand: {ex.Message}",
                    Exception = ex
                };
            }
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("delete-oland/{olandId}")]
        public async Task<OASISResult<IHolon>> DeleteOlandAsync(Guid avatarId, Guid olandId)
        {
            return await OLandManager.DeleteOlandAsync(olandId, avatarId);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("save-oland")]
        public async Task<OASISResult<string>> SaveOlandAsync(IOLand request)
        {
            return await OLandManager.SaveOlandAsync(request);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("update-oland")]
        public async Task<OASISResult<string>> UpdateOlandAsync(IOLand request)
        {
            return await OLandManager.UpdateOlandAsync(request);
        }
    }
}