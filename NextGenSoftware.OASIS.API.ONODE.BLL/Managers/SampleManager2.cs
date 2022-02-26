using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    // EXAMPLE MANAGER ON HOW TO LOAD/SAVE DATA...
    public class SampleManager2
    {
        private HolonManager _holonManager = null;

        public SampleManager2()
        {
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
            {
                string errorMessage = string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message);
                ErrorHandling.HandleError(ref result, errorMessage, true, false, true);
            }
            else
                _holonManager = new HolonManager(result.Result);
        }

        public OASISResult<IHolon> PurchaseNFT(string walletAddress, string avatarUsername, Guid avatarId, string jsonSelectedTiles)
        {
            Holon purchaseHolon = new Holon();
            purchaseHolon.MetaData["WalletAddress"] = walletAddress;
            purchaseHolon.MetaData["AvatarUsername"] = avatarUsername;
            purchaseHolon.MetaData["AvatarId"] = avatarId.ToString();
            purchaseHolon.MetaData["JsonSelectedTiles"] = jsonSelectedTiles;

            //If you do not want to use relfection then use the MetaData directly and pass in a Holon (not prefered way).
            //return _holonManager.SaveHolon<Holon>(purchaseHolon);

            //Or you can call the non-generic overload
            return _holonManager.SaveHolon(purchaseHolon);
        }

        public OASISResult<IHolon> LoadNFTPurchaseData(Guid holonId)
        {
            //This will have the metadata in the metadata dictionary, that you will need to extract yourself (not prefered way).
            return _holonManager.LoadHolon(holonId);
        }

        public OASISResult<PurchaseNFTHolon> PurchaseNFT2(string walletAddress, string avatarUsername, Guid avatarId, string jsonSelectedTiles)
        {
            PurchaseNFTHolon purchaseHolon = new PurchaseNFTHolon();
            purchaseHolon.WalletAddress = walletAddress;
            purchaseHolon.AvatarUsername = avatarUsername;
            purchaseHolon.AvatarId = avatarId;
            purchaseHolon.JsonSelectedTiles = jsonSelectedTiles;

            //If you don't mind using reflection (very small overhead especially compared to I/O storage, etc then use this way (recommended).
            return _holonManager.SaveHolon<PurchaseNFTHolon>(purchaseHolon);
        }

        public OASISResult<PurchaseNFTHolon> LoadNFTPurchaseData2(Guid holonId)
        {
            OASISResult<PurchaseNFTHolon> result = new OASISResult<PurchaseNFTHolon>();
            OASISResult<IHolon> holonResult = _holonManager.LoadHolon(holonId);

            if (!holonResult.IsError && holonResult.Result != null)
            {
                result.Result = Mapper<IHolon, PurchaseNFTHolon>.MapBaseHolonProperties(holonResult.Result);
                result.Result.WalletAddress = result.Result.MetaData["WalletAddress"];
                result.Result.AvatarUsername = result.Result.MetaData["AvatarUsername"];
                result.Result.AvatarId = new Guid(result.Result.MetaData["AvatarId"].ToString());
                result.Result.JsonSelectedTiles = result.Result.MetaData["AvatarId"];
            }
            else
                OASISResultHolonToHolonHelper<IHolon, PurchaseNFTHolon>.CopyResult(holonResult, result);

            return result;
        }

        public OASISResult<PurchaseNFTHolon> LoadNFTPurchaseData3(Guid holonId)
        {
            // Prefered way to load holons that are strongly typed.
            return _holonManager.LoadHolon<PurchaseNFTHolon>(holonId);
        }
    }
}