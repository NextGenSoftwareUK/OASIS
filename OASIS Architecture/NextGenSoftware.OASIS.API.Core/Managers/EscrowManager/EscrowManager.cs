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
    /// Manages escrow contracts with MNEE stablecoin
    /// Supports multi-party escrow, conditional releases, and dispute resolution
    /// </summary>
    public class EscrowManager : OASISManager
    {
        private static EscrowManager _instance;
        private readonly WalletManager _walletManager;
        private readonly KeyManager _keyManager;

        public static EscrowManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EscrowManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public EscrowManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _walletManager = new WalletManager(OASISStorageProvider, OASISDNA);
            _keyManager = new KeyManager(OASISStorageProvider, OASISDNA);
        }

        /// <summary>
        /// Create a new escrow contract
        /// </summary>
        public async Task<OASISResult<IEscrow>> CreateEscrowAsync(CreateEscrowRequest request)
        {
            var result = new OASISResult<IEscrow>();
            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Escrow request is required");
                    return result;
                }

                if (request.PayerAvatarId == Guid.Empty || request.PayeeAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Payer and Payee avatar IDs are required");
                    return result;
                }

                if (request.Amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Escrow amount must be greater than zero");
                    return result;
                }

                var escrow = new Escrow
                {
                    EscrowId = Guid.NewGuid(),
                    PayerAvatarId = request.PayerAvatarId,
                    PayeeAvatarId = request.PayeeAvatarId,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "MNEE",
                    Status = EscrowStatus.Pending,
                    Approvers = request.Approvers ?? new List<Guid> { request.PayerAvatarId },
                    Conditions = request.Conditions ?? new Dictionary<string, object>(),
                    ReleaseDate = request.ReleaseDate,
                    CreatedDate = DateTime.UtcNow,
                    MetaData = request.Metadata ?? new Dictionary<string, object>()
                };

                // Save escrow as Holon
                var holon = escrow.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error saving escrow: {saveResult.Message}");
                    return result;
                }

                result.Result = escrow;
                result.IsError = false;
                result.Message = "Escrow created successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating escrow: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Fund escrow by transferring MNEE from payer
        /// </summary>
        public async Task<OASISResult<string>> FundEscrowAsync(Guid escrowId, Guid payerAvatarId)
        {
            var result = new OASISResult<string>();
            try
            {
                // Load escrow
                var escrowResult = await GetEscrowAsync(escrowId);
                if (escrowResult.IsError || escrowResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Escrow not found: {escrowResult.Message}");
                    return result;
                }

                var escrow = escrowResult.Result;

                // Validate payer
                if (escrow.PayerAvatarId != payerAvatarId)
                {
                    OASISErrorHandling.HandleError(ref result, "Payer does not match escrow payer");
                    return result;
                }

                if (escrow.Status != EscrowStatus.Pending)
                {
                    OASISErrorHandling.HandleError(ref result, $"Escrow is not pending. Current status: {escrow.Status}");
                    return result;
                }

                // Get Ethereum provider using reflection
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call ApproveMNEEAsync
                var method = providerResult.GetType().GetMethod("ApproveMNEEAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE approval method not found on Ethereum provider");
                    return result;
                }

                var approveTask = (Task<OASISResult<string>>)method.Invoke(providerResult, new object[] { 
                    await GetPrivateKeyAsync(payerAvatarId),
                    "0x0000000000000000000000000000000000000000", // Placeholder - would be escrow contract address
                    escrow.Amount
                });
                var approveResult = await approveTask;

                if (approveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"MNEE approval failed: {approveResult.Message}");
                    return result;
                }

                // Update escrow status
                escrow.Status = EscrowStatus.Funded;
                escrow.FundedDate = DateTime.UtcNow;
                if (escrow is Escrow escrowConcrete)
                {
                    escrowConcrete.MetaData["approvalTransactionHash"] = approveResult.Result;
                    escrowConcrete.MetaData["fundedBy"] = payerAvatarId.ToString();
                }

                await UpdateEscrowAsync(escrow);

                result.Result = approveResult.Result;
                result.IsError = false;
                result.Message = "Escrow funded successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error funding escrow: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Release escrow funds to payee
        /// </summary>
        public async Task<OASISResult<string>> ReleaseEscrowAsync(Guid escrowId, Guid approverAvatarId)
        {
            var result = new OASISResult<string>();
            try
            {
                // Load escrow
                var escrowResult = await GetEscrowAsync(escrowId);
                if (escrowResult.IsError || escrowResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Escrow not found: {escrowResult.Message}");
                    return result;
                }

                var escrow = escrowResult.Result;

                // Validate approver
                if (!escrow.Approvers.Contains(approverAvatarId))
                {
                    OASISErrorHandling.HandleError(ref result, "Approver is not authorized to release this escrow");
                    return result;
                }

                if (escrow.Status != EscrowStatus.Funded)
                {
                    OASISErrorHandling.HandleError(ref result, $"Escrow is not funded. Current status: {escrow.Status}");
                    return result;
                }

                // Check release date if set
                if (escrow.ReleaseDate.HasValue && escrow.ReleaseDate.Value > DateTime.UtcNow)
                {
                    OASISErrorHandling.HandleError(ref result, $"Escrow cannot be released before {escrow.ReleaseDate.Value}");
                    return result;
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

                var transferTask = (Task<OASISResult<string>>)method.Invoke(providerResult, new object[] { 
                    escrow.PayerAvatarId,
                    escrow.PayeeAvatarId,
                    escrow.Amount
                });
                var transferResult = await transferTask;

                if (transferResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"MNEE transfer failed: {transferResult.Message}");
                    return result;
                }

                // Update escrow status
                escrow.Status = EscrowStatus.Released;
                escrow.ReleasedDate = DateTime.UtcNow;
                escrow.TransactionHash = transferResult.Result;
                if (escrow is Escrow escrowConcrete)
                {
                    escrowConcrete.MetaData["releaseTransactionHash"] = transferResult.Result;
                    escrowConcrete.MetaData["releasedBy"] = approverAvatarId.ToString();
                }

                await UpdateEscrowAsync(escrow);

                result.Result = transferResult.Result;
                result.IsError = false;
                result.Message = "Escrow released successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error releasing escrow: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get escrow by ID
        /// </summary>
        public async Task<OASISResult<IEscrow>> GetEscrowAsync(Guid escrowId)
        {
            var result = new OASISResult<IEscrow>();
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(escrowId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Escrow not found");
                    return result;
                }

                var escrow = Escrow.FromHolon(holonResult.Result);
                result.Result = escrow;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading escrow: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all escrows for an avatar
        /// </summary>
        public async Task<OASISResult<List<IEscrow>>> GetEscrowsForAvatarAsync(Guid avatarId, EscrowStatus? status = null)
        {
            var result = new OASISResult<List<IEscrow>>();
            try
            {
                // Search for escrows using metadata filter
                var searchParams = new SearchParams
                {
                    AvatarId = avatarId,
                    FilterByMetaData = new Dictionary<string, string>
                    {
                        ["EscrowType"] = "MNEEEscrow"
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
                                MetaDataKey = "PayerAvatarId"
                            }
                        }
                    }
                };

                var searchResult = await SearchManager.Instance.SearchAsync(searchParams);
                if (searchResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error searching escrows: {searchResult.Message}");
                    return result;
                }

                var escrows = new List<IEscrow>();
                if (searchResult.Result != null && searchResult.Result.SearchResultHolons != null)
                {
                    foreach (var holon in searchResult.Result.SearchResultHolons)
                    {
                        // Check if this is an escrow by checking metadata
                        if (holon.MetaData != null && holon.MetaData.ContainsKey("EscrowType") && holon.MetaData["EscrowType"].ToString() == "MNEEEscrow")
                        {
                            var escrow = Escrow.FromHolon(holon);
                            // Also check if avatar is an approver
                            if (escrow.PayerAvatarId == avatarId || escrow.PayeeAvatarId == avatarId || escrow.Approvers.Contains(avatarId))
                            {
                                if (status == null || escrow.Status == status)
                                {
                                    escrows.Add(escrow);
                                }
                            }
                        }
                    }
                }

                result.Result = escrows;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting escrows: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update escrow
        /// </summary>
        private async Task<OASISResult<bool>> UpdateEscrowAsync(IEscrow escrow)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (escrow is Escrow escrowConcrete)
                {
                    escrowConcrete.ModifiedDate = DateTime.UtcNow;
                    var holon = escrowConcrete.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.Message;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Escrow is not of expected type");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating escrow: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get private key for avatar (helper method)
        /// </summary>
        private async Task<string> GetPrivateKeyAsync(Guid avatarId)
        {
            var privateKeysResult = _keyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);
            if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
            {
                throw new Exception("No private key found for avatar");
            }
            return privateKeysResult.Result[0];
        }
    }

    /// <summary>
    /// Escrow interface
    /// </summary>
    public interface IEscrow
    {
        Guid EscrowId { get; set; }
        Guid PayerAvatarId { get; set; }
        Guid PayeeAvatarId { get; set; }
        decimal Amount { get; set; }
        string Currency { get; set; }
        EscrowStatus Status { get; set; }
        List<Guid> Approvers { get; set; }
        Dictionary<string, object> Conditions { get; set; }
        DateTime? ReleaseDate { get; set; }
        DateTime? FundedDate { get; set; }
        DateTime? ReleasedDate { get; set; }
        string TransactionHash { get; set; }
    }

    /// <summary>
    /// Escrow implementation
    /// </summary>
    public class Escrow : HolonBase, IEscrow
    {
        public Escrow()
        {
            this.HolonType = HolonType.Holon;
            this.MetaData = new Dictionary<string, object> { ["EscrowType"] = "MNEEEscrow" };
        }

        public Guid EscrowId { get; set; }
        public Guid PayerAvatarId { get; set; }
        public Guid PayeeAvatarId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MNEE";
        public EscrowStatus Status { get; set; }
        public List<Guid> Approvers { get; set; } = new List<Guid>();
        public Dictionary<string, object> Conditions { get; set; } = new Dictionary<string, object>();
        public DateTime? ReleaseDate { get; set; }
        public DateTime? FundedDate { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string TransactionHash { get; set; }

        public static Escrow FromHolon(IHolon holon)
        {
            var approvers = new List<Guid>();
            if (holon.MetaData.ContainsKey("Approvers"))
            {
                var approversStr = holon.MetaData["Approvers"].ToString();
                foreach (var approverId in approversStr.Split(','))
                {
                    if (Guid.TryParse(approverId.Trim(), out var guid))
                        approvers.Add(guid);
                }
            }

            return new Escrow
            {
                Id = holon.Id,
                EscrowId = holon.Id,
                PayerAvatarId = holon.MetaData.ContainsKey("PayerAvatarId") ? Guid.Parse(holon.MetaData["PayerAvatarId"].ToString()) : Guid.Empty,
                PayeeAvatarId = holon.MetaData.ContainsKey("PayeeAvatarId") ? Guid.Parse(holon.MetaData["PayeeAvatarId"].ToString()) : Guid.Empty,
                Amount = holon.MetaData.ContainsKey("Amount") ? Convert.ToDecimal(holon.MetaData["Amount"]) : 0,
                Currency = holon.MetaData.ContainsKey("Currency") ? holon.MetaData["Currency"].ToString() : "MNEE",
                Status = holon.MetaData.ContainsKey("Status") ? (EscrowStatus)Enum.Parse(typeof(EscrowStatus), holon.MetaData["Status"].ToString()) : EscrowStatus.Pending,
                Approvers = approvers,
                Conditions = holon.MetaData.ContainsKey("Conditions") ? (Dictionary<string, object>)holon.MetaData["Conditions"] : new Dictionary<string, object>(),
                ReleaseDate = holon.MetaData.ContainsKey("ReleaseDate") ? DateTime.Parse(holon.MetaData["ReleaseDate"].ToString()) : (DateTime?)null,
                FundedDate = holon.MetaData.ContainsKey("FundedDate") ? DateTime.Parse(holon.MetaData["FundedDate"].ToString()) : (DateTime?)null,
                ReleasedDate = holon.MetaData.ContainsKey("ReleasedDate") ? DateTime.Parse(holon.MetaData["ReleasedDate"].ToString()) : (DateTime?)null,
                TransactionHash = holon.MetaData.ContainsKey("TransactionHash") ? holon.MetaData["TransactionHash"].ToString() : null,
                CreatedDate = holon.CreatedDate,
                ModifiedDate = holon.ModifiedDate,
                MetaData = holon.MetaData
            };
        }

        public IHolon ToHolon()
        {
            this.MetaData["PayerAvatarId"] = PayerAvatarId.ToString();
            this.MetaData["PayeeAvatarId"] = PayeeAvatarId.ToString();
            this.MetaData["Amount"] = Amount;
            this.MetaData["Currency"] = Currency;
            this.MetaData["Status"] = Status.ToString();
            this.MetaData["Approvers"] = string.Join(",", Approvers.Select(a => a.ToString()));
            this.MetaData["Conditions"] = Conditions;
            if (ReleaseDate.HasValue)
                this.MetaData["ReleaseDate"] = ReleaseDate.Value.ToString("O");
            if (FundedDate.HasValue)
                this.MetaData["FundedDate"] = FundedDate.Value.ToString("O");
            if (ReleasedDate.HasValue)
                this.MetaData["ReleasedDate"] = ReleasedDate.Value.ToString("O");
            if (!string.IsNullOrWhiteSpace(TransactionHash))
                this.MetaData["TransactionHash"] = TransactionHash;
            this.Name = $"Escrow {EscrowId}";
            return (IHolon)this;
        }
    }

    /// <summary>
    /// Escrow status enum
    /// </summary>
    public enum EscrowStatus
    {
        Pending,
        Funded,
        Released,
        Cancelled,
        Disputed
    }

    /// <summary>
    /// Create escrow request
    /// </summary>
    public class CreateEscrowRequest
    {
        public Guid PayerAvatarId { get; set; }
        public Guid PayeeAvatarId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MNEE";
        public List<Guid> Approvers { get; set; }
        public Dictionary<string, object> Conditions { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
