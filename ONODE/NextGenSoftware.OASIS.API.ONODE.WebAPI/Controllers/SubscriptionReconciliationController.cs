using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[Route("api/subscription/usage/admin/reconciliation")]
public sealed class SubscriptionReconciliationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly MongoUsageReconciler _reconciler;
    private readonly StripeUsageEvidenceReader _stripe;
    private readonly ISubscriptionService _subscriptions;
    public SubscriptionReconciliationController(IConfiguration configuration, MongoUsageReconciler reconciler,
        StripeUsageEvidenceReader stripe, ISubscriptionService subscriptions)
    { _configuration = configuration; _reconciler = reconciler; _stripe = stripe; _subscriptions = subscriptions; }

    [HttpPost("provider-receipts")]
    public async Task<IActionResult> ProviderReceipt([FromBody] UsageExternalReceipt receipt)
    {
        try
        {
            string actor = SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
            if (receipt == null || receipt.Source != "provider") throw new ArgumentException("Only independently obtained provider evidence is accepted here.");
            receipt.Actor = actor;
            return Ok(new OASISResult<UsageExternalReceipt> { Result = await _reconciler.RecordReceiptAsync(receipt, HttpContext.RequestAborted) });
        }
        catch (UnauthorizedAccessException) { return StatusCode(403, Error("Administrator access required.")); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpPost("stripe-invoices")]
    public async Task<IActionResult> StripeInvoice([FromBody] StripeInvoiceEvidenceRequest request)
    {
        try
        {
            string actor = SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
            var subscription = await _subscriptions.GetSubscriptionAsync(request.UserId)
                ?? throw new ArgumentException("Subscription was not found.");
            return Ok(new OASISResult<UsageExternalReceipt> { Result = await _stripe.ImportAsync(request.InvoiceId,
                subscription.StripeCustomerId, request.UserId, request.Month, actor, HttpContext.RequestAborted) });
        }
        catch (UnauthorizedAccessException) { return StatusCode(403, Error("Administrator access required.")); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] ReconcileAccountRequest request)
    {
        try
        {
            SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
            var subscription = await _subscriptions.GetSubscriptionAsync(request.UserId)
                ?? throw new ArgumentException("Subscription was not found.");
            // Refresh immutable invoice evidence before making this point-in-time claim.
            // A now-void or otherwise invalid invoice must fail the check visibly.
            await _stripe.CollectAsync(subscription.StripeCustomerId, request.UserId, request.Month, HttpContext.RequestAborted);
            return Ok(new OASISResult<UsageReconciliationReport> { Result = await _reconciler.ReconcileAsync(request.UserId, request.Month, HttpContext.RequestAborted) });
        }
        catch (UnauthorizedAccessException) { return StatusCode(403, Error("Administrator access required.")); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] string userId, [FromQuery] string month)
    {
        try
        {
            SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
            return Ok(new OASISResult<IReadOnlyList<UsageReconciliationReport>>
            { Result = await _reconciler.GetReportsAsync(userId, month, HttpContext.RequestAborted) });
        }
        catch (UnauthorizedAccessException) { return StatusCode(403, Error("Administrator access required.")); }
    }

    private static OASISResult<bool> Error(string message) => new() { IsError = true, Message = message };
    public sealed class ReconcileAccountRequest { public string UserId { get; set; } public string Month { get; set; } }
    public sealed class StripeInvoiceEvidenceRequest { public string UserId { get; set; } public string Month { get; set; } public string InvoiceId { get; set; } }
}
