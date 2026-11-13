using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Usage visibility for app owners, usage warnings and app-pool pay-as-you-go. These are the node-wide defaults (on, except email);
    /// each person and each app account can override them for themselves in the portal (their choice wins over this setting). Set in OASIS_DNA.json under OASIS.UsageVisibility (on Railway edit OASIS_DNA_JSON).
    /// </summary>
    public class UsageVisibilitySettings
    {
        /// <summary>Lets avatars linked as owners of an app account open usage for ONLY that app. Admin owner assignment works regardless.</summary>
        public bool OwnerViewEnabled { get; set; } = true;
        /// <summary>Lets people and app owners choose warning thresholds and see in-portal usage alerts.</summary>
        public bool WarningsEnabled { get; set; } = true;
        /// <summary>Email usage warnings (once per threshold per month) through the existing OASIS.Email settings. Nothing is sent unless email is configured there, and anyone can opt out.</summary>
        public bool EmailEnabled { get; set; } = true;
        /// <summary>Warning thresholds (percent of the monthly limit) used until someone chooses their own.</summary>
        public List<int> DefaultWarningPercents { get; set; } = new List<int> { 80, 100 };
        /// <summary>Settings and display of pay-as-you-go for app pools. Even when true, nothing is charged and an app cannot enable it: it needs prices in app-overage-prices.json, which ships empty, and the charging step is not built.</summary>
        public bool AppPayAsYouGoEnabled { get; set; } = true;
    }
}
