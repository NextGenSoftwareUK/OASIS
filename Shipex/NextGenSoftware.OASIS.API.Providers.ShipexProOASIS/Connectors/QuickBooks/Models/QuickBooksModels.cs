using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models
{
    /// <summary>
    /// QuickBooks Customer model
    /// </summary>
    public class QuickBooksCustomer
    {
        public string Id { get; set; } = string.Empty;
        public string SyncToken { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public QuickBooksAddress? BillAddr { get; set; }
    }

    /// <summary>
    /// QuickBooks Address model
    /// </summary>
    public class QuickBooksAddress
    {
        public string Line1 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CountrySubDivisionCode { get; set; } = string.Empty; // State
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>
    /// QuickBooks Invoice model
    /// </summary>
    public class QuickBooksInvoice
    {
        public string Id { get; set; } = string.Empty;
        public string SyncToken { get; set; } = string.Empty;
        public QuickBooksReference? CustomerRef { get; set; }
        public List<QuickBooksLineItem> Line { get; set; } = new();
        public decimal TotalAmt { get; set; }
        public string DocNumber { get; set; } = string.Empty;
        public DateTime? TxnDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PrivateNote { get; set; } = string.Empty;
    }

    /// <summary>
    /// QuickBooks Line Item model
    /// </summary>
    public class QuickBooksLineItem
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public QuickBooksReference? ItemRef { get; set; }
        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    /// <summary>
    /// QuickBooks Reference model (for IDs)
    /// </summary>
    public class QuickBooksReference
    {
        public string value { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    /// <summary>
    /// QuickBooks API Response wrapper
    /// </summary>
    public class QuickBooksResponse<T>
    {
        public T QueryResponse { get; set; }
        public DateTime time { get; set; }
    }

    /// <summary>
    /// QuickBooks Query Response
    /// </summary>
    public class QuickBooksQueryResponse<T>
    {
        public List<T> maxResults { get; set; } = new();
        public int startPosition { get; set; }
    }
}

