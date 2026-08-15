using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public partial class Holon
    {

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonsForParentAsync(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolonsAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(result.Result);
                    this.Children = result.Result.ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolonsAsync Calling HolonManager.LoadHolonsForParentAsync. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolonsAsync Calling HolonManager.LoadHolonsForParentAsync. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolonsForParent(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolons. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(result.Result);
                    this.Children = result.Result.ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolons Calling HolonManager.LoadHolonsForParent. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolons Calling HolonManager.LoadHolonsForParent. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadChildHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolonsAsync<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(Mapper.Convert(result.Result));
                    this.Children = Mapper.Convert(result.Result).ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolonsAsync<T> Calling HolonManager.LoadHolonsForParentAsync<T>. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolonsAsync<T> Calling HolonManager.LoadHolonsForParentAsync<T>. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadChildHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolonsForParent<T>(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent<T>(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolons<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(Mapper.Convert(result.Result));
                    this.Children = Mapper.Convert(result.Result).ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolons<T> Calling HolonManager.LoadHolonsForParent<T>. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolons<T> Calling HolonManager.LoadHolonsForParent<T>. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.SaveAsync<T> Calling HolonManager.SaveHolonAsync<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync<T> Calling HolonManager.SaveHolonAsync<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.SaveHolon((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = HolonManager.Instance.SaveHolon<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Save<T> Calling HolonManager.SaveHolon<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save<T> Calling HolonManager.SaveHolon<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.DeleteHolonAsync(this.Id, avatarId, softDelete, providerType);

                if (result != null && !result.IsError)
                    OnDeleted?.Invoke(this, new HolonDeletedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.DeleteAsync method calling DeleteHolonAsync attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error Details: ", result.Message));
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.DeleteAsync Calling HolonManager.DeleteHolonAsync. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> Delete(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.DeleteHolon(this.Id, avatarId, softDelete, providerType);

                if (result != null && !result.IsError)
                    OnDeleted?.Invoke(this, new HolonDeletedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.Delete method calling DeleteHolon attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error Details: ", result.Message));
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Delete Calling HolonManager.DeleteHolon. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);

            try
            {
                holon.ParentHolonId = this.Id;
                //this.Children.Add(holon);
                ((List<IHolon>)this.Children).Add(holon);

                if (saveHolon)
                {
                    result = await HolonManager.Instance.SaveHolonAsync(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolonAsync method calling SaveHolonAsync attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
                }
                else
                    OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.AddHolonAsync method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }
    }
}
