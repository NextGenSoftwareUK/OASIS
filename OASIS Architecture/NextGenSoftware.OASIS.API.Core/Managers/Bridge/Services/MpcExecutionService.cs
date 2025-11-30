using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services
{
    public class MpcExecutionService
    {
        public Task<OASISResult<string>> StartMpcSessionAsync(string fromChain, string toChain, decimal amount, CancellationToken token = default)
        {
            var sessionId = $"mpc_{Guid.NewGuid():N}";
            return Task.FromResult(new OASISResult<string>(sessionId)
            {
                Message = $"MPC session scheduled for {fromChain}->{toChain} amount {amount}"
            });
        }
    }
}

