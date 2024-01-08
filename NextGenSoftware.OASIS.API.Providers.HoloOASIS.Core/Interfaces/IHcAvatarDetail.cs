using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcAvatarDetail : IHoloNETAuditEntryBase // : IHcObject
    {
        #region IAvatarDetail Properties
        Guid Id { get; set; }
        string Email { get; set; }
        string Username { get; set; }
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
        //EnumValue<AvatarType> AvatarType { get; set; }
        IAvatarChakras Chakras { get; set; }
        //EnumValue<OASISType> CreatedOASISType { get; set; }
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

        #endregion

        #region IHolonBase Properties

        string Name { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        string ProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }
        public Dictionary<ProviderType, string> ProviderUsername { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<string, string> MetaData { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
        Guid PreviousVersionId { get; set; }
        bool IsActive { get; set; }
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string ModifiedBy { get; set; }
        DateTime ModifiedDate { get; set; }
        string DeletedBy { get; set; }
        DateTime DeletedDate { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }

        #endregion
    }
}