using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarDetail : IHolon
    {
        new Guid Id { get; set; }
        string Username { get; set; }
        string Email { get; set; }
        int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        int Level { get; set; }
        int XP { get; set; }
        string Model3D { get; set; }
        string UmaJson { get; set; }
        string Portrait { get; set; }
        string DOB { get; set; }
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
        public IDictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        public ConsoleColor FavouriteColour { get; set; }
        public IList<IGeneKey> GeneKeys { get; set; }
        public IList<IAvatarGift> Gifts { get; set; }
        public IList<HeartRateEntry> HeartRateData { get; set; }
        public IHumanDesign HumanDesign { get; set; }
        public IList<IInventoryItem> Inventory { get; set; }
        public IList<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public IOmiverse Omniverse { get; set; }
        public IAvatarSkills Skills { get; set; }
        public IList<ISpell> Spells { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        public IAvatarStats Stats { get; set; }
        public IAvatarSuperPowers SuperPowers { get; set; }

        OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        OASISResult<IAvatarDetail> Save();
        Task<OASISResult<IAvatarDetail>> SaveAsync();
    }
}