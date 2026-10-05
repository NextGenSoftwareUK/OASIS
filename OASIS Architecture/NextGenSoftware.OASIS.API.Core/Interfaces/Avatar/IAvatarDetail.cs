using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarDetail : IHolon
    {
        new Guid Id { get; set; }
        string Username { get; set; }
        string Email { get; set; }
        long Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        int Level { get; }
        int XP { get; set; }
        string Model3D { get; set; }
        string UmaJson { get; set; }
        string Portrait { get; set; }
        DateTime DOB { get; set; }
        string Address { get; set; }
        string Town { get; set; }
        string County { get; set; }
        string Country { get; set; }
        string Postcode { get; set; }
        string Landline { get; set; }
        string Mobile { get; set; }
        IList<IAchievement> Achievements { get; set; }
        IAvatarAttributes Attributes { get; set; }
        IAvatarAura Aura { get; set; }
        IAvatarChakras Chakras { get; set; }
        IDictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }
        IDictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        ConsoleColor FavouriteColour { get; set; }
        IList<IGeneKey> GeneKeys { get; set; }
        IList<IAvatarGift> Gifts { get; set; }
        IList<IHeartRateEntry> HeartRateData { get; set; }
        IHumanDesign HumanDesign { get; set; }
        IList<IInventoryItem> Inventory { get; set; }
        IList<IKarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        IOmiverse Omniverse { get; set; }
        IAvatarSkills Skills { get; set; }
        IList<ISpell> Spells { get; set; }
        ConsoleColor STARCLIColour { get; set; }
        IAvatarStats Stats { get; set; }
        IAvatarSuperPowers SuperPowers { get; set; }

        OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<IAvatarDetail> Save();
        Task<OASISResult<IAvatarDetail>> SaveAsync();
    }
}