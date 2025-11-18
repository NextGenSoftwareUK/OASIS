using NextGenSoftware.OASIS.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Interfaces
{
    /// <summary>
    /// Interface for chain observers that monitor blockchain state and provide oracle data.
    /// Inspired by API3's first-party oracle approach - allows data providers to run their own oracle nodes.
    /// </summary>
    public interface IChainObserver
    {
        /// <summary>
        /// Gets the chain identifier (e.g., "Radix", "Ethereum", "Solana")
        /// </summary>
        string ChainName { get; }

        /// <summary>
        /// Gets whether the observer is currently monitoring the chain
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Gets the current chain state (block height, epoch, health, etc.)
        /// </summary>
        Task<OASISResult<ChainStateData>> GetChainStateAsync(CancellationToken token = default);

        /// <summary>
        /// Gets the latest block information
        /// </summary>
        Task<OASISResult<BlockData>> GetLatestBlockAsync(CancellationToken token = default);

        /// <summary>
        /// Gets transaction details by hash
        /// </summary>
        Task<OASISResult<TransactionData>> GetTransactionAsync(string transactionHash, CancellationToken token = default);

        /// <summary>
        /// Verifies a transaction's validity and status
        /// </summary>
        Task<OASISResult<TransactionVerification>> VerifyTransactionAsync(string transactionHash, CancellationToken token = default);

        /// <summary>
        /// Gets price feed data for a token (e.g., XRD/USD)
        /// </summary>
        Task<OASISResult<PriceData>> GetPriceFeedAsync(string tokenSymbol, string currency = "USD", CancellationToken token = default);

        /// <summary>
        /// Starts monitoring the chain for events
        /// </summary>
        Task<OASISResult<bool>> StartMonitoringAsync(CancellationToken token = default);

        /// <summary>
        /// Stops monitoring the chain
        /// </summary>
        Task<OASISResult<bool>> StopMonitoringAsync(CancellationToken token = default);

        /// <summary>
        /// Gets chain health metrics
        /// </summary>
        Task<OASISResult<ChainHealthData>> GetChainHealthAsync(CancellationToken token = default);

        /// <summary>
        /// Event fired when a chain event occurs (new block, transaction, etc.)
        /// </summary>
        event EventHandler<ChainEventData> OnChainEvent;

        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event EventHandler<OASISErrorEventArgs> OnError;
    }

    /// <summary>
    /// Chain state data structure
    /// </summary>
    public class ChainStateData
    {
        public string ChainName { get; set; } = string.Empty;
        public ulong BlockHeight { get; set; }
        public ulong? Epoch { get; set; } // For Radix
        public DateTime LastBlockTime { get; set; }
        public bool IsHealthy { get; set; }
        public decimal? GasPrice { get; set; } // For EVM chains
        public string NetworkId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Block data structure
    /// </summary>
    public class BlockData
    {
        public string BlockHash { get; set; } = string.Empty;
        public ulong BlockHeight { get; set; }
        public ulong? Epoch { get; set; }
        public DateTime Timestamp { get; set; }
        public int TransactionCount { get; set; }
        public string PreviousBlockHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transaction data structure
    /// </summary>
    public class TransactionData
    {
        public string TransactionHash { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TokenSymbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public ulong BlockHeight { get; set; }
        public ulong? Epoch { get; set; }
    }

    /// <summary>
    /// Transaction verification result
    /// </summary>
    public class TransactionVerification
    {
        public string TransactionHash { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool IsConfirmed { get; set; }
        public int Confirmations { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; }
        public decimal Confidence { get; set; } // 0-100%
    }

    /// <summary>
    /// Price data structure
    /// </summary>
    public class PriceData
    {
        public string TokenSymbol { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal? Change24h { get; set; }
        public decimal? Volume24h { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chain health metrics
    /// </summary>
    public class ChainHealthData
    {
        public string ChainName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public decimal Uptime { get; set; } // 0-100%
        public TimeSpan AverageResponseTime { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastHealthCheck { get; set; }
    }

    /// <summary>
    /// Chain event data
    /// </summary>
    public class ChainEventData : EventArgs
    {
        public string EventType { get; set; } = string.Empty; // "NewBlock", "NewTransaction", "PriceUpdate", etc.
        public string ChainName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public object? EventData { get; set; }
    }
}

