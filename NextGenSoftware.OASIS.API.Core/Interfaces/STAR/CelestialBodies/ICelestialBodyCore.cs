using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodyCore : IZome
    {
        IEnumerable<IHolon> Holons { get; }
        List<IZome> Zomes { get; set; }

        event EventDelegates.HolonError OnHolonError;
        event EventDelegates.HolonLoaded OnHolonLoaded;
        event EventDelegates.HolonSaved OnHolonSaved;
        event EventDelegates.HolonsError OnHolonsError;
        event EventDelegates.HolonsLoaded OnHolonsLoaded;
        event EventDelegates.HolonsSaved OnHolonsSaved;
        event EventDelegates.ZomeAdded OnZomeAdded;
        event EventDelegates.ZomeError OnZomeError;
        event EventDelegates.ZomeLoaded OnZomeLoaded;
        event EventDelegates.ZomeRemoved OnZomeRemoved;
        event EventDelegates.ZomeSaved OnZomeSaved;
        event EventDelegates.ZomesError OnZomesError;
        event EventDelegates.ZomesLoaded OnZomesLoaded;
        event EventDelegates.ZomesSaved OnZomesSaved;

        OASISResult<IZome> AddZome(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> AddZome<T>(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IZome>> AddZomeAsync(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> AddZomeAsync<T>(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IZome> RemoveZome(IZome zome, ProviderType providerType = ProviderType.Default);
        OASISResult<T> RemoveZome<T>(IZome zome, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IZome>> RemoveZomeAsync(IZome zome, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> RemoveZomeAsync<T>(IZome zome, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = true, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new();
    }
}