using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class AddRemoveKarmaToAvatarRequest
    {
       // public Guid AvatarId { get; set; }
        //public KarmaTypePositive KarmaType { get; set; }
        //public KarmaSourceType karmaSourceType { get; set; }

        //Optional, users can also call the alternative REST endpoints that pass in the AvatarId instead.
      //  public IAvatar Avatar { get; set; }

        public string KarmaType { get; set; }
        public string karmaSourceType { get; set; }

        public string KaramSourceTitle { get; set; }

        public string KarmaSourceDesc { get; set; }
    }
}