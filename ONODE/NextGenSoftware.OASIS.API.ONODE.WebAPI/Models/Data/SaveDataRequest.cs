using System;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveDataRequest : OASISRequest
    {
        public string Value { get; set; }
        public string Key { get; set; }
        public Guid? AvatarId { get; set; }
        public string Provider { get; set; }
    }
}
