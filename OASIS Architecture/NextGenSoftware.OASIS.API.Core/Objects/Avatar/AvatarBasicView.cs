using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects.Avatar
{
    public class AvatarBasicView
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return string.Concat(FirstName, " ", LastName).Trim();
            }
        }

        public string FullNameWithTitle
        {
            get
            {
                return string.Concat(Title, " ", FirstName, " ", LastName).Trim();
            }
        }

        public string Username { get; set; }

        public EnumValue<AvatarType> AvatarType { get; set; }

        public DateTime? LastBeamedIn { get; set; }
        public DateTime? LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }

        public long Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int XP { get; set; }
        public int Level
        {
            get
            {
                return LevelManager.GetLevelFromKarma(Karma);
            }
        }
    }
}
