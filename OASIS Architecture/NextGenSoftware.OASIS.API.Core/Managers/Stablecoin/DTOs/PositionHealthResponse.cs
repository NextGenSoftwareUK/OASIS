namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs
{
    /// <summary>
    /// Response for position health check
    /// </summary>
    public class PositionHealthResponse
    {
        /// <summary>
        /// Health status
        /// </summary>
        public string Health { get; set; } = string.Empty;

        /// <summary>
        /// Current collateral ratio
        /// </summary>
        public decimal Ratio { get; set; }

        /// <summary>
        /// Collateral amount
        /// </summary>
        public decimal CollateralAmount { get; set; }

        /// <summary>
        /// Debt amount
        /// </summary>
        public decimal DebtAmount { get; set; }

        /// <summary>
        /// Current ZEC price
        /// </summary>
        public decimal ZecPrice { get; set; }
    }
}

