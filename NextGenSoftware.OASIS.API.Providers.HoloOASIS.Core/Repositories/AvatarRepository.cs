using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories
{
    public class AvatarRepository
    {
        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsAsync(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                HcAvatarCollection hcAvatars = new HcAvatarCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatars != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcAvatars.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcAvatar> loadResult = await hcAvatars.LoadCollectionAsync(collectionAnchor);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IAvatar> avatars = new List<IAvatar>();

                        foreach (HcAvatar avatar in loadResult.EntriesLoaded)
                            avatars.Add(DataHelper.ConvertHcAvatarToAvatar(avatar));

                        result.Result = avatars;
                    }


                    //result = HandleLoadCollectionResponse(await hcAvatars.LoadCollectionAsync(collectionAnchor), collectionName, collectionAnchor, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the AvatarRepository.LoadAvatarsAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAvatars(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                HcAvatarCollection hcAvatars = new HcAvatarCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatars != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcAvatars.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcAvatar> loadResult = hcAvatars.LoadCollection(collectionAnchor);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IAvatar> avatars = new List<IAvatar>();

                        foreach (HcAvatar avatar in loadResult.EntriesLoaded)
                            avatars.Add(DataHelper.ConvertHcAvatarToAvatar(avatar));

                        result.Result = avatars;
                    }


                    //result = HandleLoadCollectionResponse(await hcAvatars.LoadCollectionAsync(collectionAnchor), collectionName, collectionAnchor, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the AvatarRepository.LoadAvatars method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }
    }
}
