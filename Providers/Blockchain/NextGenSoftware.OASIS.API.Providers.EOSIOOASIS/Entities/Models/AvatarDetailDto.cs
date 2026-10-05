using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

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
            return JsonConvert.DeserializeObject<IAvatarDetail>(Info);
        }
    }
}