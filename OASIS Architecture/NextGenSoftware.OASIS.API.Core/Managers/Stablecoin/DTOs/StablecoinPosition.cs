using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs
{
    /// <summary>
    /// Represents a stablecoin position (ZEC collateral locked, zUSD minted)
    /// </summary>
    public class StablecoinPosition
    {
        /// <summary>
        /// Unique identifier for the position
        /// </summary>
        public Guid PositionId { get; set; }

        /// <summary>
        /// Avatar ID of the position owner
        /// </summary>
        public Guid AvatarId { get; set; }

        /// <summary>
        /// Amount of ZEC locked as collateral
        /// </summary>
        public decimal CollateralAmount { get; set; }

        /// <summary>
        /// Amount of zUSD minted (debt)
        /// </summary>
        public decimal DebtAmount { get; set; }

        /// <summary>
        /// Collateral ratio (percentage)
        /// </summary>
        public decimal CollateralRatio { get; set; }

        /// <summary>
        /// Health status of the position
        /// </summary>
        public PositionHealth Health { get; set; }

        /// <summary>
        /// When the position was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Hash of viewing key for private position tracking (optional)
        /// </summary>
        public string? ViewingKeyHash { get; set; }

        /// <summary>
        /// Zcash address used for collateral
        /// </summary>
        public string ZcashAddress { get; set; } = string.Empty;

        /// <summary>
        /// Aztec address receiving zUSD
        /// </summary>
        public string AztecAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Position health status
    /// </summary>
    public enum PositionHealth
    {
        Safe,        // > 150% collateral ratio
        Warning,     // 120-150% collateral ratio
        Danger,      // 110-120% collateral ratio
        Liquidated   // < 110% collateral ratio
    }
}

