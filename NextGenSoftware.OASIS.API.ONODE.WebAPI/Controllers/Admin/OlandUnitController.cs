using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class OlandUnitController : OASISControllerBase
    {
        private readonly IOlandService _olandService;
        public OlandUnitController(IOlandService olandService)
        {
            _olandService = olandService;
        }
        
        [HttpPost]
        public async Task<OASISResult<string>> Create(ManageOlandUnitRequestDto request)
        {
            return await _olandService.CreateOland(request);
        }

        [HttpPut("{id:guid}")]
        public async Task<OASISResult<string>> Update(ManageOlandUnitRequestDto request, Guid id)
        {
            return await _olandService.UpdateOland(request, id);
        }

        [HttpDelete("{id:guid}")]
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            return await _olandService.DeleteOland(id);
        }

        [HttpGet("{id:guid}")]
        public async Task<OASISResult<IOland>> Get(Guid id)
        {
            return await _olandService.GetOland(id);
        }

        [HttpGet("GetAll")]
        public async Task<OASISResult<IEnumerable<IOland>>> GetAll()
        {
            return await _olandService.GetAllOlands();
        }
    }
}