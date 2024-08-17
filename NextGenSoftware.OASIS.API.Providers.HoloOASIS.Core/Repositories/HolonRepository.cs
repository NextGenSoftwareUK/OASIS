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
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsAsync<T>(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null) where T : IHolonBase
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

                    HoloNETCollectionLoadedResult<HcHolon> loadResult = await hcHolons.LoadCollectionAsync(collectionAnchor);

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

        public OASISResult<IEnumerable<IHolon>> LoadHolons<T>(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null) where T : IHolonBase
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

                    HoloNETCollectionLoadedResult<HcHolon> loadResult = hcHolons.LoadCollection(collectionAnchor);

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
    }
}
