using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OLandManager : OASISManager
    {
        private const int OlandUnitPrice = 17;
        private INFTManager _nftManager;
        //private static OLandManager _instance = null;

        //public static OLandManager Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //            _instance = new OLandManager(NFTManager.Instance, ProviderManager.Instance.CurrentStorageProvider);

        //        return _instance;
        //    }
        //}

        //TODO: Move this to a DB (use OASIS Data API) so this data is dynamic and can be changed at runtime!
        //TODO: But better still is to replace this lookup and unit price above with an algorithm to calculate the price with a bigger and bigger discount the more that is purchased... similar to what I did for the level/karma score...
        /// <summary>
        /// Key: OLAND Count
        /// Value: Price
        /// </summary>
        private readonly Dictionary<int, int> _OlandByCountPrice = new Dictionary<int, int>()
        {
            { 5, 80 },
            { 10, 160 },
            { 20, 325 },
            { 25, 405 },
            { 50, 820 },
            { 100, 1665 },
            { 200, 3360 },
            { 400, 6740 },
            { 500, 8435 },
            { 800, 13530 },
            { 1600, 27100 },
            { 3200, 54000 },
            { 6400, 108000 },
            { 12800, 216000 },
            { 25600, 432000 },
            { 51200, 864000 },
            { 102400, 1728000 },
            { 204800, 3456000 },
            { 409600, 6912000 },
            { 819200, 13824000 },
        };

        public OLandManager(INFTManager nftManager, IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            _nftManager = nftManager;
        }

        public OLandManager(INFTManager nftManager, Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            _nftManager = nftManager;
        }

        public async Task<OASISResult<IEnumerable<IOLand>>> LoadAllOlandsAsync()
        {
            var response = new OASISResult<IEnumerable<IOLand>>();
            try
            {
                var loadResult = await Data.LoadAllHolonsAsync();
                if (loadResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = loadResult.Message;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var olandsEntity = loadResult.Result.Select(holon => new Oland
                {
                    Discount = Convert.ToDecimal(holon.MetaData[nameof(IOLand.Discount)].ToString().Replace(",", ".")),
                    Id = Guid.Parse(holon.MetaData[nameof(IOLand.Id)].ToString()),
                    Price = Convert.ToDecimal(holon.MetaData[nameof(IOLand.Price)].ToString().Replace(",", ".")),
                    IsRemoved = bool.Parse(holon.MetaData[nameof(IOLand.IsRemoved)].ToString()),
                    OlandsCount = int.Parse(holon.MetaData[nameof(IOLand.OlandsCount)].ToString()),
                    PreviousId = Guid.Parse(holon.MetaData[nameof(IOLand.PreviousId)].ToString()),
                    RightSize = Convert.ToDecimal(holon.MetaData[nameof(IOLand.RightSize)].ToString().Replace(",", ".")),
                    TopSize = Convert.ToDecimal(holon.MetaData[nameof(IOLand.TopSize)].ToString().Replace(",", ".")),
                    UnitOfMeasure = holon.MetaData[nameof(IOLand.UnitOfMeasure)].ToString()
                });
                response.Result = olandsEntity;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                response.Result = null;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<IOLand>> LoadOlandAsync(Guid olandId)
        {
            var response = new OASISResult<IOLand>();
            try
            {
                var loadResult = await Data.LoadHolonAsync(olandId);
                if (loadResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = loadResult.Message;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var olandEntity = new Oland()
                {
                    Discount = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.Discount)].ToString().Replace(",", ".")),
                    Id = Guid.Parse(loadResult.Result.MetaData[nameof(IOLand.Id)].ToString()),
                    Price = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.Price)].ToString().Replace(",", ".")),
                    IsRemoved = bool.Parse(loadResult.Result.MetaData[nameof(IOLand.IsRemoved)].ToString()),
                    OlandsCount = int.Parse(loadResult.Result.MetaData[nameof(IOLand.OlandsCount)].ToString()),
                    PreviousId = Guid.Parse(loadResult.Result.MetaData[nameof(IOLand.PreviousId)].ToString()),
                    RightSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.RightSize)].ToString().Replace(",", ".")),
                    TopSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.TopSize)].ToString().Replace(",", ".")),
                    UnitOfMeasure = loadResult.Result.MetaData[nameof(IOLand.UnitOfMeasure)].ToString()
                };

                response.Result = olandEntity;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                response.Result = null;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<IHolon>> DeleteOlandAsync(Guid olandId, Guid avatarId)
        {
            return await Data.DeleteHolonAsync(olandId, avatarId);
        }

        public async Task<OASISResult<string>> SaveOlandAsync(IOLand request)
        {
            var response = new OASISResult<string>();
            try
            {
                request.Id = new Guid();
                var olandHolon = new Holon
                {
                    IsNewHolon = true,
                    MetaData =
                    {
                        [nameof(IOLand.UnitOfMeasure)] = request.UnitOfMeasure,
                        [nameof(IOLand.Discount)] = request.Discount.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOLand.Id)] = request.Id.ToString(),
                        [nameof(IOLand.Price)] = request.Price.ToString(CultureInfo.CurrentCulture).Replace(".", ","),
                        [nameof(IOLand.IsRemoved)] = request.IsRemoved.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOLand.OlandsCount)] = request.OlandsCount.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOLand.PreviousId)] = request.PreviousId.ToString(),
                        [nameof(IOLand.RightSize)] = request.RightSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOLand.TopSize)] = request.TopSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                    }
                };
                var saveResult = await Data.SaveHolonAsync(olandHolon, AvatarId);
                if (saveResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = saveResult.Message;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                response.Result = null;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<string>> UpdateOlandAsync(IOLand request)
        {
            var response = new OASISResult<string>();
            try
            {
                request.PreviousId = request.Id;
                request.Id = new Guid();
                var olandHolon = new Holon
                {
                    IsNewHolon = false,
                    MetaData =
                    {
                        [nameof(IOLand.UnitOfMeasure)] = request.UnitOfMeasure,
                        [nameof(IOLand.Discount)] = request.Discount.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOLand.Id)] = request.Id.ToString(),
                        [nameof(IOLand.Price)] = request.Price.ToString(CultureInfo.CurrentCulture).Replace(".", ","),
                        [nameof(IOLand.IsRemoved)] = request.IsRemoved.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOLand.OlandsCount)] = request.OlandsCount.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOLand.PreviousId)] = request.PreviousId.ToString(),
                        [nameof(IOLand.RightSize)] = request.RightSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOLand.TopSize)] = request.TopSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                    }
                };

                var saveResult = await Data.SaveHolonAsync(olandHolon, AvatarId);
                if (saveResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = saveResult.Message;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                response.Result = null;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<Guid>> PurchaseOlandAsync(IOLandPurchase olandPurchase)
        {
            var response = new OASISResult<Guid>();

            try
            {
                Guid olandPurchaseId = new Guid();

                var olandHolon = new Holon
                {
                    // IsNewHolon = true,
                    MetaData =
                    {
                        [nameof(olandPurchaseId)] = olandPurchaseId.ToString(),
                        [nameof(IOLandPurchase.OlandIds)] = ListHelper.ConvertFromList(olandPurchase.OlandIds),
                        [nameof(IOLandPurchase.AvatarId)] = olandPurchase.AvatarId.ToString(),
                        [nameof(IOLandPurchase.AvatarUsername)] = olandPurchase.AvatarUsername,
                        [nameof(IOLandPurchase.Tiles)] = olandPurchase.Tiles,
                        [nameof(IOLandPurchase.WalletAddress)] = olandPurchase.WalletAddress,
                        [nameof(IOLandPurchase.PurchaseDate)] = olandPurchase.PurchaseDate.ToString(CultureInfo.InvariantCulture),
                        [nameof(IOLandPurchase.TransactionHash)] = olandPurchase.TransactionHash,
                        //[nameof(IOLandPurchase.CargoSaleId)] = olandPurchase.CargoSaleId,
                        [nameof(IOLandPurchase.IsSucceedPurchase)] = olandPurchase.IsSucceedPurchase.ToString(),
                        //[nameof(IOLandPurchase.ErrorMessage)] = olandPurchase.ErrorMessage
                    }
                };

                var saveResult = await Data.SaveHolonAsync(olandHolon, AvatarId);

                if (saveResult.IsError)
                {
                    response.Message = saveResult.Message;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                else
                    response.Result = olandPurchaseId;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref response, e.Message, e);
            }

            return response;
        }

        public async Task<OASISResult<int>> GetOlandPriceAsync(int count, string couponCode = null)
        {
            var response = new OASISResult<int>();

            try
            {
                if (count <= 0)
                    OASISErrorHandling.HandleError(ref response, "Count property need's to be greater then zero!");
                else
                    response.Result = _OlandByCountPrice.ContainsKey(count)
                    ? _OlandByCountPrice[count]
                    : OlandUnitPrice * count;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref response, e.Message, e);
            }

            return response;
        }

        public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOlandAsync(PurchaseOlandRequest request)
        {
            var response = new OASISResult<PurchaseOlandResponse>();
            try
            {
                if (request == null)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = "Request is NULL! Bad Request!";
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                OASISResult<INFTTransactionRespone> nftTransactionResponse = await _nftManager.SendNFTAsync(new NFTWalletTransactionRequest()
                {
                    Amount = Convert.ToDecimal(await GetOlandPriceAsync(request.OlandIds.Count)), //TODO:Currently only fixed sizes of OLANDS are supported, need to make dyanmic so any number of OLANDs can be used...
                    //Date = DateTime.Now,
                    FromWalletAddress = null, //TODO: Need to either pre-mint OLAND NFT's and then use FromWalletAddress of the NFT or mint on the fly and then use the new address...
                    MemoText = $"{request.OlandIds.Count} OLAND(s) with OLANDID's {ListHelper.ConvertFromList(request.OlandIds)} for Avatar {request.AvatarUsername} with AvatarID {request.AvatarId}", //TODO: Need to dervive from the tiles selected.
                    MintWalletAddress = null, //TODO: Need to either pre-mint OLAND NFT's and then use FromWalletAddress of the NFT or mint on the fly and then use the new address...
                    ToWalletAddress = request.WalletAddress,
                    FromProviderType = request.ProviderType,
                    //FromToken = "POLY" //TODO: Currently OLAND's are minted on Polgon via OpenSea, this may change in future... This will also be dynamic in future...
                });

                //var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(new PurchaseRequestModel(request.CargoSaleId));
                //if (cargoPurchaseResponse.IsError)
                //{
                //    response.IsError = true;
                //    response.IsSaved = false;
                //    response.Message = cargoPurchaseResponse.Message;
                //    OASISErrorHandling.HandleError(ref response, response.Message);
                //    return response;
                //}

                if (nftTransactionResponse.Result != null && !nftTransactionResponse.IsError)
                {
                    var purchaseOlandResult = await PurchaseOlandAsync(new OLandPurchase()
                    {
                        PurchaseDate = DateTime.Now,
                        Tiles = request.Tiles,
                        AvatarId = request.AvatarId,
                        AvatarUsername = request.AvatarUsername,
                        WalletAddress = request.WalletAddress,
                        OlandIds = request.OlandIds,
                        TransactionHash = nftTransactionResponse.Result.TransactionResult,
                        IsSucceedPurchase = true
                    });

                    response.Result = new PurchaseOlandResponse()
                    {
                        OLandPurchaseId = purchaseOlandResult.Result,
                        OlandIds = request.OlandIds,
                        TransactionHash = nftTransactionResponse.Result.TransactionResult
                    };
                }
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsError = false;
                response.Message = e.Message;
                response.Exception = e;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }
    }
}