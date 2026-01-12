using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages programmable invoices with MNEE stablecoin payments
    /// Supports automated invoicing, payment tracking, and settlement
    /// </summary>
    public class InvoiceManager : OASISManager
    {
        private static InvoiceManager _instance;
        private readonly WalletManager _walletManager;
        private readonly KeyManager _keyManager;

        public static InvoiceManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new InvoiceManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public InvoiceManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _walletManager = new WalletManager(OASISStorageProvider, OASISDNA);
            _keyManager = new KeyManager(OASISStorageProvider, OASISDNA);
        }

        /// <summary>
        /// Create a new invoice
        /// </summary>
        public async Task<OASISResult<IInvoice>> CreateInvoiceAsync(CreateInvoiceRequest request)
        {
            var result = new OASISResult<IInvoice>();
            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Invoice request is required");
                    return result;
                }

                if (request.FromAvatarId == Guid.Empty || request.ToAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "From and To avatar IDs are required");
                    return result;
                }

                if (request.Amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Invoice amount must be greater than zero");
                    return result;
                }

                var invoice = new Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    FromAvatarId = request.FromAvatarId,
                    ToAvatarId = request.ToAvatarId,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "MNEE",
                    Description = request.Description,
                    DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30),
                    Status = InvoiceStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    MetaData = request.Metadata ?? new Dictionary<string, object>()
                };

                // Save invoice as Holon
                var holon = invoice.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error saving invoice: {saveResult.Message}");
                    return result;
                }

                result.Result = invoice;
                result.IsError = false;
                result.Message = "Invoice created successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating invoice: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Pay an invoice using MNEE
        /// </summary>
        public async Task<OASISResult<string>> PayInvoiceAsync(Guid invoiceId, Guid payerAvatarId, bool autoApprove = false)
        {
            var result = new OASISResult<string>();
            try
            {
                // Load invoice
                var invoiceResult = await GetInvoiceAsync(invoiceId);
                if (invoiceResult.IsError || invoiceResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Invoice not found: {invoiceResult.Message}");
                    return result;
                }

                var invoice = invoiceResult.Result;

                // Validate payer
                if (invoice.ToAvatarId != payerAvatarId)
                {
                    OASISErrorHandling.HandleError(ref result, "Payer does not match invoice recipient");
                    return result;
                }

                // Check invoice status
                if (invoice.Status != InvoiceStatus.Pending)
                {
                    OASISErrorHandling.HandleError(ref result, $"Invoice is not pending. Current status: {invoice.Status}");
                    return result;
                }

                // Check if due date has passed
                if (invoice.DueDate < DateTime.UtcNow)
                {
                    invoice.Status = InvoiceStatus.Overdue;
                    await UpdateInvoiceAsync(invoice);
                }

                // Get Ethereum provider using reflection
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call TransferMNEEBetweenAvatarsAsync
                var method = providerResult.GetType().GetMethod("TransferMNEEBetweenAvatarsAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE transfer method not found on Ethereum provider");
                    return result;
                }

                var transferTask = (Task<OASISResult<string>>)method.Invoke(providerResult, new object[] { payerAvatarId, invoice.FromAvatarId, invoice.Amount });
                var transferResult = await transferTask;

                if (transferResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"MNEE transfer failed: {transferResult.Message}");
                    return result;
                }

                // Update invoice status
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;
                invoice.TransactionHash = transferResult.Result;
                if (invoice is Invoice invoiceConcrete)
                {
                    invoiceConcrete.MetaData["paymentTransactionHash"] = transferResult.Result;
                    invoiceConcrete.MetaData["paidBy"] = payerAvatarId.ToString();
                }

                await UpdateInvoiceAsync(invoice);

                result.Result = transferResult.Result;
                result.IsError = false;
                result.Message = "Invoice paid successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error paying invoice: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        public async Task<OASISResult<IInvoice>> GetInvoiceAsync(Guid invoiceId)
        {
            var result = new OASISResult<IInvoice>();
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(invoiceId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Invoice not found");
                    return result;
                }

                var invoice = Invoice.FromHolon(holonResult.Result);
                result.Result = invoice;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading invoice: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all invoices for an avatar
        /// </summary>
        public async Task<OASISResult<List<IInvoice>>> GetInvoicesForAvatarAsync(Guid avatarId, InvoiceStatus? status = null)
        {
            var result = new OASISResult<List<IInvoice>>();
            try
            {
                // Search for invoices using metadata filter
                var searchParams = new SearchParams
                {
                    AvatarId = avatarId,
                    FilterByMetaData = new Dictionary<string, string>
                    {
                        ["InvoiceType"] = "MNEEInvoice"
                    },
                    MetaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All,
                    SearchGroups = new List<ISearchGroupBase>
                    {
                        new SearchTextGroup
                        {
                            HolonType = HolonType.Holon,
                            SearchQuery = avatarId.ToString(),
                            SearchHolons = true,
                            HolonSearchParams = new SearchHolonParams
                            {
                                MetaData = true,
                                MetaDataKey = "FromAvatarId"
                            }
                        }
                    }
                };

                var searchResult = await SearchManager.Instance.SearchAsync(searchParams);
                if (searchResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error searching invoices: {searchResult.Message}");
                    return result;
                }

                var invoices = new List<IInvoice>();
                if (searchResult.Result != null && searchResult.Result.SearchResultHolons != null)
                {
                    foreach (var holon in searchResult.Result.SearchResultHolons)
                    {
                        // Check if this is an invoice by checking metadata
                        if (holon.MetaData != null && holon.MetaData.ContainsKey("InvoiceType") && holon.MetaData["InvoiceType"].ToString() == "MNEEInvoice")
                        {
                            var invoice = Invoice.FromHolon(holon);
                            // Check if avatar is sender or recipient
                            if ((invoice.FromAvatarId == avatarId || invoice.ToAvatarId == avatarId) && (status == null || invoice.Status == status))
                            {
                                invoices.Add(invoice);
                            }
                        }
                    }
                }

                result.Result = invoices;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting invoices: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update invoice
        /// </summary>
        private async Task<OASISResult<bool>> UpdateInvoiceAsync(IInvoice invoice)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (invoice is Invoice invoiceConcrete)
                {
                    invoiceConcrete.ModifiedDate = DateTime.UtcNow;
                    var holon = invoiceConcrete.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.Message;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Invoice is not of expected type");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating invoice: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Cancel an invoice
        /// </summary>
        public async Task<OASISResult<bool>> CancelInvoiceAsync(Guid invoiceId, Guid cancellerAvatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var invoiceResult = await GetInvoiceAsync(invoiceId);
                if (invoiceResult.IsError || invoiceResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Invoice not found");
                    return result;
                }

                var invoice = invoiceResult.Result;

                // Only invoice creator can cancel
                if (invoice.FromAvatarId != cancellerAvatarId)
                {
                    OASISErrorHandling.HandleError(ref result, "Only invoice creator can cancel the invoice");
                    return result;
                }

                if (invoice.Status == InvoiceStatus.Paid)
                {
                    OASISErrorHandling.HandleError(ref result, "Cannot cancel a paid invoice");
                    return result;
                }

                invoice.Status = InvoiceStatus.Cancelled;
                if (invoice is Invoice invoiceConcrete)
                {
                    invoiceConcrete.MetaData["cancelledBy"] = cancellerAvatarId.ToString();
                    invoiceConcrete.MetaData["cancelledDate"] = DateTime.UtcNow;
                }

                await UpdateInvoiceAsync(invoice);

                result.Result = true;
                result.IsError = false;
                result.Message = "Invoice cancelled successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error cancelling invoice: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// Invoice interface
    /// </summary>
    public interface IInvoice
    {
        Guid InvoiceId { get; set; }
        Guid FromAvatarId { get; set; }
        Guid ToAvatarId { get; set; }
        decimal Amount { get; set; }
        string Currency { get; set; }
        string Description { get; set; }
        DateTime DueDate { get; set; }
        InvoiceStatus Status { get; set; }
        DateTime? PaidDate { get; set; }
        string TransactionHash { get; set; }
    }

    /// <summary>
    /// Invoice implementation
    /// </summary>
    public class Invoice : HolonBase, IInvoice
    {
        public Invoice()
        {
            this.HolonType = HolonType.Holon;
            this.MetaData = new Dictionary<string, object> { ["InvoiceType"] = "MNEEInvoice" };
        }

        public Guid InvoiceId { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MNEE";
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public string TransactionHash { get; set; }

        public static Invoice FromHolon(IHolon holon)
        {
            return new Invoice
            {
                Id = holon.Id,
                InvoiceId = holon.Id,
                FromAvatarId = holon.MetaData.ContainsKey("FromAvatarId") ? Guid.Parse(holon.MetaData["FromAvatarId"].ToString()) : Guid.Empty,
                ToAvatarId = holon.MetaData.ContainsKey("ToAvatarId") ? Guid.Parse(holon.MetaData["ToAvatarId"].ToString()) : Guid.Empty,
                Amount = holon.MetaData.ContainsKey("Amount") ? Convert.ToDecimal(holon.MetaData["Amount"]) : 0,
                Currency = holon.MetaData.ContainsKey("Currency") ? holon.MetaData["Currency"].ToString() : "MNEE",
                Description = holon.Description,
                DueDate = holon.MetaData.ContainsKey("DueDate") ? DateTime.Parse(holon.MetaData["DueDate"].ToString()) : DateTime.UtcNow,
                Status = holon.MetaData.ContainsKey("Status") ? (InvoiceStatus)Enum.Parse(typeof(InvoiceStatus), holon.MetaData["Status"].ToString()) : InvoiceStatus.Pending,
                PaidDate = holon.MetaData.ContainsKey("PaidDate") ? DateTime.Parse(holon.MetaData["PaidDate"].ToString()) : (DateTime?)null,
                TransactionHash = holon.MetaData.ContainsKey("TransactionHash") ? holon.MetaData["TransactionHash"].ToString() : null,
                CreatedDate = holon.CreatedDate,
                ModifiedDate = holon.ModifiedDate,
                MetaData = holon.MetaData
            };
        }

        public IHolon ToHolon()
        {
            this.MetaData["FromAvatarId"] = FromAvatarId.ToString();
            this.MetaData["ToAvatarId"] = ToAvatarId.ToString();
            this.MetaData["Amount"] = Amount;
            this.MetaData["Currency"] = Currency;
            this.MetaData["DueDate"] = DueDate.ToString("O");
            this.MetaData["Status"] = Status.ToString();
            if (PaidDate.HasValue)
                this.MetaData["PaidDate"] = PaidDate.Value.ToString("O");
            if (!string.IsNullOrWhiteSpace(TransactionHash))
                this.MetaData["TransactionHash"] = TransactionHash;
            this.Description = Description;
            this.Name = $"Invoice {InvoiceId}";
            return (IHolon)this;
        }
    }

    /// <summary>
    /// Invoice status enum
    /// </summary>
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Overdue,
        Cancelled
    }

    /// <summary>
    /// Create invoice request
    /// </summary>
    public class CreateInvoiceRequest
    {
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MNEE";
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
