using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Invoice model - represents QuickBooks invoice tracking.
    /// Full implementation will be completed in Task 1.2 (Schema Design) and Task 1.3 (Repository).
    /// </summary>
    public class Invoice
    {
        public Guid InvoiceId { get; set; }
        public Guid ShipmentId { get; set; }
        public Guid MerchantId { get; set; }
        public string QuickBooksInvoiceId { get; set; }
        public string QuickBooksCustomerId { get; set; }
        public decimal Amount { get; set; }
        public List<InvoiceLineItem> LineItems { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InvoiceLineItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid
    }
}

