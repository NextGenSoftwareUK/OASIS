using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IConditionalHolonReceiptEvidenceStorageProvider
{
    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return _holonRepository.ReadConditionalHolonWriteOutcomeAsync(original, cancellationToken);
    }
    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadBoundConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity expectedSource,
        CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return _holonRepository.ReadBoundConditionalHolonWriteOutcomeAsync(original, expectedSource, cancellationToken);
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonReceiptEvidenceAsync(
        ConditionalHolonReceiptEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return _holonRepository.ReadConditionalHolonReceiptEvidenceAsync(request, cancellationToken);
    }

}
