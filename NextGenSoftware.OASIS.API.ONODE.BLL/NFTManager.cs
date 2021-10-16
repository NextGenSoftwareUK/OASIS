using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.BLL
{
    public class NFTManager
    {
        private HolonManager _holonManager = null;

        public NFTManager()
        {
            OASISResult<IOASISStorage> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

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

            return _holonManager.SaveHolon(purchaseHolon);
        }

        public OASISResult<IHolon> LoadNFTPurchaseData(Guid holonId)
        {
            return _holonManager.LoadHolon(holonId);
        }

        public OASISResult<PurchaseNFTHolon> PurchaseNFT2(string walletAddress, string avatarUsername, Guid avatarId, string jsonSelectedTiles)
        {
            OASISResult<PurchaseNFTHolon> result = new OASISResult<PurchaseNFTHolon>();

            PurchaseNFTHolon purchaseHolon = new PurchaseNFTHolon();
            purchaseHolon.WalletAddress = walletAddress;
            purchaseHolon.AvatarUsername = avatarUsername;
            purchaseHolon.AvatarId = avatarId;
            purchaseHolon.JsonSelectedTiles = jsonSelectedTiles;

            //TODO: SaveHolon also needs to be made into a Generic method like the new LoadHolon<T> method below... :)
            OASISResult<IHolon> holonResult = _holonManager.SaveHolon(purchaseHolon);

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

        // TODO: This is the way we want to ideally load custom Holons! ;-)
        // There may be better ways of doing this that does not involve reflection or the MetaData Dictionary? Maybe JSON? etc...
        // Please investigate and use best and fastest performing way... thanks! ;-)
        public OASISResult<PurchaseNFTHolon> LoadNFTPurchaseData3(Guid holonId)
        {
            return _holonManager.LoadHolon<PurchaseNFTHolon>(holonId);
        }
    }
}