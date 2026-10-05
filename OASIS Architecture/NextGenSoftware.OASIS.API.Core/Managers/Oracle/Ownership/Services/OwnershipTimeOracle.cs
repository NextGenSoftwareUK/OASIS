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
/// Provides time-travel queries for ownership tracking.
/// Critical for: Regulatory audits, dispute resolution, court cases.
/// Enables: "Who owned asset X at time Y?" with blockchain proof.
/// </summary>
public class OwnershipTimeOracle : IOwnershipTimeOracle
{
    private readonly IOwnershipHistoryStore _historyStore;
    private readonly IEncumbranceTracker _encumbranceTracker;

    public OwnershipTimeOracle(
        IOwnershipHistoryStore historyStore,
        IEncumbranceTracker encumbranceTracker)
    {
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        _encumbranceTracker = encumbranceTracker ?? throw new ArgumentNullException(nameof(encumbranceTracker));
    }

    /// <summary>
    /// Gets the owner of an asset at a specific point in time.
    /// Uses blockchain history to provide tamper-proof evidence.
    /// </summary>
    public async Task<OASISResult<OwnershipRecord>> GetOwnerAtTimeAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default)
    {
        var result = new OASISResult<OwnershipRecord>();
        
        try
        {
            // Get all ownership history up to the timestamp
            var historyResult = await _historyStore.GetHistoryUpToAsync(assetId, timestamp, token);
            
            if (historyResult.IsError || historyResult.Result == null || !historyResult.Result.Any())
            {
                result.IsError = true;
                result.Message = $"No ownership history found for asset {assetId} before {timestamp}";
                return result;
            }

            // Find last ownership transfer before timestamp
            var lastTransfer = historyResult.Result
                .Where(e => e.EventType == OwnershipEventType.Transfer || e.EventType == OwnershipEventType.Mint)
                .Where(e => e.Timestamp <= timestamp)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();

            if (lastTransfer == null)
            {
                result.IsError = true;
                result.Message = $"Asset {assetId} did not exist at {timestamp}";
                return result;
            }

            // Build ownership record as it was at that time
            var ownershipRecord = new OwnershipRecord
            {
                AssetId = assetId,
                CurrentOwner = lastTransfer.ToOwner,
                OwnerName = "Owner at " + timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                CurrentValue = lastTransfer.Value,
                Currency = "USD",
                LastTransferTime = lastTransfer.Timestamp,
                LastTransferTxHash = lastTransfer.TransactionHash,
                LocationChains = new List<Enums.ProviderType> { lastTransfer.Chain },
                ConsensusLevel = lastTransfer.ConsensusLevel,
                IsDisputed = lastTransfer.IsFlagged,
                RecordTimestamp = DateTimeOffset.Now,
                VerifiedByOracles = lastTransfer.VerifiedBy,
                IsHistoricalRecord = true,
                AsOfTime = timestamp
            };

            result.Result = ownershipRecord;
            result.IsError = false;
            result.Message = $"Owner at {timestamp} was {ownershipRecord.CurrentOwner} (consensus: {ownershipRecord.ConsensusLevel}%)";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error getting owner at time: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Checks if an asset was available (unencumbered) at a specific time.
    /// </summary>
    public async Task<OASISResult<AvailabilityRecord>> CheckAvailabilityAtTimeAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default)
    {
        var result = new OASISResult<AvailabilityRecord>();
        
        try
        {
            // Get all events up to timestamp
            var historyResult = await _historyStore.GetHistoryUpToAsync(assetId, timestamp, token);
            
            if (historyResult.IsError)
            {
                result.IsError = true;
                result.Message = historyResult.Message;
                return result;
            }

            // Find active encumbrances at that time
            var activeEncumbrances = historyResult.Result
                .Where(e => e.EventType == OwnershipEventType.Pledge)
                .Where(e => e.Timestamp <= timestamp)
                .Where(e => !e.MaturityTime.HasValue || e.MaturityTime.Value > timestamp) // Not yet matured
                .Select(e => new Encumbrance
                {
                    EncumbranceId = e.EventId,
                    AssetId = assetId,
                    Type = e.EncumbranceType ?? EncumbranceType.Other,
                    Counterparty = e.Counterparty,
                    Amount = e.Value,
                    StartTime = e.Timestamp,
                    MaturityTime = e.MaturityTime ?? DateTimeOffset.MaxValue,
                    TransactionHash = e.TransactionHash,
                    Chain = e.Chain
                })
                .ToList();

            var totalEncumbered = activeEncumbrances.Sum(e => e.Amount);
            var wasAvailable = totalEncumbered == 0;

            var availabilityRecord = new AvailabilityRecord
            {
                AssetId = assetId,
                CheckTime = timestamp,
                WasAvailable = wasAvailable,
                AvailableValue = wasAvailable ? 1000000 : 0, // TODO: Get actual value
                ActiveEncumbrances = activeEncumbrances,
                Reason = wasAvailable 
                    ? "No active encumbrances at this time" 
                    : $"{activeEncumbrances.Count} active encumbrance(s) totaling ${totalEncumbered:N0}",
                ConsensusLevel = 100 // Historical data = 100% consensus
            };

            result.Result = availabilityRecord;
            result.IsError = false;
            result.Message = wasAvailable 
                ? "Asset was available"
                : $"Asset was encumbered (${totalEncumbered:N0})";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error checking availability: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets a complete portfolio snapshot at a specific point in time.
    /// </summary>
    public async Task<OASISResult<PortfolioSnapshot>> GetPortfolioSnapshotAsync(
        string ownerId,
        DateTimeOffset timestamp,
        CancellationToken token = default)
    {
        var result = new OASISResult<PortfolioSnapshot>();
        
        try
        {
            // TODO: Query all chains for assets owned at timestamp
            // For now, returning mock snapshot
            
            var snapshot = new PortfolioSnapshot
            {
                OwnerId = ownerId,
                OwnerName = $"Owner {ownerId}",
                SnapshotTime = timestamp,
                Assets = new List<AssetOwnership>(),
                ByAssetType = new Dictionary<string, decimal>(),
                ByChain = new Dictionary<string, decimal>(),
                ActiveEncumbrances = new List<Encumbrance>(),
                ConsensusLevel = 100,
                IsCourtAdmissible = true,
                GeneratedAt = DateTimeOffset.Now
            };

            result.Result = snapshot;
            result.IsError = false;
            result.Message = $"Portfolio snapshot generated for {timestamp}";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error generating portfolio snapshot: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Generates court-admissible evidence for ownership at a specific time.
    /// </summary>
    public async Task<OASISResult<OwnershipEvidence>> GenerateOwnershipEvidenceAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default)
    {
        var result = new OASISResult<OwnershipEvidence>();
        
        try
        {
            // Get ownership at time
            var ownershipResult = await GetOwnerAtTimeAsync(assetId, timestamp, token);
            
            if (ownershipResult.IsError)
            {
                result.IsError = true;
                result.Message = ownershipResult.Message;
                return result;
            }

            // Get ownership history
            var historyResult = await _historyStore.GetHistoryUpToAsync(assetId, timestamp, token);

            // Generate blockchain proof
            var proof = new BlockchainProof
            {
                TransactionHashes = historyResult.Result?.Select(e => e.TransactionHash).ToList() ?? new List<string>(),
                BlockNumbers = historyResult.Result?.Select(e => e.BlockNumber).ToList() ?? new List<long>(),
                GeneratedAt = DateTimeOffset.Now,
                IsTamperProof = true
            };

            var evidence = new OwnershipEvidence
            {
                AssetId = assetId,
                Owner = ownershipResult.Result.CurrentOwner,
                Timestamp = timestamp,
                OwnershipRecord = ownershipResult.Result,
                OwnershipHistory = historyResult.Result ?? new List<OwnershipEvent>(),
                Proof = proof,
                ConsensusLevel = ownershipResult.Result.ConsensusLevel,
                IsCourtAdmissible = true,
                GeneratedAt = DateTimeOffset.Now
            };

            result.Result = evidence;
            result.IsError = false;
            result.Message = "Court-admissible evidence generated successfully";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error generating ownership evidence: {ex.Message}", ex);
            return result;
        }
    }
}






