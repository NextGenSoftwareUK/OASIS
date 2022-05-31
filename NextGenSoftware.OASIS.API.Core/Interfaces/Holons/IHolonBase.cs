using System;
using System.Collections.Generic;
using System.ComponentModel;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolonBase
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<string, string> MetaData { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
        Guid PreviousVersionId { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } 
        bool IsActive { get; set; }
        bool IsChanged { get; set; }
        bool IsNewHolon { get; set; }
        bool IsSaving { get; set; }
        IHolon Original { get; set; }
        Avatar CreatedByAvatar { get; set; }
        Guid CreatedByAvatarId { get; set; }
        DateTime CreatedDate { get; set; }
        Avatar ModifiedByAvatar { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        DateTime ModifiedDate { get; set; }
        Avatar DeletedByAvatar { get; set; }
        Guid DeletedByAvatarId { get; set; }
        DateTime DeletedDate { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }
        bool HasHolonChanged(bool checkChildren = true);
        void NotifyPropertyChanged(string propertyName);
        event PropertyChangedEventHandler PropertyChanged;
    }
}