namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>
    /// OPORTAL subscription plans, matching the plan IDs returned by GET /api/subscription/plans.
    /// The plan drives the base AI access tier in WEB6; karma can boost the effective tier by one level.
    /// </summary>
    public enum SubscriptionPlan
    {
        /// <summary>No active subscription — evaluation access, lowest model tier.</summary>
        Free = 0,

        /// <summary>Bronze plan ($9/mo) — developer entry tier.</summary>
        Bronze = 1,

        /// <summary>Silver plan ($29/mo) — individual developers and small projects.</summary>
        Silver = 2,

        /// <summary>Gold plan ($99/mo) — teams and production applications.</summary>
        Gold = 3,

        /// <summary>Enterprise plan (custom) — unlimited access, SLA, priority routing.</summary>
        Enterprise = 4,
    }
}
