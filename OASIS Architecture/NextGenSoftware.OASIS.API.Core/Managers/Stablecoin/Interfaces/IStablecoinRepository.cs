using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Interfaces
{
    /// <summary>
    /// Repository interface for stablecoin position storage
    /// </summary>
    public interface IStablecoinRepository
    {
        Task<OASISResult<StablecoinPosition>> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default);
        Task<OASISResult<List<StablecoinPosition>>> GetPositionsByAvatarAsync(Guid avatarId, CancellationToken cancellationToken = default);
        Task<OASISResult<List<StablecoinPosition>>> GetAllPositionsAsync(CancellationToken cancellationToken = default);
        Task<OASISResult<bool>> SavePositionAsync(StablecoinPosition position, CancellationToken cancellationToken = default);
        Task<OASISResult<bool>> DeletePositionAsync(Guid positionId, CancellationToken cancellationToken = default);
    }
}

