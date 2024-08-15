using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcHolon : IHoloNETAuditEntryBase //: IHcObject
    {
        #region IHolonBase Properties

        Guid Id { get; set; }
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