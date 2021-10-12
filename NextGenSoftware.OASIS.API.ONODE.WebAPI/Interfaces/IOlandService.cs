using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Admin;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IOlandService
    {
        Task<OASISResult<IEnumerable<OlandUnitDto>>> GetAllOlands();
        Task<OASISResult<OlandUnitDto>> GetOland(int id);
        Task<OASISResult<bool>> DeleteOland(int id);
        Task<OASISResult<int>> CreateOland(CreateOlandUnitRequest request);
        Task<OASISResult<bool>> UpdateOland(UpdateOlandUnitRequest request, int id);
    }
}