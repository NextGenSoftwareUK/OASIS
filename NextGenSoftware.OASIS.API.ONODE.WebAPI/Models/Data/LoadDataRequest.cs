using System;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadDataRequest : OASISRequest
    {
        public string Key { get; set; }
        public string Provider { get; set; }
        public Guid? AvatarId { get; set; }
    }
}
