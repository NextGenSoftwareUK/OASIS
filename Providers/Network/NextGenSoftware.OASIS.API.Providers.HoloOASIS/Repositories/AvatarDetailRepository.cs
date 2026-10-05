using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories
{
    public class AvatarDetailRepository
    {
        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAvatarDetailsAsync(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();

            try
            {
                HcAvatarDetailCollection hcAvatars = new HcAvatarDetailCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatars != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcAvatars.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcAvatarDetail> loadResult = await hcAvatars.LoadCollectionAsync(collectionAnchor);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IAvatarDetail> avatarDetails = new List<IAvatarDetail>();

                        foreach (HcAvatarDetail avatarDetail in loadResult.EntriesLoaded)
                            avatarDetails.Add(DataHelper.ConvertHcAvatarDetailToAvatarDetail(avatarDetail));

                        result.Result = avatarDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the AvatarDetailRepository.LoadAvatarDetailsAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAvatarDetails(string collectionName, string collectionAnchor = "", string zomeLoadCollectionFunctionName = "", int version = 0, dynamic additionalParams = null)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();

            try
            {
                HcAvatarDetailCollection hcAvatars = new HcAvatarDetailCollection();
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatars != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeLoadCollectionFunctionName))
                        hcAvatars.ZomeLoadCollectionFunction = zomeLoadCollectionFunctionName;

                    HoloNETCollectionLoadedResult<HcAvatarDetail> loadResult = hcAvatars.LoadCollection(collectionAnchor);

                    if (loadResult != null && !loadResult.IsError)
                    {
                        List<IAvatarDetail> avatarDetails = new List<IAvatarDetail>();

                        foreach (HcAvatarDetail avatarDetail in loadResult.EntriesLoaded)
                            avatarDetails.Add(DataHelper.ConvertHcAvatarDetailToAvatarDetail(avatarDetail));

                        result.Result = avatarDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the AvatarDetailRepository.LoadAvatarDetails method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }
    }
}
