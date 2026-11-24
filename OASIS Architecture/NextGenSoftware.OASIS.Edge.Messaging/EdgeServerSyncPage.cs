using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    public sealed class EdgeServerSyncPage
    {
        public List<EdgeServerMessage> Messages { get; set; } = new List<EdgeServerMessage>();
        public DateTime? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
