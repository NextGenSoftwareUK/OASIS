using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private OASISResult<IHolon> SaveHolonForProviderType(IHolon holon, Guid avatarId, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<T> SaveHolonForProviderType<T>(IHolon holon, Guid avatarId, ProviderType providerType, OASISResult<T> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = (T)saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveHolonForProviderTypeAsync(IHolon holon, Guid avatarId, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) //TODO: Dont think this should be an optional param?!
        {
            string errorMessage = $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the HolonManager.SaveHolonForProviderTypeAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason:";

            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    List<IHolon> children = null;
                    List<IHolon> allChildHolons = null;

                    //Build child holon list recursively (flatten holons).
                    allChildHolons = BuildAllChildHolonsList(holon, new List<IHolon>(), recursive, maxChildDepth, 0, continueOnError);

                    //Store the child id's so it is quicker and easier to load all child holons in future.
                    //TODO: This may cause an issue for large collection of child holons because the id's are GUIDs so not sure the maximum size the string can be? And also cause issues with how much each provider can store?
                    //      Well I guess, if this limit is reached then we can either store some of them and flag somehow its not the full list so it will need to find the others the slower way (but if we need to do that is there any point storing anything?).
                    //      I think for now, if the limit is reached then we won't store anything for the id list's and then the load code will need to work the old way (slower) where it needs to manually load each level if child nodes.
                    //      Be good, if we could store smaller id's somewhow instead of Guids? Be good to have a long Id number as well as the Guid? The Guid will be unique across the full OASIS but the long id's would only be unique for that provider. We already have ProviderUniqueStorageKey but sometimes these keys could also be long so the long is better.
                    //      But for now we will stick with Guids and how it works here and if and when it creates a problem we can try out the above solutions... ;-)
                    holon.ChildIdListCache = BuildChildHolonIdList(holon);
                    holon.AllChildIdListCache = BuildAllChildHolonIdList(allChildHolons);

                    //We will save the children seperateley so temp remove and restore after.
                    if (!saveChildrenOnProvider)
                    {
                        children = holon.Children.ToList();
                        holon.Children.Clear();
                    }

                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                    {
                        if (!saveChildrenOnProvider)
                        {
                            holon.Children = children;
                            saveHolonResult.Result.Children = children;
                        }
                       
                        if (saveChildren && !saveChildrenOnProvider)
                        {
                            //Build child holon list recursively (flatten holons).
                            //List<IHolon> childHolons = BuildChildHolonsList(holon, new List<IHolon>(), recursive, maxChildDepth, 0, continueOnError);

                            if (allChildHolons.Count > 0)
                            {
                                OASISResult<IEnumerable<IHolon>> saveChildHolonsResult = await SaveHolonsAsync(allChildHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, true, providerType);

                                if (saveChildHolonsResult != null && saveChildHolonsResult.Result != null && !saveChildHolonsResult.IsError)
                                {
                                    result.SavedCount += allChildHolons.Count;
                                    
                                    //TODO: Dont think this is needed because updated automatically because all objects are byRef! ;-) But double check...
                                    //saveHolonResult.Result.Children = MapChildren(saveChildHolonsResult.Result, saveHolonResult.Result.Children); //Map the saved holons back onto their original non flattened holons.
                                }
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The holon {LoggingHelper.GetHolonInfoForLogging(holon)} saved fine but errors occured saving some of it's children: {saveChildHolonsResult.Message}");
                            }
                        }

                        //result.Result = saveHolonResult.Result;
                        result.Result = holon; //Automatically updated because ByRef! ;-)
                        result.IsSaved = true;
                        result.SavedCount++;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveHolonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveHolonForProviderTypeAsync<T>(IHolon holon, Guid avatarId, ProviderType providerType, OASISResult<T> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            string errorMessage = $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the HolonManager.SaveHolonForProviderTypeAsync<T> method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason:";

            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    List<IHolon> children = null;
                    List<IHolon> allChildHolons = null;

                    //Build child holon list recursively (flatten holons).
                    allChildHolons = BuildAllChildHolonsList(holon, new List<IHolon>(), recursive, maxChildDepth, 0, continueOnError);

                    //Store the child id's so it is quicker and easier to load all child holons in future.
                    //TODO: This may cause an issue for large collection of child holons because the id's are GUIDs so not sure the maximum size the string can be? And also cause issues with how much each provider can store?
                    //      Well I guess, if this limit is reached then we can either store some of them and flag somehow its not the full list so it will need to find the others the slower way (but if we need to do that is there any point storing anything?).
                    //      I think for now, if the limit is reached then we won't store anything for the id list's and then the load code will need to work the old way (slower) where it needs to manually load each level if child nodes.
                    //      Be good, if we could store smaller id's somewhow instead of Guids? Be good to have a long Id number as well as the Guid? The Guid will be unique across the full OASIS but the long id's would only be unique for that provider. We already have ProviderUniqueStorageKey but sometimes these keys could also be long so the long is better.
                    //      But for now we will stick with Guids and how it works here and if and when it creates a problem we can try out the above solutions... ;-)
                    holon.ChildIdListCache = BuildChildHolonIdList(holon);
                    holon.AllChildIdListCache = BuildAllChildHolonIdList(allChildHolons);

                    //We will save the children seperateley so temp remove and restore after.
                    if (!saveChildrenOnProvider)
                    {
                        children = holon.Children.ToList();
                        holon.Children.Clear();
                    }

                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                    {
                        if (!saveChildrenOnProvider)
                        {
                            holon.Children = children;
                            saveHolonResult.Result.Children = children;
                        }

                        if (saveChildren && !saveChildrenOnProvider)
                        {
                            //Build child holon list recursively (flatten holons).
                            //List<IHolon> childHolons = BuildChildHolonsList(holon, new List<IHolon>(), recursive, maxChildDepth, 0, continueOnError);

                            if (allChildHolons.Count > 0)
                            {
                                List<T> genericHolons = new List<T>();

                                foreach (IHolon child in allChildHolons)
                                    genericHolons.Add((T)child);

                                //OASISResult<IEnumerable<T>> saveChildHolonsResult = await SaveHolonsAsync<T>((IEnumerable<T>)allChildHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, true, providerType);
                                OASISResult<IEnumerable<T>> saveChildHolonsResult = await SaveHolonsAsync(genericHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, true, providerType);

                                if (saveChildHolonsResult != null && saveChildHolonsResult.Result != null && !saveChildHolonsResult.IsError)
                                {
                                    result.SavedCount += allChildHolons.Count;

                                    //TODO: Dont think this is needed because updated automatically because all objects are byRef! ;-) But double check...
                                    //saveHolonResult.Result.Children = MapChildren(saveChildHolonsResult.Result, saveHolonResult.Result.Children); //Map the saved holons back onto their original non flattened holons.
                                }
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The holon {LoggingHelper.GetHolonInfoForLogging(holon)} saved fine but errors occured saving some of it's children: {saveChildHolonsResult.Message}");
                            }
                        }

                        //result.Result = saveHolonResult.Result;
                        result.Result = (T)holon; //Automatically updated because ByRef! ;-)
                        result.IsSaved = true;
                        result.SavedCount++;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveHolonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForProviderType(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    foreach (IHolon holon in holons)
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        result.Result = saveHolonsResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to save the holons in the SaveHolons method using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> SaveHolonsForProviderType<T>(IEnumerable<T> holons, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    List<IHolon> holonsToSave = new List<IHolon>();

                    foreach (IHolon holon in holons)
                    {
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                        holonsToSave.Add(holon);
                    }

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holonsToSave, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        List<T> savedHolons = new List<T>();

                        foreach (IHolon holon in saveHolonsResult.Result)
                            savedHolons.Add((T)holon);

                        result.Result = savedHolons;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to save the holons in the SaveHolons method using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForProviderTypeAsync(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false)
        {
            string errorMessage = $"An error occured attempting to save the holons in the SaveHolonsAsync method for the {Enum.GetName(providerType)} provider. Reason:";

            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    Dictionary<string, List<IHolon>> children = new Dictionary<string, List<IHolon>>();

                    //If the childHolons have not already been flattened into the holons list (recursively build a flat list of all children using BuildChildHolonsList or other means) then flatten now...
                    if (!childHolonsFlattened)
                        holons = BuildAllChildHolonsList(holons, new List<IHolon>());

                    foreach (IHolon holon in holons)
                    {
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
     
                        //We will save the children seperateley so temp remove and restore after.
                        if (!saveChildrenOnProvider)
                        {
                            children[holon.Id.ToString()] = new List<IHolon>(holon.Children);
                            holon.Children.Clear();
                        }

                        //Store the child id's so it is quicker and easier to load all child holons in future.
                        //TODO: This may cause an issue for large collection of child holons because the id's are GUIDs so not sure the maximum size the string can be? And also cause issues with how much each provider can store?
                        //      Well I guess, if this limit is reached then we can either store some of them and flag somehow its not the full list so it will need to find the others the slower way (but if we need to do that is there any point storing anything?).
                        //      I think for now, if the limit is reached then we won't store anything for the id list's and then the load code will need to work the old way (slower) where it needs to manually load each level if child nodes.
                        //      Be good, if we could store smaller id's somewhow instead of Guids? Be good to have a long Id number as well as the Guid? The Guid will be unique across the full OASIS but the long id's would only be unique for that provider. We already have ProviderUniqueStorageKey but sometimes these keys could also be long so the long is better.
                        //      But for now we will stick with Guids and how it works here and if and when it creates a problem we can try out the above solutions... ;-)
                        holon.ChildIdListCache = BuildChildHolonIdList(holon);
                        holon.AllChildIdListCache = BuildAllChildHolonIdList(holons.ToList()); //TODO: Check if this works as expected...
                    }

                    if (saveChildren && !saveChildrenOnProvider)
                    {
                        ////If the childHolons have not already been flattened into the holons list (recursively build a flat list of all children using BuildChildHolonsList or other means) then flatten now...
                        //if (!childHolonsFlattened)
                        //    holons = BuildAllChildHolonsList(holons, new List<IHolon>());

                        OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                        if (saveHolonsResult != null && !saveHolonsResult.IsError && saveHolonsResult.Result != null)
                        {
                            if (!saveChildrenOnProvider)
                            {
                                foreach (IHolon holon in holons)
                                    holon.Children = children[holon.Id.ToString()];
                            }

                            result.SavedCount += saveHolonsResult.Result.Count();
                            //result.Result = saveHolonsResult.Result;
                            result.Result = holons; //Automatically updated because ByRef! ;-) //TODO: Check this works for lists like it does above for single holons! ;-)
                            result.IsSaved = true;
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} {saveHolonsResult.Message}");
                    }      
                }
            }
            catch (Exception ex)
            {
                string errorMessage2 = $"{ errorMessage } {ex}";

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage2);
                }
                else
                    LoggingManager.Log(errorMessage2, LogType.Error);
            }

            return result;
        }

        //private async Task ProcessChildrenAsync(IHolon holon, Guid avatarId, IEnumerable<IHolon> children, OASISResult<IEnumerable<IHolon>> result, ProviderType providerType, string errorMessage, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) 
        //{
        //    try
        //    {
        //        //holon.AllChildren = children;
        //        result.Result.AllChildren = children;
        //    }
        //    catch (Exception e) { }

        //    if (saveChildren && !saveChildrenOnProvider)
        //    {
        //        List<IHolon> childHolons = BuildChildHolonsList(holon, new List<IHolon>());

        //        if (childHolons.Count > 0)
        //        {
        //            OASISResult<IEnumerable<IHolon>> saveChildHolonsResult = await SaveHolonsAsync(childHolons, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

        //            if (saveChildHolonsResult != null && saveChildHolonsResult.Result != null && !saveChildHolonsResult.IsError)
        //            {
        //                result.SavedCount += childHolons.Count;

        //                for (int i = 0; i < result.Result.Children.Count; i++)
        //                {
        //                    IHolon child = saveChildHolonsResult.Result.FirstOrDefault(x => x.Id == result.Result.Children[i].Id);

        //                    if (child != null)
        //                        result.Result.Children[i] = Mapper.MapBaseHolonProperties(child, result.Result.Children[i]);
        //                }
        //            }
        //            else
        //                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The holon {LoggingHelper.GetHolonInfoForLogging(holon)} saved fine but errors occured saving some of it's children: {saveChildHolonsResult.Message}");
        //        }
        //    }
        //}

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForProviderTypeAsync<T>(IEnumerable<T> holons, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    List<IHolon> holonsToSave = new List<IHolon>();
                    
                    foreach (IHolon holon in holons)
                    {
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                        holonsToSave.Add(holon);
                    }

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holonsToSave, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        List<T> savedHolons = new List<T>();

                        foreach (IHolon holon in saveHolonsResult.Result)
                            savedHolons.Add((T)holon);

                        result.Result = savedHolons;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occured attempting to save the holons in the SaveHolonsAsync method for the {Enum.GetName(providerType)} provider. Reason: {ex.ToString()}";

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForListOfProviders<T>(IEnumerable<IHolon> holons, OASISResult<IEnumerable<IHolon>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = SaveHolonsForProviderType(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForListOfProvidersAsync(IEnumerable<IHolon> holons, OASISResult<IEnumerable<IHolon>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonsForProviderTypeAsync(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> SaveHolonsForListOfProviders<T>(IEnumerable<T> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<IEnumerable<T>> holonSaveResult = new OASISResult<IEnumerable<T>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = SaveHolonsForProviderType(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForListOfProvidersAsync<T>(IEnumerable<T> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<IEnumerable<T>> holonSaveResult = new OASISResult<IEnumerable<T>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonsForProviderTypeAsync(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<IHolon> SaveHolonForListOfProviders(IHolon holon, Guid avatarId, OASISResult<IHolon> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = SaveHolonForProviderType(holon, avatarId, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveHolonForListOfProvidersAsync(IHolon holon, Guid avatarId, OASISResult<IHolon> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonForProviderTypeAsync(holon, avatarId, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<T> SaveHolonForListOfProviders<T>(IHolon holon, Guid avatarId, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<T> holonSaveResult = new OASISResult<T>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                { 
                    holonSaveResult = SaveHolonForProviderType(holon, avatarId, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveHolonForListOfProvidersAsync<T>(IHolon holon, Guid avatarId, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            OASISResult<T> holonSaveResult = new OASISResult<T>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonForProviderTypeAsync<T>(holon, avatarId, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError<T>(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        //private IList<IHolon> MapChildren(IEnumerable<IHolon> sourceFlattenedHolons, IList<IHolon> targetHolons)
        //{
        //    for (int i = 0; i < targetHolons.Count(); i++)
        //    {
        //        IHolon child = sourceFlattenedHolons.FirstOrDefault(x => x.Id == targetHolons[i].Id);

        //        if (child != null)
        //            targetHolons[i] = Mapper.MapBaseHolonProperties(child, targetHolons[i], true, false);

        //        targetHolons[i].Children = MapChildren(sourceFlattenedHolons, targetHolons[i].Children);
        //    }

        //    return targetHolons;
        //}

        //private IList<IHolon> MapChildren<T>(IEnumerable<T> sourceFlattenedHolons, IList<IHolon> targetHolons) where T : IHolon
        //{
        //    for (int i = 0; i < targetHolons.Count(); i++)
        //    {
        //        IHolon child = sourceFlattenedHolons.FirstOrDefault(x => x.Id == targetHolons[i].Id);

        //        if (child != null)
        //            targetHolons[i] = Mapper.MapBaseHolonProperties(child, targetHolons[i], true, false);

        //        targetHolons[i].Children = MapChildren(sourceFlattenedHolons, targetHolons[i].Children);
        //    }

        //    return targetHolons;
        //}
    }
}