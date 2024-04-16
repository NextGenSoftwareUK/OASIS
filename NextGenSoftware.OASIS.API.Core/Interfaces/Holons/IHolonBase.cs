using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolonBase
    {
        Avatar CreatedByAvatar { get; set; }
        Guid CreatedByAvatarId { get; set; }
        DateTime CreatedDate { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        string CustomKey { get; set; }
        Avatar DeletedByAvatar { get; set; }
        Guid DeletedByAvatarId { get; set; }
        DateTime DeletedDate { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        Guid Id { get; set; }
        EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        bool IsActive { get; set; }
        bool IsChanged { get; set; }
        bool IsNewHolon { get; set; }
        bool IsSaving { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        Avatar ModifiedByAvatar { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        DateTime ModifiedDate { get; set; }
        string Name { get; set; }
        IHolon Original { get; set; }
        Guid PreviousVersionId { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }

        event PropertyChangedEventHandler PropertyChanged;

        OASISResult<bool> Delete(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteAsync(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        bool HasHolonChanged(bool checkChildren = true);
        OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        void NotifyPropertyChanged(string propertyName);
        OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
    }
}