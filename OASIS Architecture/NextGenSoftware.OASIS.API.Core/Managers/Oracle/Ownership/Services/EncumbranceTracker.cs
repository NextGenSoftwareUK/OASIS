using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Services;

/// <summary>
/// Tracks all encumbrances (pledges, liens, locks) across all blockchains.
/// Critical for: Knowing what collateral is actually available vs locked.
/// </summary>
public class EncumbranceTracker : IEncumbranceTracker
{
    private readonly IOwnershipHistoryStore _historyStore;
    // TODO: Will be injected when chain observers are implemented
    // private readonly IEnumerable<IChainObserver> _chainObservers;

    public EncumbranceTracker(IOwnershipHistoryStore historyStore)
    {
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
    }

    /// <summary>
    /// Checks if an asset has any active encumbrances.
    /// Quick check for availability before attempting operations.
    /// </summary>
    public async Task<OASISResult<bool>> CheckEncumbranceAsync(
        string assetId,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            // Get all active encumbrances for the asset
            var encumbrancesResult = await GetActiveEncumbrancesAsync(assetId, token);
            
            if (encumbrancesResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Error checking encumbrances: {encumbrancesResult.Message}";
                return result;
            }

            // Asset is encumbered if there are any active encumbrances
            result.Result = encumbrancesResult.Result?.Any() ?? false;
            result.IsError = false;
            result.Message = result.Result 
                ? $"Asset {assetId} has {encumbrancesResult.Result?.Count ?? 0} active encumbrance(s)"
                : $"Asset {assetId} is available (no active encumbrances)";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error checking encumbrance status: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets all active encumbrances for an asset.
    /// </summary>
    public async Task<OASISResult<List<Encumbrance>>> GetActiveEncumbrancesAsync(
        string assetId,
        CancellationToken token = default)
    {
        var result = new OASISResult<List<Encumbrance>>();
        
        try
        {
            // TODO: Query all chain observers for active pledges
            // For now, returning mock data
            var mockEncumbrances = new List<Encumbrance>
            {
                new Encumbrance
                {
                    EncumbranceId = Guid.NewGuid().ToString(),
                    AssetId = assetId,
                    Type = EncumbranceType.Repo,
                    Owner = "BankA",
                    Counterparty = "JP Morgan",
                    Amount = 500000, // $500k
                    StartTime = DateTimeOffset.Now.AddHours(-4),
                    MaturityTime = DateTimeOffset.Now.AddHours(2), // Matures in 2 hours
                    Priority = 1, // First lien
                    TransactionHash = "0x" + new string('a', 64),
                    Chain = Enums.ProviderType.EthereumOASIS,
                    SmartContractAddress = "0x" + new string('b', 40),
                    IsActive = true,
                    InterestRate = 5.25m,
                    Haircut = 2.0m,
                    AutoRelease = true,
                    CreatedAt = DateTimeOffset.Now.AddHours(-4),
                    UpdatedAt = DateTimeOffset.Now
                }
            };

            result.Result = mockEncumbrances;
            result.IsError = false;
            result.Message = $"Found {mockEncumbrances.Count} active encumbrances";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error checking encumbrance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets all pledges made by an owner across all chains.
    /// </summary>
    public async Task<OASISResult<List<Encumbrance>>> GetAllPledgesAsync(
        string ownerId,
        CancellationToken token = default)
    {
        var result = new OASISResult<List<Encumbrance>>();
        
        try
        {
            // TODO: Query all chain observers for pledges by this owner
            result.Result = new List<Encumbrance>();
            result.IsError = false;
            result.Message = $"Retrieved pledges for owner {ownerId}";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error retrieving pledges: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets maturity schedule showing when pledged collateral becomes available.
    /// Critical for: "When will I have $X available?"
    /// </summary>
    public async Task<OASISResult<List<MaturitySchedule>>> GetMaturityScheduleAsync(
        string ownerId,
        int hoursAhead = 24,
        CancellationToken token = default)
    {
        var result = new OASISResult<List<MaturitySchedule>>();
        
        try
        {
            // Get all pledges for this owner
            var pledgesResult = await GetAllPledgesAsync(ownerId, token);
            
            if (pledgesResult.IsError)
            {
                result.IsError = true;
                result.Message = pledgesResult.Message;
                return result;
            }

            var now = DateTimeOffset.Now;
            var cutoff = now.AddHours(hoursAhead);

            // Group by maturity time (rounded to hour)
            var schedule = pledgesResult.Result
                .Where(p => p.IsActive && p.MaturityTime >= now && p.MaturityTime <= cutoff)
                .GroupBy(p => new DateTimeOffset(
                    p.MaturityTime.Year,
                    p.MaturityTime.Month,
                    p.MaturityTime.Day,
                    p.MaturityTime.Hour,
                    0,
                    0,
                    p.MaturityTime.Offset))
                .Select(g => new MaturitySchedule
                {
                    Time = g.Key,
                    Assets = g.ToList(),
                    TotalValueFreeing = g.Sum(p => p.Amount),
                    AssetTypes = g.Select(p => p.Type.ToString()).Distinct().ToList(),
                    Counterparties = g.Select(p => p.Counterparty).Distinct().ToList(),
                    Chains = g.Select(p => p.Chain.ToString()).Distinct().ToList(),
                    HasAutoRelease = g.All(p => p.AutoRelease)
                })
                .OrderBy(m => m.Time)
                .ToList();

            result.Result = schedule;
            result.IsError = false;
            result.Message = $"Retrieved {schedule.Count} maturity events in next {hoursAhead} hours";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error retrieving maturity schedule: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Creates a new encumbrance (pledge, lien, lock).
    /// </summary>
    public async Task<OASISResult<Encumbrance>> CreateEncumbranceAsync(
        CreateEncumbranceRequest request,
        CancellationToken token = default)
    {
        var result = new OASISResult<Encumbrance>();
        
        try
        {
            // Validate request
            if (request == null)
            {
                result.IsError = true;
                result.Message = "Encumbrance request is required";
                return result;
            }

            // TODO: Execute smart contract to create pledge on specified chain
            
            var encumbrance = new Encumbrance
            {
                EncumbranceId = Guid.NewGuid().ToString(),
                AssetId = request.AssetId,
                Type = request.Type,
                Owner = request.Owner,
                Counterparty = request.Counterparty,
                Amount = request.Amount,
                StartTime = DateTimeOffset.Now,
                MaturityTime = request.MaturityTime,
                Priority = request.Priority,
                TransactionHash = "0x" + Guid.NewGuid().ToString("N"),
                Chain = request.Chain,
                IsActive = true,
                InterestRate = request.InterestRate,
                Haircut = request.Haircut,
                AutoRelease = request.AutoRelease,
                Terms = request.Terms,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now
            };

            // Record to history
            var eventRecord = new OwnershipEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = OwnershipEventType.Pledge,
                AssetId = request.AssetId,
                FromOwner = request.Owner,
                ToOwner = request.Owner, // Owner doesn't change on pledge
                Value = request.Amount,
                Chain = request.Chain,
                TransactionHash = encumbrance.TransactionHash,
                Timestamp = DateTimeOffset.Now,
                Counterparty = request.Counterparty,
                EncumbranceType = request.Type,
                MaturityTime = request.MaturityTime,
                ConsensusLevel = 100,
                RecordedAt = DateTimeOffset.Now
            };

            await _historyStore.RecordOwnershipEventAsync(eventRecord, token);

            result.Result = encumbrance;
            result.IsError = false;
            result.Message = $"Encumbrance created successfully. Matures at {encumbrance.MaturityTime}";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error creating encumbrance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Releases an encumbrance (e.g., repo matures, loan repaid).
    /// </summary>
    public async Task<OASISResult<EncumbranceRelease>> ReleaseEncumbranceAsync(
        string encumbranceId,
        CancellationToken token = default)
    {
        var result = new OASISResult<EncumbranceRelease>();
        
        try
        {
            // TODO: Execute smart contract to release pledge
            
            var release = new EncumbranceRelease
            {
                EncumbranceId = encumbranceId,
                AssetId = "ASSET_MOCK",
                ReleaseTime = DateTimeOffset.Now,
                ReleaseTransactionHash = "0x" + Guid.NewGuid().ToString("N"),
                Reason = "Maturity reached",
                WasAutomatic = true
            };

            // Record release event
            var eventRecord = new OwnershipEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = OwnershipEventType.Release,
                AssetId = release.AssetId,
                Timestamp = DateTimeOffset.Now,
                TransactionHash = release.ReleaseTransactionHash,
                ConsensusLevel = 100,
                RecordedAt = DateTimeOffset.Now
            };

            await _historyStore.RecordOwnershipEventAsync(eventRecord, token);

            result.Result = release;
            result.IsError = false;
            result.Message = "Encumbrance released successfully";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error releasing encumbrance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Monitors for encumbrances approaching maturity and triggers auto-release.
    /// Runs continuously in background.
    /// </summary>
    public async Task StartMaturityMonitoringAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // TODO: Query all active encumbrances
                // TODO: Check which have matured
                // TODO: Auto-release those with AutoRelease = true
                
                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue monitoring
                Console.WriteLine($"Error in maturity monitoring: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }
    }
}





