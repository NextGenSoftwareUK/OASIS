using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Preventive action record
    /// </summary>
    public class PreventiveAction
    {
        public DateTime Timestamp { get; set; }
        public ProviderType FromProvider { get; set; }
        public ProviderType ToProvider { get; set; }
        public PreventiveActionType ActionType { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; }
    }
}
