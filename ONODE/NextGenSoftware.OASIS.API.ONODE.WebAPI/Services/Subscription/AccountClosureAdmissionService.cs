using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Billing stage of account closure. Callers must authenticate and authorize the account subject.
/// This service does not delete the avatar, private data or provider replicas.</summary>
public sealed class AccountClosureAdmissionService(MongoSubscriptionUsageRepository repository,
    IConfiguration configuration, ILogger<AccountClosureAdmissionService> logger)
{
    public async Task<OASISResult<bool>> CloseBillingAdmissionAsync(Guid avatarId, Guid operationId, CancellationToken token)
    {
        if (avatarId == Guid.Empty || operationId == Guid.Empty)
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "INVALID_ACCOUNT_CLOSURE",
                Message = "A valid account and closure operation are required." };
        try
        {
            var dependencies = await new AccountClosurePrerequisiteReader(repository, configuration).ReadAsync(avatarId, token);
            if (dependencies.SubscriptionRequiresCancellation || dependencies.HasOpenStripeInvoices ||
                dependencies.HasPendingStripeCheckouts || dependencies.HasOwnedApplications || dependencies.HasUnsettledUsage ||
                dependencies.HasUnbilledCharges || dependencies.HasUnresolvedStatements)
                throw new SubscriptionUsageConflictException("Resolve subscriptions, owned applications, outstanding usage, charges and checkouts before account closure.");
            var closed = await repository.CloseBillingAdmissionAsync(avatarId, operationId, dependencies.LedgerSnapshot, token);
            if (!closed) throw new InvalidOperationException("Billing admission closure was not acknowledged.");
            return new OASISResult<bool> { Result = closed, IsSaved = closed,
                Message = "Billing admission is closed. Avatar and provider data deletion is not yet complete." };
        }
        catch (SubscriptionUsageConflictException conflict)
        {
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "ACCOUNT_CLOSURE_BLOCKED", Message = conflict.Message };
        }
        catch (Exception cause) when (cause is not OperationCanceledException)
        {
            logger.LogWarning("Billing admission closure is unconfirmed ({FailureType}).", cause.GetType().Name);
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "ACCOUNT_CLOSURE_UNCONFIRMED",
                Message = "Billing admission closure could not be confirmed. Avatar and provider data deletion has not been performed." };
        }
    }
}
