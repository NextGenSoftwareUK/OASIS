using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Messaging
{
    public class CreateGroupRequest
    {
        public string Title { get; set; }
        public List<Guid> ParticipantIds { get; set; } = new List<Guid>();
    }
}
