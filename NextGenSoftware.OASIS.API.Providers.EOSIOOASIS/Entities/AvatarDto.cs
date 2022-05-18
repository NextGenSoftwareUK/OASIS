using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities
{
    public sealed class AvatarDto
    {
        public string AvatarId { get; set; }
        public int EntityId { get; set; }
        public string Info { get; set; }
        public bool IsDeleted { get; set; } = false;

        public int GetEntityId()
        {
            if (string.IsNullOrEmpty(AvatarId))
                throw new ArgumentNullException(nameof(AvatarId));
            return HashUtility.GetNumericHash(AvatarId);
        }

        public IAvatar GetBaseAvatar()
        {
            if (string.IsNullOrEmpty(Info))
                throw new ArgumentNullException(nameof(Info));
            return JsonConvert.DeserializeObject<IAvatar>(Info);
        }
    }
}