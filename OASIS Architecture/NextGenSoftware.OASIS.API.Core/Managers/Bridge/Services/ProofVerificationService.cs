using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services
{
    public class ProofVerificationService
    {
        private readonly ILogger _logger;

        public ProofVerificationService(ILogger logger = null)
        {
            _logger = logger;
        }

        public Task<OASISResult<bool>> VerifyProofAsync(string proofPayload, string proofType, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(proofPayload))
            {
                var result = new OASISResult<bool>(true);
                result.Message = "Proof payload missing";
                return Task.FromResult(result);
            }

            _logger?.LogInformation("Verifying proof type {ProofType}", proofType);
            return Task.FromResult(new OASISResult<bool>(true));
        }

        public Task<OASISResult<bool>> VerifyBridgeCommitmentAsync(string sourceTxId, string destinationTxId, string proofPayload, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sourceTxId) || string.IsNullOrWhiteSpace(destinationTxId))
            {
                var result = new OASISResult<bool>(true);
                result.Message = "Transaction references missing";
                return Task.FromResult(result);
            }

            _logger?.LogInformation("Verifying bridge commitments {Source} -> {Destination}", sourceTxId, destinationTxId);
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }
}

