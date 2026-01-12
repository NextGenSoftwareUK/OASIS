using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Programmable invoice management endpoints
    /// Supports automated invoicing and MNEE stablecoin payments
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : OASISControllerBase
    {
        private InvoiceManager _invoiceManager;

        public InvoiceManager InvoiceManager
        {
            get
            {
                if (_invoiceManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));
                    _invoiceManager = new InvoiceManager(result.Result);
                }
                return _invoiceManager;
            }
        }

        /// <summary>
        /// Create a new invoice
        /// </summary>
        [Authorize]
        [HttpPost("create")]
        public async Task<OASISResult<IInvoice>> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            // Use authenticated avatar as sender if not specified
            if (request.FromAvatarId == Guid.Empty)
                request.FromAvatarId = AvatarId;

            return await InvoiceManager.CreateInvoiceAsync(request);
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        [Authorize]
        [HttpGet("{invoiceId}")]
        public async Task<OASISResult<IInvoice>> GetInvoice(Guid invoiceId)
        {
            return await InvoiceManager.GetInvoiceAsync(invoiceId);
        }

        /// <summary>
        /// Get all invoices for an avatar
        /// </summary>
        [Authorize]
        [HttpGet("avatar/{avatarId?}")]
        public async Task<OASISResult<List<IInvoice>>> GetInvoicesForAvatar(Guid? avatarId, [FromQuery] InvoiceStatus? status = null)
        {
            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                var result = new OASISResult<List<IInvoice>>();
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            return await InvoiceManager.GetInvoicesForAvatarAsync(targetAvatarId, status);
        }

        /// <summary>
        /// Pay an invoice using MNEE
        /// </summary>
        [Authorize]
        [HttpPost("{invoiceId}/pay")]
        public async Task<OASISResult<string>> PayInvoice(Guid invoiceId, [FromQuery] bool autoApprove = false)
        {
            return await InvoiceManager.PayInvoiceAsync(invoiceId, AvatarId, autoApprove);
        }

        /// <summary>
        /// Cancel an invoice
        /// </summary>
        [Authorize]
        [HttpPost("{invoiceId}/cancel")]
        public async Task<OASISResult<bool>> CancelInvoice(Guid invoiceId)
        {
            return await InvoiceManager.CancelInvoiceAsync(invoiceId, AvatarId);
        }
    }
}
