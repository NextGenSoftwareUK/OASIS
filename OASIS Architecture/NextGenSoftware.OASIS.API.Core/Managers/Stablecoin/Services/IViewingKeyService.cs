using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Service for generating viewing keys for private position tracking
    /// </summary>
    public interface IViewingKeyService
    {
        Task<OASISResult<string>> GenerateViewingKeyAsync(Guid avatarId, CancellationToken cancellationToken = default);
    }
}

