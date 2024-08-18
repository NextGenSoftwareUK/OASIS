using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories
{
    public class HolonRepository
    {
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsAsync(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                HcHolonCollection hcHolons = new HcHolonCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcHolons != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcHolons.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcHolon> loadResult = await hcHolons.LoadCollectionAsync(collectionAnchor, customDataKeyValuePairs);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IHolon> holons = new List<IHolon>();

                        foreach (HcHolon holon in loadResult.EntriesLoaded)
                            holons.Add(DataHelper.ConvertHcHolonToHolon(holon));

                        result.Result = holons;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the HolonRepository.LoadHolonsAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolons(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                HcHolonCollection hcHolons = new HcHolonCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcHolons != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcHolons.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcHolon> loadResult = hcHolons.LoadCollection(collectionAnchor, customDataKeyValuePairs);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IHolon> holons = new List<IHolon>();

                        foreach (HcHolon holon in loadResult.EntriesLoaded)
                            holons.Add(DataHelper.ConvertHcHolonToHolon(holon));

                        result.Result = holons;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the HolonRepository.LoadHolons method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, string collectionName, string collectionAnchor = "", string zomeBatchUpdateCollectionFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                HcHolonCollection hcHolons = new HcHolonCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcHolons != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeBatchUpdateCollectionFunctionName))
                        hcHolons.ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunctionName;

                    HoloNETCollectionSavedResult saveResult = await hcHolons.SaveAllChangesAsync(true, true, true, true, collectionAnchor, customDataKeyValuePairs);

                    if (saveResult != null && !saveResult.IsError)
                    {
                        List<IHolon> savedHolons = new List<IHolon>();

                        foreach (HcHolon holon in saveResult.EntiesSaved)
                            savedHolons.Add(DataHelper.ConvertHcHolonToHolon(holon));

                        result.Result = savedHolons;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving collection {collectionName} with anchor {collectionAnchor} in the HolonRepository.SaveHolonsAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, string collectionName, string collectionAnchor = "", string zomeBatchUpdateCollectionFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                HcHolonCollection hcHolons = new HcHolonCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcHolons != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeBatchUpdateCollectionFunctionName))
                        hcHolons.ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunctionName;

                    HoloNETCollectionSavedResult saveResult = hcHolons.SaveAllChanges(true, true, true, true, collectionAnchor, customDataKeyValuePairs);

                    if (saveResult != null && !saveResult.IsError)
                    {
                        List<IHolon> savedHolons = new List<IHolon>();

                        foreach (HcHolon holon in saveResult.EntiesSaved)
                            savedHolons.Add(DataHelper.ConvertHcHolonToHolon(holon));

                        result.Result = savedHolons;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving collection {collectionName} with anchor {collectionAnchor} in the HolonRepository.SaveHolonsAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }
    }
}
