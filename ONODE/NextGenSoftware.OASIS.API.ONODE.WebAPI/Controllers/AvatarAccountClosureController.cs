using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

public sealed record AccountClosureAssessment(bool SubscriptionRequiresCancellation,
    bool HasOpenStripeInvoices, bool HasPendingStripeCheckouts, bool HasOwnedApplications, bool HasUnsettledUsage,
    bool HasUnbilledCharges, bool HasUnresolvedStatements, DateTime AssessedAtUtc);

[ApiController]
[Route("api/avatar/account-closure")]
public sealed class AvatarAccountClosureController : OASISControllerBase
{
    private readonly AccountClosurePrerequisiteReader _reader;
    private readonly ILogger<AvatarAccountClosureController> _logger;
    public AvatarAccountClosureController(MongoSubscriptionUsageRepository repository, IConfiguration configuration,
        ILogger<AvatarAccountClosureController> logger)
    {
        _reader = new AccountClosurePrerequisiteReader(repository, configuration);
        _logger = logger;
    }

    /// <summary>Read the authenticated account's billing and ownership dependencies. This does not authorize or perform deletion.</summary>
    [Authorize]
    [HttpGet("assessment")]
    public async Task<OASISResult<AccountClosureAssessment>> Assess()
    {
        if (AvatarId == Guid.Empty)
            return new OASISResult<AccountClosureAssessment> { IsError = true, ErrorCode = "AUTHENTICATION_REQUIRED",
                Message = "Beam in to review your account dependencies." };
        try
        {
            var assessment = await _reader.ReadAsync(AvatarId, HttpContext.RequestAborted);
            return new OASISResult<AccountClosureAssessment> { IsLoaded = true,
                Result = new AccountClosureAssessment(assessment.SubscriptionRequiresCancellation,
                    assessment.HasOpenStripeInvoices, assessment.HasPendingStripeCheckouts, assessment.HasOwnedApplications, assessment.HasUnsettledUsage,
                    assessment.HasUnbilledCharges, assessment.HasUnresolvedStatements, DateTime.UtcNow),
                Message = "Current billing and ownership dependencies loaded. No account or billing data were changed." };
        }
        catch (Exception cause) when (cause is not OperationCanceledException)
        {
            _logger.LogWarning("Account dependency assessment failed ({FailureType}); no mutation attempted.", cause.GetType().Name);
            return new OASISResult<AccountClosureAssessment> { IsError = true, ErrorCount = 1,
                ErrorCode = "ACCOUNT_CLOSURE_ASSESSMENT_FAILED",
                Message = "Your account dependencies could not be verified. No account or billing changes were made." };
        }
    }
}
