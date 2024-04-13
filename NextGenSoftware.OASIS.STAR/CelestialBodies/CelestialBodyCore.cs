using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBodyCore<T> : ZomeBase, ICelestialBodyCore where T : ICelestialBody, new()
    {
        //public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        ////  public event HolonsLoaded OnHolonsLoaded;
        //public event Events.HolonsLoaded OnHolonsLoaded;

        //public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        ////public event ZomesLoaded OnZomesLoaded;
        //public event Events.ZomesLoaded OnZomesLoaded;

        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonError OnHolonError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;

        public List<IZome> Zomes { get; set; } = new List<IZome>();

        public new IEnumerable<IHolon> Holons
        {
            get
            {
                if (Zomes != null)
                {
                    List<IHolon> holons = new List<IHolon>();

                    foreach (IZome zome in Zomes)
                        holons.Add(zome);

                    //Now we need to add the base holons that are linked directly to the celestialbody.
                    holons.AddRange(base.Holons);
                    return holons;
                }

                return null;
            }
        }

        public CelestialBodyCore(Guid id) : base(id)
        {

        }

        public CelestialBodyCore(string providerKey, ProviderType providerType = ProviderType.Default) : base(providerKey, providerType)
        {

        }

        //public CelestialBodyCore(Dictionary<ProviderType, string> providerKeys) : base(providerKeys)
        //{

        //}

        public CelestialBodyCore() : base()
        {
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await base.LoadHolonsForParentAsync(AvatarManager.LoggedInAvatar.Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, version);
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, result);
            //OASISResultHelperForHolons<IHolon, IZome>.CopyResult(holonResult, result); //TODO: Ideally be good to get this version working then will not need seperate call to MapBaseHolonProperties below because this version calls it internally... ;-) 

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result); //TODO: Not sure if this would work? DOUBLE CHECK ASAP! :)
                this.Zomes = result.Result.ToList();
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>> holonResult = base.LoadHolonsForParent(AvatarManager.LoggedInAvatar.Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, version);
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, result);
            //OASISResultHelperForHolons<IHolon, IZome>.CopyResult(holonResult, result); //TODO: Ideally be good to get this version working then will not need seperate call to MapBaseHolonProperties below because this version calls it internally... ;-) 

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result); //TODO: Not sure if this would work? DOUBLE CHECK ASAP! :)
                this.Zomes = (List<IZome>)result.Result;
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await base.LoadHolonsForParentAsync(AvatarManager.LoggedInAvatar.Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, version);
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<T>>.CopyResult(holonResult, result);
            //OASISResultHelperForHolons<IHolon, IZome>.CopyResult(holonResult, result); //TODO: Ideally be good to get this version working then will not need seperate call to MapBaseHolonProperties below because this version calls it internally... ;-) 

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result); //TODO: Not sure if this would work? DOUBLE CHECK ASAP! :)

                foreach (T zome in result.Result)
                    this.Zomes.Add(zome);

                //this.Zomes = result.Result.ToList();
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelperForHolons.CopyResultToIZome(result) });

            return result;

            //return OASISResultHelperForHolons<IZome, T>.CopyResult(await LoadZomesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonResult = base.LoadHolonsForParent(AvatarManager.LoggedInAvatar.Id, HolonType.Zome, loadChildren, recursive, maxChildDepth, continueOnError, version);
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<T>>.CopyResult(holonResult, result);
            //OASISResultHelperForHolons<IHolon, IZome>.CopyResult(holonResult, result); //TODO: Ideally be good to get this version working then will not need seperate call to MapBaseHolonProperties below because this version calls it internally... ;-) 

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result); //TODO: Not sure if this would work? DOUBLE CHECK ASAP! :)

                foreach (T zome in result.Result)
                    this.Zomes.Add(zome);

                //this.Zomes = result.Result.ToList();
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelperForHolons.CopyResultToIZome(result) });

            return result;

            //return OASISResultHelperForHolons<IZome, T>.CopyResult(LoadZomes(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        //TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            //OASISResult<IZome> zomeResult = new OASISResult<IZome>();
            List<IZome> savedZomes = new List<IZome>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        //zomeResult = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                        OASISResult<IHolon> holonResult = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                        {
                            result.SavedCount++;
                            savedZomes.Add((IZome)holonResult.Result);
                        }
                        else
                        {
                            result.ErrorCount++;
                            OASISErrorHandling.HandleWarning(ref result, $"There was an error in the CelestialBodyCore.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {holonResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomes method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsSaved = true;

            result.Result = savedZomes;
            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IHolon> zomeResult = new OASISResult<IHolon>();
            List<IZome> savedZomes = new List<IZome>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        zomeResult = zome.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
                        {
                            result.SavedCount++;
                            savedZomes.Add((IZome)zomeResult.Result);
                        }
                        else
                        {
                            result.ErrorCount++;
                            OASISErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBodyCore.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomes method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsSaved = true;

            result.Result = savedZomes;
            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<T> zomeResult = new OASISResult<T>();
            List<T> savedZomes = new List<T>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        zomeResult = await zome.SaveAsync<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
                        {
                            result.SavedCount++;
                            savedZomes.Add(zomeResult.Result);
                        }
                        else
                        {
                            result.ErrorCount++;
                            OASISErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBodyCore.SaveZomesAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomesAsync method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            }
            else
                result.IsSaved = true;

            result.Result = savedZomes;
            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;

            //return OASISResultHelperForHolons<IZome, T>.CopyResult(await SaveZomesAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<T> zomeResult = new OASISResult<T>();
            List<T> savedZomes = new List<T>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        zomeResult = zome.Save<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
                        {
                            result.SavedCount++;
                            savedZomes.Add(zomeResult.Result);
                        }
                        else
                        {
                            result.ErrorCount++;
                            OASISErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBodyCore.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomes method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            }
            else
                result.IsSaved = true;

            result.Result = savedZomes;
            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;

            //return OASISResultHelperForHolons<IZome, T>.CopyResult(SaveZomes(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public async Task<OASISResult<IZome>> AddZomeAsync(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            SetParentsAndAddZome(zome);
            OASISResult<IHolon> holonResult = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

            if (holonResult.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in CelestialBodyCore.AddZomeAsync method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", holonResult.Message), Exception = holonResult.Exception });

            result = OASISResultHelperForHolons.CopyResultToIZome(holonResult);
            OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IZome> AddZome(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            SetParentsAndAddZome(zome);
            OASISResult<IHolon> holonResult = zome.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

            if (holonResult.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in CelestialBodyCore.AddZome method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", holonResult.Message), Exception = holonResult.Exception });

            result = OASISResultHelperForHolons.CopyResultToIZome(holonResult);
            OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<T>> AddZomeAsync<T>(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            SetParentsAndAddZome(zome);
            result = await zome.SaveAsync<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.AddZomeAsync<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;
        }

        public OASISResult<T> AddZome<T>(IZome zome, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            SetParentsAndAddZome(zome);
            result = zome.Save<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.AddZome<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;
        }

        public async Task<OASISResult<IZome>> RemoveZomeAsync(IZome zome, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            BlankParentsAndRemoveZome(zome);
            OASISResult<IHolon> holonResult = await zome.SaveAsync(true, true, 0, true, false, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZomeAsync method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            result = OASISResultHelperForHolons.CopyResultToIZome(holonResult);
            OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IZome> RemoveZome(IZome zome, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            BlankParentsAndRemoveZome(zome);
            OASISResult<IHolon> holonResult = zome.Save(true, true, 0, true, false, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZome method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            result = OASISResultHelperForHolons.CopyResultToIZome(holonResult);
            OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<T>> RemoveZomeAsync<T>(IZome zome, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            BlankParentsAndRemoveZome(zome);
            result = await zome.SaveAsync<T>(true, true, 0, true, false, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZomeAsync<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;
        }

        public OASISResult<T> RemoveZome<T>(IZome zome, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            BlankParentsAndRemoveZome(zome);
            result = zome.Save<T>(true, true, 0, true, false, providerType);

            if (result != null && result.IsError)
            {
                string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZome<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Error Details: ", result.Message);
                OASISErrorHandling.HandleError(ref result, errorMessage);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage, Exception = result.Exception });
            }

            OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelperForHolons.CopyResultToIZome(result) });
            return result;
        }

        //TODO: ALL THESE METHODS ARE NOW REDUNDANT BECAUSE LOAD/SAVE METHODS ON HOLONBASE WILL LOAD/SAVE EXCEPT MAYBE WE DO NEED TO CAST THE RESULT TO ICELESTIALBODY AND ALSO RAISE DIFFERENT EVENTS?
        //      SAME WITH ZOMEBASE...

        ////TODO: Why are we passing in savingHolon here? Shouldnt this just be saving the current celestialbody/holon?
        //public async Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return await base.SaveHolonAsync(savingHolon, false, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
        //}

        //public OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        //{
        //    //TODO: Not sure if this is a good way of doing this?
        //    return SaveCelestialBodyAsync(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType).Result;
        //}

        //public async Task<OASISResult<T>> SaveCelestialBodyAsync<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return await base.SaveHolonAsync<T>(savingHolon, false, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
        //}

        //public OASISResult<T> SaveCelestialBody<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    //TODO: Not sure if this is a good way of doing this?
        //    return SaveCelestialBodyAsync<T>(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType).Result;
        //}

        //public async Task<OASISResult<T>> LoadCelestialBodyAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return OASISResultHelperForHolons<IHolon, T>.CopyResult(await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        //}

        //public OASISResult<T> LoadCelestialBody<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return OASISResultHelperForHolons<IHolon, T>.CopyResult(base.LoadHolon(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        //}

        //public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);

        //    return result;
        //}

        //public OASISResult<ICelestialBody> LoadCelestialBody(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = base.LoadHolon(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
        //    result.Result = (ICelestialBody)holonResult.Result;

        //    return result;
        //}

        //public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result, (CelestialBody)result.Result);

        //    return result;
        //}


        //MOVE TO HolonManager because this is a gernic method and does not directly apply to CelestialBodyCore.
        //protected virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentCelestialBody, IHolon holon, List<IHolon> holons, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    if (holons == null)
        //        holons = new List<IHolon>();

        //    else if (holons.Any(x => x.Name == holon.Name))
        //    {
        //        result.IsError = true;
        //        result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
        //        return result;
        //    }

        //    holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

        //    if (holon.ParentOmniverseId == Guid.Empty)
        //    {
        //        holon.ParentOmniverseId = parentCelestialBody.ParentOmniverseId;
        //        holon.ParentOmniverse = parentCelestialBody.ParentOmniverse;
        //    }

        //    if (holon.ParentMultiverseId == Guid.Empty)
        //    {
        //        holon.ParentMultiverseId = parentCelestialBody.ParentMultiverseId;
        //        holon.ParentMultiverse = parentCelestialBody.ParentMultiverse;
        //    }

        //    if (holon.ParentUniverseId == Guid.Empty)
        //    {
        //        holon.ParentUniverseId = parentCelestialBody.ParentUniverseId;
        //        holon.ParentUniverse = parentCelestialBody.ParentUniverse;
        //    }

        //    if (holon.ParentDimensionId == Guid.Empty)
        //    {
        //        holon.ParentDimensionId = parentCelestialBody.ParentDimensionId;
        //        holon.ParentDimension = parentCelestialBody.ParentDimension;
        //    }

        //    if (holon.ParentGalaxyClusterId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyClusterId = parentCelestialBody.ParentGalaxyClusterId;
        //        holon.ParentGalaxyCluster = parentCelestialBody.ParentGalaxyCluster;
        //    }

        //    if (holon.ParentGalaxyId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyId = parentCelestialBody.ParentGalaxyId;
        //        holon.ParentGalaxy = parentCelestialBody.ParentGalaxy;
        //    }

        //    if (holon.ParentSolarSystemId == Guid.Empty)
        //    {
        //        holon.ParentSolarSystemId = parentCelestialBody.ParentSolarSystemId;
        //        holon.ParentSolarSystem = parentCelestialBody.ParentSolarSystem;
        //    }

        //    if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGreatGrandSuperStarId = parentCelestialBody.ParentGreatGrandSuperStarId;
        //        holon.ParentGreatGrandSuperStar = parentCelestialBody.ParentGreatGrandSuperStar;
        //    }

        //    if (holon.ParentGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGrandSuperStarId = parentCelestialBody.ParentGrandSuperStarId;
        //        holon.ParentGrandSuperStar = parentCelestialBody.ParentGrandSuperStar;
        //    }

        //    if (holon.ParentSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentSuperStarId = parentCelestialBody.ParentSuperStarId;
        //        holon.ParentSuperStar = parentCelestialBody.ParentSuperStar;
        //    }

        //    if (holon.ParentStarId == Guid.Empty)
        //    {
        //        holon.ParentStarId = parentCelestialBody.ParentStarId;
        //        holon.ParentStar = parentCelestialBody.ParentStar;
        //    }

        //    if (holon.ParentPlanetId == Guid.Empty)
        //    {
        //        holon.ParentPlanetId = parentCelestialBody.ParentPlanetId;
        //        holon.ParentPlanet = parentCelestialBody.ParentPlanet;
        //    }

        //    if (holon.ParentMoonId == Guid.Empty)
        //    {
        //        holon.ParentMoonId = parentCelestialBody.ParentMoonId;
        //        holon.ParentMoon = parentCelestialBody.ParentMoon;
        //    }

        //    if (holon.ParentCelestialSpaceId == Guid.Empty)
        //    {
        //        holon.ParentCelestialSpaceId = parentCelestialBody.ParentCelestialSpaceId;
        //        holon.ParentCelestialSpace = parentCelestialBody.ParentCelestialSpace;
        //    }

        //    if (holon.ParentCelestialBodyId == Guid.Empty)
        //    {
        //        holon.ParentCelestialBodyId = parentCelestialBody.ParentCelestialBodyId;
        //        holon.ParentCelestialBody = parentCelestialBody.ParentCelestialBody;
        //    }

        //    if (holon.ParentZomeId == Guid.Empty)
        //    {
        //        holon.ParentZomeId = parentCelestialBody.ParentZomeId;
        //        holon.ParentZome = parentCelestialBody.ParentZome;
        //    }

        //    if (holon.ParentHolonId == Guid.Empty)
        //    {
        //        holon.ParentHolonId = parentCelestialBody.ParentHolonId;
        //        holon.ParentHolon = parentCelestialBody.ParentHolon;
        //    }

        //    switch (parentCelestialBody.HolonType)
        //    {
        //        case HolonType.GreatGrandSuperStar:
        //            holon.ParentGreatGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GrandSuperStar:
        //            holon.ParentGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGrandSuperStar = (IGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SuperStar:
        //            holon.ParentSuperStarId = parentCelestialBody.Id;
        //            holon.ParentSuperStar = (ISuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Multiverse:
        //            holon.ParentMultiverseId = parentCelestialBody.Id;
        //            holon.ParentMultiverse = (IMultiverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Universe:
        //            holon.ParentUniverseId = parentCelestialBody.Id;
        //            holon.ParentUniverse = (IUniverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Dimension:
        //            holon.ParentDimensionId = parentCelestialBody.Id;
        //            holon.ParentDimension = (IDimension)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GalaxyCluster:
        //            holon.ParentGalaxyClusterId = parentCelestialBody.Id;
        //            holon.ParentGalaxyCluster = (IGalaxyCluster)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Galaxy:
        //            holon.ParentGalaxyId = parentCelestialBody.Id;
        //            holon.ParentGalaxy = (IGalaxy)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SolarSystem:
        //            holon.ParentSolarSystemId = parentCelestialBody.Id;
        //            holon.ParentSolarSystem = (ISolarSystem)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Star:
        //            holon.ParentStarId = parentCelestialBody.Id;
        //            holon.ParentStar = (IStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Planet:
        //            holon.ParentPlanetId = parentCelestialBody.Id;
        //            holon.ParentPlanet = (IPlanet)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Moon:
        //            holon.ParentMoonId = parentCelestialBody.Id;
        //            holon.ParentMoon = (IMoon)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Zome:
        //            holon.ParentZomeId = parentCelestialBody.Id;
        //            holon.ParentZome = (IZome)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Holon:
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = parentCelestialBody;
        //            break;
        //    }

        //    holons.Add(holon);

        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

        //    if (saveHolon)
        //    {
        //        result = await base.SaveHolonAsync(holon, false, true, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
        //        result.IsSaved = true;
        //    }
        //    else
        //    {
        //        result.Message = "Holon was not saved due to saveHolon being set to false.";
        //        result.IsSaved = false;
        //        result.Result = holon;
        //    }

        //    return result;
        //}

        protected virtual async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsAsync(IEnumerable<IHolon> holons, HolonType holonType, bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons == null || refresh)
                //result = await base.LoadHolonsForParentAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);
                result = await base.LoadChildHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);
            else
            {
                result.Message = "Refresh not required";
                result.Result = holons;
            }

            return result;
        }

        private void SetParentsAndAddZome(IZome zome)
        {
            zome.ParentHolonId = this.Id;
            zome.ParentCelestialBodyId = this.Id;

            switch (this.HolonType)
            {
                case HolonType.Moon:
                    zome.ParentMoonId = this.Id;
                    break;

                case HolonType.Planet:
                    zome.ParentPlanetId = this.Id;
                    break;

                case HolonType.Star:
                    zome.ParentStarId = this.Id;
                    break;

                case HolonType.SuperStar:
                    zome.ParentSuperStarId = this.Id;
                    break;

                case HolonType.GrandSuperStar:
                    zome.ParentGrandSuperStarId = this.Id;
                    break;

                case HolonType.GreatGrandSuperStar:
                    zome.ParentGrandSuperStarId = this.Id;
                    break;
            }

            this.Zomes.Add(zome);
        }


        private void BlankParentsAndRemoveZome(IZome zome)
        {
            zome.ParentHolonId = Guid.Empty;
            zome.ParentCelestialBodyId = Guid.Empty;

            switch (this.HolonType)
            {
                case HolonType.Moon:
                    zome.ParentMoonId = Guid.Empty;
                    break;

                case HolonType.Planet:
                    zome.ParentPlanetId = Guid.Empty;
                    break;

                case HolonType.Star:
                    zome.ParentStarId = Guid.Empty;
                    break;

                case HolonType.SuperStar:
                    zome.ParentSuperStarId = Guid.Empty;
                    break;

                case HolonType.GrandSuperStar:
                    zome.ParentGrandSuperStarId = Guid.Empty;
                    break;

                case HolonType.GreatGrandSuperStar:
                    zome.ParentGrandSuperStarId = Guid.Empty;
                    break;
            }

            this.Zomes.Remove(zome);
        }

    }
}