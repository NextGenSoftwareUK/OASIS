using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;
using NextGenSoftware.OASIS.Common;
using Stripe;
using Stripe.Checkout;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

public partial class OLandController
{
    [HttpGet("purchase-options")]
    public Task<IActionResult> PurchaseOptions([FromServices] OLandCommerceService commerce) => Commerce(() => Task.FromResult(commerce.Options()));

    [HttpGet("parcel-grid")]
    public Task<IActionResult> ParcelGrid(double latitude, double longitude, [FromServices] OLandCommerceService commerce, int width = 16) =>
        Commerce(() => commerce.Grid(latitude, longitude, width, HttpContext.RequestAborted));

    [HttpPost("quotes")]
    public Task<IActionResult> Quote([FromBody] OLandQuoteRequest request, [FromServices] OLandCommerceService commerce) =>
        Commerce(() => commerce.Quote(AvatarId, Request.Headers["Idempotency-Key"].FirstOrDefault(), request, HttpContext.RequestAborted));

    [HttpPost("orders/{orderId}/checkout")]
    public Task<IActionResult> Checkout(string orderId, [FromServices] OLandCommerceService commerce) =>
        Commerce(() => commerce.BeginCheckout(AvatarId, orderId, HttpContext.RequestAborted));

    [HttpGet("orders")]
    public Task<IActionResult> OrderHistory([FromServices] OLandCommerceService commerce, string beforeOrderId = null, int limit = 50) =>
        Commerce(() => commerce.History(AvatarId, beforeOrderId, limit, HttpContext.RequestAborted));

    [HttpGet("orders/{orderId}")]
    public Task<IActionResult> OrderStatus(string orderId, [FromServices] OLandCommerceService commerce) =>
        Commerce(() => commerce.Status(AvatarId, orderId, HttpContext.RequestAborted));

    [HttpPost("orders/{orderId}/confirm-payment")]
    public Task<IActionResult> ConfirmPayment(string orderId, [FromBody] OLandPaymentRequest request, [FromServices] OLandCommerceService commerce) =>
        Commerce(() => commerce.ConfirmNative(AvatarId, orderId, request?.TransactionId, HttpContext.RequestAborted));

    [HttpPost("orders/{orderId}/fulfil")]
    public Task<IActionResult> Fulfil(string orderId, [FromServices] OLandCommerceService commerce) =>
        Commerce(() => commerce.Fulfil(AvatarId, orderId, HttpContext.RequestAborted));

    [HttpDelete("quotes/{orderId}")]
    public Task<IActionResult> CancelQuote(string orderId, [FromServices] OLandCommerceService commerce) => Commerce(async () =>
    { await commerce.CancelQuote(AvatarId, orderId, HttpContext.RequestAborted); return true; });

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("webhooks/stripe")]
    [RequestSizeLimit(262144)]
    public async Task<IActionResult> OLandStripeWebhook([FromServices] OLandCommerceService commerce, [FromServices] OLandCommerceSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.StripeWebhookSecret)) return StatusCode(503, new OASISResult<bool> { IsError = true, Message = "OLAND Stripe webhook is not configured." });
        string payload = await new StreamReader(Request.Body).ReadToEndAsync(HttpContext.RequestAborted);
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(payload, Request.Headers["Stripe-Signature"].FirstOrDefault(), settings.StripeWebhookSecret, throwOnApiVersionMismatch: true); }
        catch (Exception ex) when (ex is StripeException or ArgumentException or Newtonsoft.Json.JsonException or System.Text.Json.JsonException)
        { return BadRequest(new OASISResult<bool> { IsError = true, ErrorCode = "OLAND_WEBHOOK_INVALID", Message = "Invalid Stripe webhook signature or payload." }); }
        if (stripeEvent.Type is not ("checkout.session.completed" or "checkout.session.async_payment_succeeded" or "checkout.session.expired")) return Ok(new { Received = true });
        if (stripeEvent.Data.Object is not Session session || !session.Metadata.TryGetValue("oland_order_id", out var order)) return Ok(new { Received = true });
        return await Commerce(() => commerce.StripeNotification(order, session.Id, HttpContext.RequestAborted));
    }
    private async Task<IActionResult> Commerce<T>(Func<Task<T>> operation)
    {
        try { return Ok(new OASISResult<T> { Result = await operation() }); }
        catch (OLandCommerceException ex)
        {
            int status = ex.Code == "OLAND_ORDER_NOT_FOUND" ? 404 : ex.Code == "OLAND_CONFLICT" ? 409 :
                ex.Code.Contains("UNAVAILABLE") || ex.Code.Contains("UNVERIFIED") || ex.Code.Contains("CONFIGURATION") ? 503 : 400;
            return StatusCode(status, new OASISResult<T> { IsError = true, ErrorCode = ex.Code, Message = ex.Message });
        }
        catch (ArgumentException ex) { return BadRequest(new OASISResult<T> { IsError = true, ErrorCode = "OLAND_REQUEST_INVALID", Message = ex.Message }); }
    }
}
