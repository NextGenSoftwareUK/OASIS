using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service interface for webhook processing.
    /// This will be implemented by Agent D.
    /// </summary>
    public interface IWebhookService
    {
        /// <summary>
        /// Process a webhook event from an external system (iShip, Shipox, etc.).
        /// </summary>
        /// <param name="webhook">Webhook event data</param>
        /// <returns>True if processing was successful</returns>
        Task<OASISResult<bool>> ProcessWebhookAsync(WebhookEvent webhook);

        /// <summary>
        /// Retry processing a failed webhook event.
        /// </summary>
        /// <param name="webhookEventId">Webhook event identifier</param>
        /// <returns>True if retry was successful</returns>
        Task<OASISResult<bool>> RetryFailedWebhookAsync(Guid webhookEventId);

        /// <summary>
        /// Verify webhook signature for security.
        /// </summary>
        /// <param name="webhook">Webhook event data</param>
        /// <param name="signature">Expected signature</param>
        /// <param name="secret">Secret key for verification</param>
        /// <returns>True if signature is valid</returns>
        Task<OASISResult<bool>> VerifyWebhookSignatureAsync(WebhookEvent webhook, string signature, string secret);
    }
}
