using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class PriceController : OASISControllerBase
    {
        [HttpPost]
        public async Task<OASISResult<bool>> Create()
        {
            
        }

        [HttpPut("{id:int}")]
        public async Task<OASISResult<bool>> Update(int id)
        {
            
        }

        [HttpDelete("{id:int}")]
        public async Task<OASISResult<bool>> Delete(int id)
        {
            
        }

        [HttpGet("{id:int}")]
        public async Task<OASISResult<OlandPriceDto>> Get(int id)
        {
        }

        [HttpGet("GetAll")]
        public async Task<OASISResult<IEnumerable<OlandPriceDto>>> GetAll()
        {
            
        }
    }

    public sealed class OlandPriceDto
    {
    }
}