using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class Key
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public KeyType KeyType { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}