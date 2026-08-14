using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBodyCore<T>
    {
        private List<IHolon> _allchildren = new List<IHolon>();

        //public event ZomeLoaded OnZomeLoaded;
        //public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        //public event CelestialBodyLoaded OnCelestialBodyLoaded;
        //public event CelestialBodySaved OnCelestialBodySaved;
        //public event CelestialBodyError OnCelestialBodyError;
        //public event HolonLoaded OnHolonLoaded;
        //public event HolonSaved OnHolonSaved;
        //public event HolonError OnHolonError;
        //public event HolonsLoaded OnHolonsLoaded;
        //public event HolonsSaved OnHolonsSaved;
        //public event HolonsError OnHolonsError;

        //TODO: Need to make this like CelestialSpace so Zomes and Holons are synced with the Children property and then ONLY the children are saved in HolonManager automatically, no need to save/load holons/zomes seperately as we do now.
        public List<IZome> Zomes { get; set; } = new List<IZome>();

        //public IEnumerable<IHolon> Holons
        //{
        //    get
        //    {
        //        if (Zomes != null)
        //        {
        //            List<IHolon> holons = new List<IHolon>();

        //            foreach (IZome zome in Zomes)
        //                holons.Add((IHolon)zome);

        //            //Now we need to add the base holons that are linked directly to the celestialbody.
        //            //holons.AddRange(base.Holons);
        //            holons.AddRange(base.Children);
        //            return holons;
        //        }

        //        return null;
        //    }
        //}

        public override ReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                _allchildren.Clear();
                _allchildren.AddRange(Children);
                _allchildren.AddRange(Zomes);

                return _allchildren.AsReadOnly();
            }
        }

        public CelestialBodyCore(Guid id) : base(id)
        {

        }

        public CelestialBodyCore(string providerKey, ProviderType providerType = ProviderType.Default) : base(providerKey, providerType)
        {

        }

        public CelestialBodyCore() : base()
        {
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            string errorMessage = "Error in CelestialBodyCore.LoadZomesAsync calling base.LoadHolonsForParentAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<IHolon>> holonResult = await GlobalHolonData.LoadHolonsForParentAsync(Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                //OASISResultHelper.CopyResult(holonResult, result);
                //OASISResultHelper.CopyResultToIZome(holonResult, result);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(result, holonResult);

                if (holonResult.Result != null && !holonResult.IsError)
                {
                    List<IZome> zomes = new List<IZome>();

                    foreach (IHolon holon in holonResult.Result)
                        zomes.Add((IZome)Mapper.MapBaseHolonProperties(holon, new Zome()));

                    result.Result = zomes;
                    this.Zomes = (List<IZome>)result.Result;
                    OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            string errorMessage = "Error in CelestialBodyCore.LoadZomes calling base.LoadHolonsForParent. Reason:";

            try
            {
                OASISResult<IEnumerable<IHolon>> holonResult = GlobalHolonData.LoadHolonsForParent(Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                OASISResultHelper.CopyResult(holonResult, result);

                if (holonResult.Result != null && !holonResult.IsError)
                {
                    this.Zomes = (List<IZome>)result.Result;
                    OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = "Error in CelestialBodyCore.LoadZomesAsync<T> calling base.LoadHolonsForParentAsync<T>. Reason:";

            try
            {
                result = await GlobalHolonData.LoadHolonsForParentAsync<T>(Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                if (result.Result != null && !result.IsError)
                {
                    //TODO: Do we want to empty the collection before adding the loaded zomes to it?
                    foreach (T zome in result.Result)
                        this.Zomes.Add(zome);

                    OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = "Error in CelestialBodyCore.LoadZomes<T> calling base.LoadHolonsForParent<T>. Reason:";

            try
            {
                result = GlobalHolonData.LoadHolonsForParent<T>(Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                if (result.Result != null && !result.IsError)
                {
                    //TODO: Do we want to empty the collection before adding the loaded zomes to it?
                    foreach (T zome in result.Result)
                        this.Zomes.Add(zome);

                    OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        //TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        //public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    string errorMessage = "Error in CelestialBodyCore.SaveZomes calling base.SaveHolons. Reason:";

        //    if (this.Zomes != null)
        //    {
        //        OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes, true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //        result = OASISResultHelper.CopyResultToIZome(holonsResult);

        //        //TODO: May be useful to use this logic in HolonManager?
        //        //foreach (IZome zome in this.Zomes)
        //        //{
        //        //    if (zome.HasHolonChanged())
        //        //    {
        //        //        OASISResult<IZome> holonResult = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

        //        //        if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
        //        //        {
        //        //            result.SavedCount++;
        //        //            savedZomes.Add(holonResult.Result);
        //        //        }
        //        //        else
        //        //        {
        //        //            result.ErrorCount++;
        //        //            OASISErrorHandling.HandleWarning(ref result, $"There was an error in the CelestialBodyCore.SaveZomesAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {holonResult.Message}", true, false, false, true, false);

        //        //            if (!continueOnError)
        //        //                break;
        //        //        }
        //        //    }
        //        //}
        //    }

        //    //TODO: May be useful to use this logic in HolonManager?
        //    //if (result.ErrorCount > 0)
        //    //{
        //    //    string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomesAsync method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //    //    if (result.SavedCount == 0)
        //    //        OASISErrorHandling.HandleError(ref result, message);
        //    //    else
        //    //    {
        //    //        OASISErrorHandling.HandleWarning(ref result, message);
        //    //        result.IsSaved = true;
        //    //    }

        //    //    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    //}
        //    //else
        //    //    result.IsSaved = true;

        //    //if (result.SavedCount > 0)
        //    //{ 
        //    //    result.Result = savedZomes;
        //    //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    //}

        //    if (result != null && !result.IsError && result.Result != null)
        //        OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    else
        //    {
        //        OASISErrorHandling.HandleError(ref result, string.Concat("Error in CelestialBodyCore.SaveZomesAsync method caling base.SaveHolonsAsync. Reason: ", result.Message));
        //        OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            string errorMessage = "Error in CelestialBodyCore.SaveZomesAsync calling base.SaveHolonsAsync. Reason:";

            try
            {
                if (this.Zomes != null)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = await GlobalHolonData.SaveHolonsAsync(this.Zomes, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    result = OASISResultHelper.CopyResultToIZome(holonsResult);
                }

                if (result != null && !result.IsError && result.Result != null)
                    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            string errorMessage = "Error in CelestialBodyCore.SaveZomes calling base.SaveHolons. Reason:";

            try
            {
                if (this.Zomes != null)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = GlobalHolonData.SaveHolons(this.Zomes, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                    result = OASISResultHelper.CopyResultToIZome(holonsResult);
                }

                if (result != null && !result.IsError && result.Result != null)
                    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = "Error in CelestialBodyCore.SaveZomesAsync<T> calling base.SaveHolonsAsync<T>. Reason:";

            try
            {
                if (this.Zomes != null)
                    result = await GlobalHolonData.SaveHolonsAsync<T>((IEnumerable<T>)this.Zomes, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }
    }
}
