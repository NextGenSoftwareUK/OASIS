using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONode.Core.Managers;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OLandController : OASISControllerBase
    {
        public OLandController()
        {

        }

        [Authorize]
        [HttpGet]
        [Route("get-oland-price")]
        public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
        {
            return await OLandManager.Instance.GetOlandPriceAsync(count, couponCode);
        }

        [Authorize]
        [HttpPost]
        [Route("purchase-oland")]
        public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request)
        {
            return await OLandManager.Instance.PurchaseOlandAsync(request);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-olands")]
        public async Task<OASISResult<IEnumerable<IOLand>>> LoadAllOlands()
        {
            return await OLandManager.Instance.LoadAllOlandsAsync();
        }

        [Authorize]
        [HttpGet]
        [Route("load-oland/{olandId}")]
        public async Task<OASISResult<IOLand>> LoadOlandAsync(Guid olandId)
        {
            return await OLandManager.Instance.LoadOlandAsync(olandId);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("delete-oland/{olandId}")]
        public async Task<OASISResult<bool>> DeleteOlandAsync(Guid olandId)
        {
            return await OLandManager.Instance.DeleteOlandAsync(olandId);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("save-oland")]
        public async Task<OASISResult<string>> SaveOlandAsync(IOLand request)
        {
            return await OLandManager.Instance.SaveOlandAsync(request);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        [Route("update-oland")]
        public async Task<OASISResult<string>> UpdateOlandAsync(IOLand request)
        {
            return await OLandManager.Instance.UpdateOlandAsync(request);
        }
    }
}