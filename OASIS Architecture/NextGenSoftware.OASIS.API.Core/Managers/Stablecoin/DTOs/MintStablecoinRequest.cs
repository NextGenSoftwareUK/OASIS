using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs
{
    /// <summary>
    /// Request to mint stablecoin (zUSD) with ZEC collateral
    /// </summary>
    public class MintStablecoinRequest
    {
        /// <summary>
        /// Amount of ZEC to lock as collateral
        /// </summary>
        public decimal ZecAmount { get; set; }

        /// <summary>
        /// Amount of zUSD to mint (calculated based on collateral ratio)
        /// </summary>
        public decimal StablecoinAmount { get; set; }

        /// <summary>
        /// Aztec wallet address to receive minted zUSD
        /// </summary>
        public string AztecAddress { get; set; } = string.Empty;

        /// <summary>
        /// Zcash wallet address to lock ZEC from
        /// </summary>
        public string ZcashAddress { get; set; } = string.Empty;

        /// <summary>
        /// Avatar ID of the user creating the position
        /// </summary>
        public Guid AvatarId { get; set; }

        /// <summary>
        /// Whether to generate a viewing key for private position tracking
        /// </summary>
        public bool GenerateViewingKey { get; set; } = true;
    }
}

