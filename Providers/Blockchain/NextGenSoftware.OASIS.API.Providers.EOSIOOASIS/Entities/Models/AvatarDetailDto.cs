using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models
{
    public class AvatarDetailDto
    {
        public int EntityId { get; set; }
        public string AvatarId { get; set; }
        public string Info { get; set; }

        public IAvatarDetail GetBaseAvatarDetail()
        {
            if (string.IsNullOrEmpty(Info))
                throw new ArgumentNullException(nameof(Info));
            return (IAvatarDetail)OasisJson.Deserialize(Info, typeof(AvatarDetail));
        }
    }
}
