using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    public enum HyperDriveMessageAction { Send = 1, MarkDelivered = 2, MarkRead = 3 }

    /// <summary>
    /// Offline messaging command. For <see cref="HyperDriveMessageAction.Send"/> the command's entity id is the
    /// client-generated message id, so replaying the command after a retry or over another route is idempotent.
    /// The sender is always the authenticated avatar, never a payload field.
    /// </summary>
    public sealed class HyperDriveMessageCommand
    {
        public HyperDriveMessageAction Action { get; set; }
        public Guid ToAvatarId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public DateTime? ClientTimestamp { get; set; }
        public List<Guid> MessageIds { get; set; }
    }
}
