using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolon : ISemanticHolon
    {
        EnumValue<OASISType> CreatedOASISType { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        GlobalHolonData GlobalHolonData { get; set; }
        EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        bool IsChanged { get; set; }
        bool IsNewHolon { get; set; }
        bool IsSaving { get; set; }
        IHolon Original { get; set; }
        Guid PreviousVersionId { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }


        event EventDelegates.HolonsLoaded OnChildrenLoaded;
        event EventDelegates.HolonsError OnChildrenLoadError;
        event EventDelegates.HolonDeleted OnDeleted;
        event EventDelegates.HolonError OnError;
        event EventDelegates.HolonAdded OnHolonAdded;
        event EventDelegates.HolonRemoved OnHolonRemoved;
        event EventDelegates.Initialized OnInitialized;
        event EventDelegates.HolonLoaded OnLoaded;
        event EventDelegates.HolonSaved OnSaved;
        event PropertyChangedEventHandler PropertyChanged;

        OASISResult<IHolon> AddHolon(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> AddHolon<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> AddHolonAsync<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        //OASISResult<IHolon> Delete(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IHolon>> DeleteAsync(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IHolon> Delete(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        bool HasHolonChanged(bool checkChildren = true);
        OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true);
        OASISResult<IEnumerable<T>> LoadChildHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new();
        Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true);
        Task<OASISResult<IEnumerable<T>>> LoadChildHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new();
        void NotifyPropertyChanged(string propertyName);
        //OASISResult<IHolon> RemoveHolon(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IHolon> RemoveHolon(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);

        OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
    }
}