namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs
{
    /// <summary>
    /// System-wide stablecoin status and metrics
    /// </summary>
    public class SystemStatus
    {
        /// <summary>
        /// Total zUSD supply (all minted stablecoin)
        /// </summary>
        public decimal TotalSupply { get; set; }

        /// <summary>
        /// Total ZEC locked as collateral across all positions
        /// </summary>
        public decimal TotalCollateral { get; set; }

        /// <summary>
        /// System-wide collateral ratio (percentage)
        /// </summary>
        public decimal CollateralRatio { get; set; }

        /// <summary>
        /// Liquidation threshold (minimum collateral ratio before liquidation)
        /// </summary>
        public decimal LiquidationThreshold { get; set; } = 110m; // 110%

        /// <summary>
        /// Current APY for positions
        /// </summary>
        public decimal CurrentAPY { get; set; }

        /// <summary>
        /// Current ZEC price in USD (from oracle)
        /// </summary>
        public decimal ZecPrice { get; set; }

        /// <summary>
        /// Minimum collateral ratio for new positions
        /// </summary>
        public decimal MinimumCollateralRatio { get; set; } = 150m; // 150%

        /// <summary>
        /// Number of active positions
        /// </summary>
        public int ActivePositions { get; set; }

        /// <summary>
        /// Number of liquidated positions
        /// </summary>
        public int LiquidatedPositions { get; set; }
    }
}

