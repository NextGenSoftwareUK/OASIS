using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public partial class Holon
    {
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET =
            "Both Id and ProviderUniqueStorageKey are null, one of these need to be set before calling this method.";

        // ── Load ─────────────────────────────────────────────────────────────

        public async Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, await HolonManager.Instance.LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));
                else if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = await HolonManager.Instance.LoadHolonAsync(keyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadAsync. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadAsync calling HolonManager.LoadHolonAsync. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadAsync. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, await HolonManager.Instance.LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));
                else if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonAsync<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = await HolonManager.Instance.LoadHolonAsync<T>(keyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadAsync<T>. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadAsync<T> calling HolonManager.LoadHolonAsync<T>. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadAsync<T>. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, HolonManager.Instance.LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));
                else if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolon(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = HolonManager.Instance.LoadHolon(keyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Load. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Load calling HolonManager.LoadHolon. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.Load. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, HolonManager.Instance.LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));
                else if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolon<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = HolonManager.Instance.LoadHolon<T>(keyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Load<T>. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Load<T> calling HolonManager.LoadHolon<T>. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.Load<T>. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        // ── LoadChildHolons ───────────────────────────────────────────────────

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonsForParentAsync(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync(keyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolonsAsync. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { this.Children = result.Result.ToList(); OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolonsAsync calling HolonManager.LoadHolonsForParentAsync. Reason: {result.Message}"); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadChildHolonsAsync. Reason: {ex}", ex); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
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
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent(keyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolons. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { this.Children = result.Result.ToList(); OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolons calling HolonManager.LoadHolonsForParent. Reason: {result.Message}"); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadChildHolons. Reason: {ex}", ex); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
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
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(keyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolonsAsync<T>. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { this.Children = Mapper.Convert(result.Result).ToList(); OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolonsAsync<T> calling HolonManager.LoadHolonsForParentAsync<T>. Reason: {result.Message}"); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadChildHolonsAsync<T>. Reason: {ex}", ex); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
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
                    OASISResult<string> keyResult = GetCurrentProviderKey(providerType);
                    if (!keyResult.IsError && !string.IsNullOrEmpty(keyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent<T>(keyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolons<T>. Reason: {keyResult.Message}", keyResult.DetailedMessage);
                }
                else { result.IsError = true; result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET; }

                if (result != null && !result.IsError && result.Result != null)
                { this.Children = Mapper.Convert(result.Result).ToList(); OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.LoadChildHolons<T> calling HolonManager.LoadHolonsForParent<T>. Reason: {result.Message}"); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.LoadChildHolons<T>. Reason: {ex}", ex); OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        // ── Save ─────────────────────────────────────────────────────────────

        public async Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                result = await HolonManager.Instance.SaveHolonAsync((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.SaveAsync calling HolonManager.SaveHolonAsync. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.SaveAsync. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            try
            {
                result = await HolonManager.Instance.SaveHolonAsync<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.SaveAsync<T> calling HolonManager.SaveHolonAsync<T>. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.SaveAsync<T>. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                result = HolonManager.Instance.SaveHolon((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Save calling HolonManager.SaveHolon. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.Save. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            try
            {
                result = HolonManager.Instance.SaveHolon<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.Id : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                if (result != null && !result.IsError && result.Result != null)
                { HolonMapper.SetProperties(this, result.Result); OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) }); }
                else
                { OASISErrorHandling.HandleError(ref result, $"Error in HolonBase.Save<T> calling HolonManager.SaveHolon<T>. Reason: {result.Message}"); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.Save<T>. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                result = await HolonManager.Instance.DeleteHolonAsync(this.Id, avatarId, softDelete, providerType);
                if (result != null && !result.IsError)
                    OnDeleted?.Invoke(this, new HolonDeletedEventArgs() { Result = result });
                else
                { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.DeleteAsync calling DeleteHolonAsync for holon ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.DeleteAsync. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
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
                { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.Delete calling DeleteHolon for holon ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, $"Unknown error in HolonBase.Delete. Reason: {ex}", ex); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        // ── AddHolon / RemoveHolon ────────────────────────────────────────────

        public async Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);
            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                if (saveHolon)
                {
                    result = await HolonManager.Instance.SaveHolonAsync(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolonAsync calling SaveHolonAsync for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
                }
                else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.AddHolonAsync for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<IHolon> AddHolon(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);
            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                if (saveHolon)
                {
                    result = HolonManager.Instance.SaveHolon(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolon calling SaveHolon for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
                }
                else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.AddHolon for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public async Task<OASISResult<T>> AddHolonAsync<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>(holon);
            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                if (saveHolon)
                {
                    result = await HolonManager.Instance.SaveHolonAsync<T>(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolonAsync<T> calling SaveHolonAsync<T> for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.AddHolonAsync<T> for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<T> AddHolon<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>(holon);
            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                if (saveHolon)
                {
                    result = HolonManager.Instance.SaveHolon<T>(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolon<T> calling SaveHolon<T> for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.AddHolon<T> for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex }); }
            return result;
        }

        public async Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);
            try
            {
                holon.ParentHolonId = Guid.Empty;
                ((List<IHolon>)this.Children).Remove(holon);
                if (deleteHolon)
                {
                    result = await HolonManager.Instance.DeleteHolonAsync(holon.Id, avatarId, softDelete, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.RemoveHolonAsync calling DeleteHolonAsync for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
                }
                else OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.RemoveHolonAsync for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }

        public OASISResult<IHolon> RemoveHolon(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);
            try
            {
                holon.ParentHolonId = Guid.Empty;
                ((List<IHolon>)this.Children).Remove(holon);
                if (deleteHolon)
                {
                    result = HolonManager.Instance.DeleteHolon(holon.Id, avatarId, softDelete, providerType);
                    if (result.IsError)
                    { OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.RemoveHolon calling DeleteHolon for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", result.Message)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception }); }
                    else OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
                }
                else OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            { OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error in HolonBase.RemoveHolon for holon ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error: ", ex)); OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex }); }
            return result;
        }
    }
}
