using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Failure event for learning
    /// </summary>
    public class FailureEvent
    {
        public DateTime Timestamp { get; set; }
        public ProviderType ProviderType { get; set; }
        public FailureType FailureType { get; set; }
        public string Description { get; set; }
        public double Duration { get; set; }
        public string Cause { get; set; }
        public string Resolution { get; set; }
    }
}
