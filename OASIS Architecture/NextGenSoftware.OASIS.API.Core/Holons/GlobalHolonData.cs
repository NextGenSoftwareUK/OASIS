using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class GlobalHolonData
    {
        public GlobalHolonData() { }

        public event HolonLoaded OnHolonLoaded;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonsSaved OnHolonsSaved;
        public event HolonError OnError;

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = await HolonManager.Instance.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = HolonManager.Instance.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = await HolonManager.Instance.LoadHolonAsync<T>(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ZomeBase.LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = HolonManager.Instance.LoadHolon<T>(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ZomeBase.LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, bool loadChildrenFromProvider = false, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = await HolonManager.Instance.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = HolonManager.Instance.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            OASISResult<T> result = await HolonManager.Instance.LoadHolonAsync<T>(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual OASISResult<T> LoadHolon<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            OASISResult<T> result = HolonManager.Instance.LoadHolon<T>(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolons method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadAllHolonsAsync<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual OASISResult<IEnumerable<T>> LoadAllHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadAllHolons<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadHolonsForParent<T>(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadHolonsForParent<T>(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

            if (result.IsError)
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
            else
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });

            return result;
        }

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IHolon>(savingHolon) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };
            OASISResult<IHolon> result = await HolonManager.Instance.SaveHolonAsync(savingHolon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result);
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IHolon>(savingHolon) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };
            OASISResult<IHolon> result = HolonManager.Instance.SaveHolon(savingHolon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result);
            return result;
        }

        public virtual async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<T> { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };
            OASISResult<T> result = await HolonManager.Instance.SaveHolonAsync<T>(savingHolon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync<T>", ref result);
            return result;
        }

        public virtual OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<T> { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };
            OASISResult<T> result = HolonManager.Instance.SaveHolon<T>(savingHolon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolon<T>", ref result);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };

            OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.SaveHolonsAsync(savingHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, false, providerType);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result);
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };

            OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.SaveHolons(savingHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<T> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            if (savingHolons == null)
                return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IEnumerable<T>>(savingHolons) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };

            OASISResult<IEnumerable<T>> saveHolonResult = await HolonManager.Instance.SaveHolonsAsync<T>(savingHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, false, providerType);
            OASISResult<IEnumerable<IHolon>> holonsResult = OASISResultHelper.CopyResult(saveHolonResult);
            HandleSaveHolonsResult(OASISResultHelper.CopyResult(result).Result, "SaveHolonsAsync<T>", ref holonsResult);
            return result;
        }

        public virtual OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<T> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            if (savingHolons == null)
                return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            var avatarId = GetLoggedInAvatarId();
            if (avatarId == Guid.Empty)
                return new OASISResult<IEnumerable<T>>(savingHolons) { IsError = true, Message = "The avatar has not been authenticated. Make sure you have authenticated first using the avatar/authenticate endpoint." };

            OASISResult<IEnumerable<T>> saveHolonResult = HolonManager.Instance.SaveHolons(savingHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

            OASISResult<IEnumerable<IHolon>> holonsResult = OASISResultHelper.CopyResult(saveHolonResult);
            HandleSaveHolonsResult(OASISResultHelper.CopyResult(result).Result, "SaveHolonsAsync<T>", ref holonsResult);
            return result;
        }

        private static Guid GetLoggedInAvatarId()
        {
            if (AvatarManager.LoggedInAvatar == null)
                return Guid.Empty;
            var id = AvatarManager.LoggedInAvatar.Id;
            if (id != Guid.Empty)
                return id;
            return AvatarManager.LoggedInAvatar.AvatarId;
        }

        public virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentHolon, IHolon holon, List<IHolon> holons, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (holons == null)
                holons = new List<IHolon>();

            else if (holons.Any(x => x.Name == holon.Name))
            {
                result.IsError = true;
                result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
                return result;
            }

            holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

            if (holon.ParentOmniverseId == Guid.Empty)
            {
                holon.ParentOmniverseId = parentHolon.ParentOmniverseId;
                holon.ParentOmniverse = parentHolon.ParentOmniverse;
            }

            if (holon.ParentMultiverseId == Guid.Empty)
            {
                holon.ParentMultiverseId = parentHolon.ParentMultiverseId;
                holon.ParentMultiverse = parentHolon.ParentMultiverse;
            }

            if (holon.ParentUniverseId == Guid.Empty)
            {
                holon.ParentUniverseId = parentHolon.ParentUniverseId;
                holon.ParentUniverse = parentHolon.ParentUniverse;
            }

            if (holon.ParentDimensionId == Guid.Empty)
            {
                holon.ParentDimensionId = parentHolon.ParentDimensionId;
                holon.ParentDimension = parentHolon.ParentDimension;
            }

            if (holon.ParentGalaxyClusterId == Guid.Empty)
            {
                holon.ParentGalaxyClusterId = parentHolon.ParentGalaxyClusterId;
                holon.ParentGalaxyCluster = parentHolon.ParentGalaxyCluster;
            }

            if (holon.ParentGalaxyId == Guid.Empty)
            {
                holon.ParentGalaxyId = parentHolon.ParentGalaxyId;
                holon.ParentGalaxy = parentHolon.ParentGalaxy;
            }

            if (holon.ParentSolarSystemId == Guid.Empty)
            {
                holon.ParentSolarSystemId = parentHolon.ParentSolarSystemId;
                holon.ParentSolarSystem = parentHolon.ParentSolarSystem;
            }

            if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGreatGrandSuperStarId = parentHolon.ParentGreatGrandSuperStarId;
                holon.ParentGreatGrandSuperStar = parentHolon.ParentGreatGrandSuperStar;
            }

            if (holon.ParentGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGrandSuperStarId = parentHolon.ParentGrandSuperStarId;
                holon.ParentGrandSuperStar = parentHolon.ParentGrandSuperStar;
            }

            if (holon.ParentSuperStarId == Guid.Empty)
            {
                holon.ParentSuperStarId = parentHolon.ParentSuperStarId;
                holon.ParentSuperStar = parentHolon.ParentSuperStar;
            }

            if (holon.ParentStarId == Guid.Empty)
            {
                holon.ParentStarId = parentHolon.ParentStarId;
                holon.ParentStar = parentHolon.ParentStar;
            }

            if (holon.ParentPlanetId == Guid.Empty)
            {
                holon.ParentPlanetId = parentHolon.ParentPlanetId;
                holon.ParentPlanet = parentHolon.ParentPlanet;
            }

            if (holon.ParentMoonId == Guid.Empty)
            {
                holon.ParentMoonId = parentHolon.ParentMoonId;
                holon.ParentMoon = parentHolon.ParentMoon;
            }

            if (holon.ParentCelestialSpaceId == Guid.Empty)
            {
                holon.ParentCelestialSpaceId = parentHolon.ParentCelestialSpaceId;
                holon.ParentCelestialSpace = parentHolon.ParentCelestialSpace;
            }

            if (holon.ParentCelestialBodyId == Guid.Empty)
            {
                holon.ParentCelestialBodyId = parentHolon.ParentCelestialBodyId;
                holon.ParentCelestialBody = parentHolon.ParentCelestialBody;
            }

            if (holon.ParentZomeId == Guid.Empty)
            {
                holon.ParentZomeId = parentHolon.ParentZomeId;
                holon.ParentZome = parentHolon.ParentZome;
            }

            if (holon.ParentHolonId == Guid.Empty)
            {
                holon.ParentHolonId = parentHolon.ParentHolonId;
                holon.ParentHolon = parentHolon.ParentHolon;
            }

            switch (parentHolon.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    holon.ParentGreatGrandSuperStarId = parentHolon.Id;
                    holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.GrandSuperStar:
                    holon.ParentGrandSuperStarId = parentHolon.Id;
                    holon.ParentGrandSuperStar = (IGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.SuperStar:
                    holon.ParentSuperStarId = parentHolon.Id;
                    holon.ParentSuperStar = (ISuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Multiverse:
                    holon.ParentMultiverseId = parentHolon.Id;
                    holon.ParentMultiverse = (IMultiverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Universe:
                    holon.ParentUniverseId = parentHolon.Id;
                    holon.ParentUniverse = (IUniverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Dimension:
                    holon.ParentDimensionId = parentHolon.Id;
                    holon.ParentDimension = (IDimension)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.GalaxyCluster:
                    holon.ParentGalaxyClusterId = parentHolon.Id;
                    holon.ParentGalaxyCluster = (IGalaxyCluster)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Galaxy:
                    holon.ParentGalaxyId = parentHolon.Id;
                    holon.ParentGalaxy = (IGalaxy)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.SolarSystem:
                    holon.ParentSolarSystemId = parentHolon.Id;
                    holon.ParentSolarSystem = (ISolarSystem)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Star:
                    holon.ParentStarId = parentHolon.Id;
                    holon.ParentStar = (IStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Planet:
                    holon.ParentPlanetId = parentHolon.Id;
                    holon.ParentPlanet = (IPlanet)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Moon:
                    holon.ParentMoonId = parentHolon.Id;
                    holon.ParentMoon = (IMoon)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Zome:
                    holon.ParentZomeId = parentHolon.Id;
                    holon.ParentZome = (IZome)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;

                case HolonType.Holon:
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;
            }

            holons.Add(holon);

            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

            if (saveHolon)
            {
                result = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
                result.IsSaved = true;
            }
            else
            {
                result.Message = "Holon was not saved due to saveHolon being set to false.";
                result.IsSaved = false;
                result.Result = holon;
            }

            return result;
        }

        private void HandleSaveHolonResult<T>(IHolon savingHolon, string callingMethodName, ref OASISResult<T> result) where T : IHolon, new()
        {
            if (!result.IsError && result.Result != null)
            {
                result.Result = (T)savingHolon;
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
            }
            else
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private void HandleSaveHolonResult(IHolon savingHolon, string callingMethodName, ref OASISResult<IHolon> result)
        {
            if (!result.IsError && result.Result != null)
            {
                savingHolon = Mapper.MapBaseHolonProperties(result.Result, savingHolon);
                result.Result = RestoreCelestialBodyCores(savingHolon);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            }
            else
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private IHolon RestoreCelestialBodyCores(IHolon savingHolon)
        {
            //First restore the parent.
            if (HolonManager.Instance.CelestialBodyCoreCache.ContainsKey(savingHolon.Id))
            {
                ICelestialBody celestialBody = savingHolon as ICelestialBody;

                if (celestialBody != null)
                {
                    celestialBody.CelestialBodyCore = HolonManager.Instance.CelestialBodyCoreCache[savingHolon.Id];
                    savingHolon = celestialBody;
                    HolonManager.Instance.CelestialBodyCoreCache.Remove(savingHolon.Id);
                }
            }

            //Then restore the children.
            for (int i = 0; i < savingHolon.Children.Count; i++)
            {
                if (HolonManager.Instance.CelestialBodyCoreCache.ContainsKey(savingHolon.Children[i].Id))
                {
                    ICelestialBody celestialBody = savingHolon.Children[i] as ICelestialBody;

                    if (celestialBody != null)
                    {
                        celestialBody.CelestialBodyCore = HolonManager.Instance.CelestialBodyCoreCache[savingHolon.Children[i].Id];
                        HolonManager.Instance.CelestialBodyCoreCache.Remove(savingHolon.Children[i].Id);
                    }
                }

                //savingHolon.Children[i] = RestoreCelestialBodyCores(savingHolon.Children[i]);
                RestoreCelestialBodyCores(savingHolon.Children[i]);
            }

            return savingHolon;
        }

        private void HandleSaveHolonsResult(IEnumerable<IHolon> savingHolons, string callingMethodName, ref OASISResult<IEnumerable<IHolon>> result)
        {
            if (!result.IsError && result.Result != null)
            {
                result.Result = savingHolons;
                OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            }
            else
                OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), Exception = result.Exception });
        }
    }
}
