namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>
    /// OPORTAL subscription plans that gate WEB6 AI model access.
    /// Karma can boost the effective tier by one level above the plan baseline.
    /// </summary>
    public enum SubscriptionPlan
    {
        /// <summary>No active subscription — free evaluation access.</summary>
        Free = 0,

        /// <summary>Starter plan — individual developers, side-projects.</summary>
        Starter = 1,

        /// <summary>Pro plan — production apps, teams.</summary>
        Pro = 2,

        /// <summary>Enterprise plan — unlimited access, SLA, priority routing.</summary>
        Enterprise = 3,
    }
}
