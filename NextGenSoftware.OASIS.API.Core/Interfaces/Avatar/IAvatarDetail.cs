using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarDetail : IHolon
    {
        // FORCE TO DUPLICATE THESE PROPERTIES FROM AVATAR BECAUSE MULTIPLE INHERIETANCE NOT SUPPORTED IN C#! :(
        // TODO: Be good if we can find a better work around?! ;-)
        // Using the multiple interfaces workaround seems to only work for methods, but not properties?

        new Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        /*
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        string Username { get; set; }
        string Email { get; set; }
       // string Password { get; set; } //Password only needed for SSO so is only in Avatar Object.
        //END DUPLICATION
        */

        List<Achievement> Achievements { get; set; }
        string Address { get; set; }
        AvatarAttributes Attributes { get; set; }
        AvatarAura Aura { get; set; }
        //EnumValue<AvatarType> AvatarType { get; set; }
        AvatarChakras Chakras { get; set; }
        string Country { get; set; }
        string County { get; set; }
        //EnumValue<OASISType> CreatedOASISType { get; set; }
        Dictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }
        Dictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        DateTime DOB { get; set; }
        ConsoleColor FavouriteColour { get; set; }
        List<GeneKey> GeneKeys { get; set; }
        List<AvatarGift> Gifts { get; set; }
        List<HeartRateEntry> HeartRateData { get; set; }
        HumanDesign HumanDesign { get; set; }
        string Portrait { get; set; }
        List<InventoryItem> Inventory { get; set; }
        long Karma { get; set; }
        List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        string Landline { get; set; }
        int Level { get; }
        string Mobile { get; set; }
        string Model3D { get; set; }
        IOmiverse Omniverse { get; set; }
        string Postcode { get; set; }
        AvatarSkills Skills { get; set; }
        List<Spell> Spells { get; set; }
        ConsoleColor STARCLIColour { get; set; }
        AvatarStats Stats { get; set; }
        AvatarSuperPowers SuperPowers { get; set; }
        string Town { get; set; }
        string UmaJson { get; set; }
        int XP { get; set; }

        //bool HasHolonChanged(bool checkChildren = true);
        OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<IAvatarDetail> Save();
        Task<OASISResult<IAvatarDetail>> SaveAsync();
    }
}