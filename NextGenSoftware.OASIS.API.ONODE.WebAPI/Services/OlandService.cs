using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class OlandService : IOlandService
    {
        public async Task<OASISResult<IEnumerable<OlandUnitDto>>> GetAllOlands()
        {
            throw new System.NotImplementedException();
        }

        public async Task<OASISResult<OlandUnitDto>> GetOland(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<OASISResult<bool>> DeleteOland(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<OASISResult<int>> CreateOland(ManageOlandUnitRequestDto request)
        {
            throw new System.NotImplementedException();
        }

        public async Task<OASISResult<bool>> UpdateOland(ManageOlandUnitRequestDto request, int id)
        {
            throw new System.NotImplementedException();
        }
    }
}