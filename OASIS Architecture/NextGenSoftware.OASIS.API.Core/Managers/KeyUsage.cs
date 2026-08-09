using System;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class KeyUsage
    {
        public Guid Id { get; set; }
        public Guid KeyId { get; set; }
        public Guid AvatarId { get; set; }
        public string Purpose { get; set; }
        public DateTime UsedAt { get; set; }
    }
}