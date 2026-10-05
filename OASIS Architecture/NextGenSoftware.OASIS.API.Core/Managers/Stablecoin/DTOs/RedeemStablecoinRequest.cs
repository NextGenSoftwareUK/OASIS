using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs
{
    /// <summary>
    /// Request to redeem stablecoin (zUSD) for ZEC collateral
    /// </summary>
    public class RedeemStablecoinRequest
    {
        /// <summary>
        /// Position ID to redeem from
        /// </summary>
        public Guid PositionId { get; set; }

        /// <summary>
        /// Amount of zUSD to redeem/burn
        /// </summary>
        public decimal StablecoinAmount { get; set; }

        /// <summary>
        /// Zcash wallet address to receive unlocked ZEC
        /// </summary>
        public string ZcashAddress { get; set; } = string.Empty;

        /// <summary>
        /// Avatar ID of the user redeeming
        /// </summary>
        public Guid AvatarId { get; set; }
    }
}

