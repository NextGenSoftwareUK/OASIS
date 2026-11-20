using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IConditionalHolonReceiptEvidenceStorageProvider
{
    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonPropertyWriteOutcomeAsync(
        ConditionalHolonPropertyWriteRequest original, CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return WithHolonRepositoryAsync(repository => repository.ReadConditionalHolonPropertyWriteOutcomeAsync(original, cancellationToken));
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return WithHolonRepositoryAsync(repository => repository.ReadConditionalHolonWriteOutcomeAsync(original, cancellationToken));
    }
    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadBoundConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity expectedSource,
        CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return WithHolonRepositoryAsync(repository => repository.ReadBoundConditionalHolonWriteOutcomeAsync(original, expectedSource, cancellationToken));
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonReceiptEvidenceAsync(
        ConditionalHolonReceiptEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_STATUS_UNVERIFIED", Message = "SQLite provider is not activated." });
        return WithHolonRepositoryAsync(repository => repository.ReadConditionalHolonReceiptEvidenceAsync(request, cancellationToken));
    }

}
