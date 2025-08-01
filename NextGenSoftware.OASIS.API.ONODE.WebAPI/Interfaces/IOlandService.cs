using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IOlandService
    {
        Task<OASISResult<IEnumerable<IOLand>>> GetAllOlands();
        Task<OASISResult<IOLand>> GetOland(Guid id);
        Task<OASISResult<bool>> DeleteOland(Guid id);
        Task<OASISResult<string>> CreateOland(ManageOlandUnitRequestDto request);
        Task<OASISResult<string>> UpdateOland(ManageOlandUnitRequestDto request, Guid id);
    }
}