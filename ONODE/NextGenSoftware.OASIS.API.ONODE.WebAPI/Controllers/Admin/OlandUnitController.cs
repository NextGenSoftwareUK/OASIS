using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class OLandUnitController : OASISControllerBase
    {
        private readonly IOlandService _olandService;

        public OLandUnitController(IOlandService olandService)
        {
            _olandService = olandService;
        }
        
        [HttpPost]
        public async Task<ActionResult<OASISResult<string>>>> Create(ManageOlandUnitRequestDto request)
        {
            if (request == null)
                return BadRequest(new OASISResult<string>(default) { IsError = true, Message = "Request body is required." });
            return Ok(await _olandService.CreateOland(request));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<OASISResult<string>>>> Update(ManageOlandUnitRequestDto request, Guid id)
        {
            if (request == null)
                return BadRequest(new OASISResult<string>(default) { IsError = true, Message = "Request body is required." });
            return Ok(await _olandService.UpdateOland(request, id));
        }

        [HttpDelete("{id:guid}")]
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            return await _olandService.DeleteOland(id);
        }

        [HttpGet("{id:guid}")]
        public async Task<OASISResult<IOLand>> Get(Guid id)
        {
            return await _olandService.GetOland(id);
        }

        [HttpGet("GetAll")]
        public async Task<OASISResult<IEnumerable<IOLand>>> GetAll()
        {
            return await _olandService.GetAllOlands();
        }
    }
}