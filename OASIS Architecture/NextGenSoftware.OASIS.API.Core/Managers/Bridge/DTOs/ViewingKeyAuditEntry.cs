using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs
{
    public class ViewingKeyAuditEntry
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ViewingKey { get; set; } = string.Empty;
        public string SourceChain { get; set; } = string.Empty;
        public string DestinationChain { get; set; } = string.Empty;
        public string DestinationAddress { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }
}

