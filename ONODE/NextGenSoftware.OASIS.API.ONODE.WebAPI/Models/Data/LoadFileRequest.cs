using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadFileRequest : OASISRequest
    {
        public Guid Id { get; set; }
        public string Provider { get; set; }
        public Guid? AvatarId { get; set; }
    }
}
