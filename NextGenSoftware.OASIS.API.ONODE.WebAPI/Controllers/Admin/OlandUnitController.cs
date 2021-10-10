using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class OlandUnitController : OASISControllerBase
    {
        [HttpPost]
        public async Task<OASISResult<bool>> Create(CreateOlandUnitRequest request)
        {
            
        }

        [HttpPut("{id:int}")]
        public async Task<OASISResult<bool>> Update(UpdateOlandUnitRequest request, int id)
        {
            
        }

        [HttpDelete("{id:int}")]
        public async Task<OASISResult<bool>> Delete(int id)
        {
            
        }

        [HttpGet("{id:int}")]
        public async Task<OASISResult<OlandUnitDto>> Get(int id)
        {
        }

        [HttpGet("GetAll")]
        public async Task<OASISResult<IEnumerable<OlandUnitDto>>> GetAll()
        {
            
        }
    }

    public sealed class UpdateOlandUnitRequest
    {
    }

    public class CreateOlandUnitRequest
    {
    }

    public sealed class OlandUnitDto
    {
    }
}