using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service interface for QuickBooks integration (invoice creation and payment tracking).
    /// This will be implemented by Agent E.
    /// </summary>
    public interface IQuickBooksService
    {
        /// <summary>
        /// Create an invoice in QuickBooks for a completed shipment.
        /// </summary>
        /// <param name="shipment">Shipment to create invoice for</param>
        /// <returns>Created invoice</returns>
        Task<OASISResult<Invoice>> CreateInvoiceAsync(Shipment shipment);

        /// <summary>
        /// Get an invoice by its ID.
        /// </summary>
        /// <param name="invoiceId">Invoice identifier</param>
        /// <returns>Invoice details</returns>
        Task<OASISResult<Invoice>> GetInvoiceAsync(Guid invoiceId);

        /// <summary>
        /// Check payment status of an invoice in QuickBooks.
        /// </summary>
        /// <param name="invoiceId">Invoice identifier</param>
        /// <returns>True if invoice is paid</returns>
        Task<OASISResult<bool>> CheckPaymentStatusAsync(Guid invoiceId);

        /// <summary>
        /// Refresh OAuth2 access token for QuickBooks connection.
        /// </summary>
        /// <param name="merchantId">Merchant identifier</param>
        /// <returns>True if token refresh was successful</returns>
        Task<OASISResult<bool>> RefreshAccessTokenAsync(Guid merchantId);
    }
}




