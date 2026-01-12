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
    /// Manages treasury operations with automated fund allocation
    /// Supports multi-wallet coordination, budget management, and automated workflows
    /// </summary>
    public class TreasuryManager : OASISManager
    {
        private static TreasuryManager _instance;
        private readonly WalletManager _walletManager;
        private readonly KeyManager _keyManager;

        public static TreasuryManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TreasuryManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public TreasuryManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _walletManager = new WalletManager(OASISStorageProvider, OASISDNA);
            _keyManager = new KeyManager(OASISStorageProvider, OASISDNA);
        }

        /// <summary>
        /// Create a new treasury
        /// </summary>
        public async Task<OASISResult<ITreasury>> CreateTreasuryAsync(CreateTreasuryRequest request)
        {
            var result = new OASISResult<ITreasury>();
            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Treasury request is required");
                    return result;
                }

                if (request.OwnerAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Owner avatar ID is required");
                    return result;
                }

                var treasury = new Treasury
                {
                    TreasuryId = Guid.NewGuid(),
                    OwnerAvatarId = request.OwnerAvatarId,
                    Name = request.Name ?? "Treasury",
                    Description = request.Description,
                    Wallets = request.Wallets ?? new List<TreasuryWallet>(),
                    Budgets = request.Budgets ?? new Dictionary<string, decimal>(),
                    Workflows = request.Workflows ?? new List<TreasuryWorkflow>(),
                    CreatedDate = DateTime.UtcNow,
                    MetaData = request.Metadata ?? new Dictionary<string, object>()
                };

                // Save treasury as Holon
                var holon = treasury.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error saving treasury: {saveResult.Message}");
                    return result;
                }

                result.Result = treasury;
                result.IsError = false;
                result.Message = "Treasury created successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating treasury: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Execute automated fund allocation workflow
        /// </summary>
        public async Task<OASISResult<Dictionary<string, string>>> ExecuteFundAllocationAsync(Guid treasuryId)
        {
            var result = new OASISResult<Dictionary<string, string>>();
            try
            {
                // Load treasury
                var treasuryResult = await GetTreasuryAsync(treasuryId);
                if (treasuryResult.IsError || treasuryResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Treasury not found: {treasuryResult.Message}");
                    return result;
                }

                var treasury = treasuryResult.Result;

                // Get main wallet balance
                var mainWallet = treasury.Wallets.FirstOrDefault(w => w.IsMain);
                if (mainWallet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "No main wallet found in treasury");
                    return result;
                }

                // Get Ethereum provider using reflection
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call GetMNEEBalanceForAvatarAsync
                var balanceMethod = providerResult.GetType().GetMethod("GetMNEEBalanceForAvatarAsync");
                if (balanceMethod == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE balance method not found on Ethereum provider");
                    return result;
                }

                var balanceTask = (Task<OASISResult<decimal>>)balanceMethod.Invoke(providerResult, new object[] { mainWallet.AvatarId });
                var balanceResult = await balanceTask;
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting balance: {balanceResult.Message}");
                    return result;
                }

                var totalBalance = balanceResult.Result;
                var transactionHashes = new Dictionary<string, string>();

                // Allocate funds according to budgets
                foreach (var budget in treasury.Budgets)
                {
                    var allocationAmount = totalBalance * (budget.Value / 100m); // Budget is percentage
                    
                    // Find target wallet for this budget category
                    var targetWallet = treasury.Wallets.FirstOrDefault(w => w.Category == budget.Key);
                    if (targetWallet != null && allocationAmount > 0)
                    {
                        // Use reflection to call TransferMNEEBetweenAvatarsAsync
                        var transferMethod = providerResult.GetType().GetMethod("TransferMNEEBetweenAvatarsAsync");
                        if (transferMethod != null)
                        {
                            var transferTask = (Task<OASISResult<string>>)transferMethod.Invoke(providerResult, new object[] { 
                                mainWallet.AvatarId,
                                targetWallet.AvatarId,
                                allocationAmount
                            });
                            var transferResult = await transferTask;

                            if (!transferResult.IsError)
                            {
                                transactionHashes[budget.Key] = transferResult.Result;
                            }
                        }
                    }
                }

                // Update treasury last allocation date
                treasury.LastAllocationDate = DateTime.UtcNow;
                if (treasury is Treasury treasuryConcrete)
                {
                    treasuryConcrete.MetaData["lastAllocationTransactions"] = transactionHashes;
                }
                await UpdateTreasuryAsync(treasury);

                result.Result = transactionHashes;
                result.IsError = false;
                result.Message = "Fund allocation executed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing fund allocation: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get treasury by ID
        /// </summary>
        public async Task<OASISResult<ITreasury>> GetTreasuryAsync(Guid treasuryId)
        {
            var result = new OASISResult<ITreasury>();
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(treasuryId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Treasury not found");
                    return result;
                }

                var treasury = Treasury.FromHolon(holonResult.Result);
                result.Result = treasury;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading treasury: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all treasuries for an avatar
        /// </summary>
        public async Task<OASISResult<List<ITreasury>>> GetTreasuriesForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<ITreasury>>();
            try
            {
                // Search for treasuries using metadata filter
                var searchParams = new SearchParams
                {
                    AvatarId = avatarId,
                    FilterByMetaData = new Dictionary<string, string>
                    {
                        ["TreasuryType"] = "MNEETreasury",
                        ["OwnerAvatarId"] = avatarId.ToString()
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
                                MetaDataKey = "OwnerAvatarId"
                            }
                        }
                    }
                };

                var searchResult = await SearchManager.Instance.SearchAsync(searchParams);
                if (searchResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error searching treasuries: {searchResult.Message}");
                    return result;
                }

                var treasuries = new List<ITreasury>();
                if (searchResult.Result != null && searchResult.Result.SearchResultHolons != null)
                {
                    foreach (var holon in searchResult.Result.SearchResultHolons)
                    {
                        if (holon.MetaData != null && holon.MetaData.ContainsKey("TreasuryType") && holon.MetaData["TreasuryType"].ToString() == "MNEETreasury")
                        {
                            var treasury = Treasury.FromHolon(holon);
                            treasuries.Add(treasury);
                        }
                    }
                }

                result.Result = treasuries;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting treasuries: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update treasury
        /// </summary>
        private async Task<OASISResult<bool>> UpdateTreasuryAsync(ITreasury treasury)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (treasury is Treasury treasuryConcrete)
                {
                    treasuryConcrete.ModifiedDate = DateTime.UtcNow;
                    var holon = treasuryConcrete.ToHolon();
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.Message;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Treasury is not of expected type");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating treasury: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get treasury balance summary
        /// </summary>
        public async Task<OASISResult<TreasuryBalanceSummary>> GetTreasuryBalanceSummaryAsync(Guid treasuryId)
        {
            var result = new OASISResult<TreasuryBalanceSummary>();
            try
            {
                var treasuryResult = await GetTreasuryAsync(treasuryId);
                if (treasuryResult.IsError || treasuryResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Treasury not found: {treasuryResult.Message}");
                    return result;
                }

                var treasury = treasuryResult.Result;

                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call GetMNEEBalanceForAvatarAsync
                var balanceMethod = providerResult.GetType().GetMethod("GetMNEEBalanceForAvatarAsync");
                if (balanceMethod == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE balance method not found on Ethereum provider");
                    return result;
                }

                var summary = new TreasuryBalanceSummary
                {
                    TreasuryId = treasuryId,
                    WalletBalances = new Dictionary<string, decimal>()
                };

                decimal totalBalance = 0;
                foreach (var wallet in treasury.Wallets)
                {
                    var balanceTask = (Task<OASISResult<decimal>>)balanceMethod.Invoke(providerResult, new object[] { wallet.AvatarId });
                    var balanceResult = await balanceTask;
                    if (!balanceResult.IsError)
                    {
                        summary.WalletBalances[wallet.Name] = balanceResult.Result;
                        totalBalance += balanceResult.Result;
                    }
                }

                summary.TotalBalance = totalBalance;
                result.Result = summary;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting treasury balance summary: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// Treasury interface
    /// </summary>
    public interface ITreasury
    {
        Guid TreasuryId { get; set; }
        Guid OwnerAvatarId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        List<TreasuryWallet> Wallets { get; set; }
        Dictionary<string, decimal> Budgets { get; set; }
        List<TreasuryWorkflow> Workflows { get; set; }
        DateTime? LastAllocationDate { get; set; }
    }

    /// <summary>
    /// Treasury implementation
    /// </summary>
    public class Treasury : HolonBase, ITreasury
    {
        public Treasury()
        {
            this.HolonType = HolonType.Holon;
            this.MetaData = new Dictionary<string, object> { ["TreasuryType"] = "MNEETreasury" };
        }

        public Guid TreasuryId { get; set; }
        public Guid OwnerAvatarId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TreasuryWallet> Wallets { get; set; } = new List<TreasuryWallet>();
        public Dictionary<string, decimal> Budgets { get; set; } = new Dictionary<string, decimal>();
        public List<TreasuryWorkflow> Workflows { get; set; } = new List<TreasuryWorkflow>();
        public DateTime? LastAllocationDate { get; set; }

        public static Treasury FromHolon(IHolon holon)
        {
            var wallets = new List<TreasuryWallet>();
            if (holon.MetaData.ContainsKey("Wallets"))
            {
                var walletsJson = holon.MetaData["Wallets"].ToString();
                // Simple deserialization - in production use proper JSON deserialization
                wallets = new List<TreasuryWallet>(); // Placeholder
            }

            var budgets = new Dictionary<string, decimal>();
            if (holon.MetaData.ContainsKey("Budgets"))
            {
                var budgetsDict = holon.MetaData["Budgets"] as Dictionary<string, object>;
                if (budgetsDict != null)
                {
                    foreach (var kvp in budgetsDict)
                    {
                        budgets[kvp.Key] = Convert.ToDecimal(kvp.Value);
                    }
                }
            }

            return new Treasury
            {
                Id = holon.Id,
                TreasuryId = holon.Id,
                OwnerAvatarId = holon.MetaData.ContainsKey("OwnerAvatarId") ? Guid.Parse(holon.MetaData["OwnerAvatarId"].ToString()) : Guid.Empty,
                Name = holon.Name,
                Description = holon.Description,
                Wallets = wallets,
                Budgets = budgets,
                Workflows = new List<TreasuryWorkflow>(), // Placeholder
                LastAllocationDate = holon.MetaData.ContainsKey("LastAllocationDate") ? DateTime.Parse(holon.MetaData["LastAllocationDate"].ToString()) : (DateTime?)null,
                CreatedDate = holon.CreatedDate,
                ModifiedDate = holon.ModifiedDate,
                MetaData = holon.MetaData
            };
        }

        public IHolon ToHolon()
        {
            this.MetaData["OwnerAvatarId"] = OwnerAvatarId.ToString();
            this.MetaData["Wallets"] = Wallets; // In production, serialize to JSON
            this.MetaData["Budgets"] = Budgets;
            this.MetaData["Workflows"] = Workflows; // In production, serialize to JSON
            if (LastAllocationDate.HasValue)
                this.MetaData["LastAllocationDate"] = LastAllocationDate.Value.ToString("O");
            this.Name = Name;
            this.Description = Description;
            return (IHolon)this;
        }
    }

    /// <summary>
    /// Treasury wallet
    /// </summary>
    public class TreasuryWallet
    {
        public Guid AvatarId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsMain { get; set; }
    }

    /// <summary>
    /// Treasury workflow
    /// </summary>
    public class TreasuryWorkflow
    {
        public Guid WorkflowId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public WorkflowTrigger Trigger { get; set; }
        public Dictionary<string, decimal> Allocation { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Workflow trigger enum
    /// </summary>
    public enum WorkflowTrigger
    {
        Manual,
        Scheduled,
        EventBased
    }

    /// <summary>
    /// Create treasury request
    /// </summary>
    public class CreateTreasuryRequest
    {
        public Guid OwnerAvatarId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TreasuryWallet> Wallets { get; set; }
        public Dictionary<string, decimal> Budgets { get; set; }
        public List<TreasuryWorkflow> Workflows { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Treasury balance summary
    /// </summary>
    public class TreasuryBalanceSummary
    {
        public Guid TreasuryId { get; set; }
        public Dictionary<string, decimal> WalletBalances { get; set; }
        public decimal TotalBalance { get; set; }
    }
}
