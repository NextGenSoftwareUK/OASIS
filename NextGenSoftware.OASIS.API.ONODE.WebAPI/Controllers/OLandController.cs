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
            return await OLandManager.LoadAllOlandsAsync();
        }

        [Authorize]
        [HttpGet]
        [Route("load-oland/{olandId}")]
        public async Task<OASISResult<IOLand>> LoadOlandAsync(Guid olandId)
        {
            return await OLandManager.LoadOlandAsync(olandId);
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