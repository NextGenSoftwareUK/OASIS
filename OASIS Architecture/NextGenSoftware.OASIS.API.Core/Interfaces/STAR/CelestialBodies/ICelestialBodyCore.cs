using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodyCore : IZome
    {
        //IEnumerable<IHolon> Holons { get; }
        List<IZome> Zomes { get; set; }

        //public event ZomeLoaded OnZomeLoaded;
        //public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        //public event HolonLoaded OnHolonLoaded;
        //public event HolonSaved OnHolonSaved;
        //public event HolonError OnHolonError;
        //public event HolonsLoaded OnHolonsLoaded;
        //public event HolonsSaved OnHolonsSaved;
        //public event HolonsError OnHolonsError;

        OASISResult<IZome> AddZome(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> AddZome<T>(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IZome>> AddZomeAsync(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> AddZomeAsync<T>(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IZome> RemoveZome(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T> RemoveZome<T>(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IZome>> RemoveZomeAsync(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> RemoveZomeAsync<T>(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = true, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
    }
}