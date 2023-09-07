using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OLandManager : OASISManager
    {
        private const int OlandUnitPrice = 17;
        private INFTManager _nftManager;

        //TODO: Move this to a DB (use OASIS Data API) so this data is dynamic and can be changed at runtime!
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

        public OLandManager(INFTManager nftManager, IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _nftManager = nftManager;
        }

        public OLandManager(INFTManager nftManager, OASISDNA OASISDNA = null) : base(OASISDNA)
        {
            _nftManager = nftManager;
        }

        public async Task<OASISResult<IEnumerable<IOLand>>> LoadAllOlands()
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
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                
                var olandsEntity = loadResult.Result.Select(holon => new Oland 
                {
                    Discount = Convert.ToDecimal(holon.MetaData[nameof(IOLand.Discount)].Replace(",", ".")),
                    Id = Guid.Parse(holon.MetaData[nameof(IOLand.Id)]),
                    Price = Convert.ToDecimal(holon.MetaData[nameof(IOLand.Price)].Replace(",", ".")),
                    IsRemoved = bool.Parse(holon.MetaData[nameof(IOLand.IsRemoved)]),
                    OlandsCount = int.Parse(holon.MetaData[nameof(IOLand.OlandsCount)]),
                    PreviousId = Guid.Parse(holon.MetaData[nameof(IOLand.PreviousId)]),
                    RightSize = Convert.ToDecimal(holon.MetaData[nameof(IOLand.RightSize)].Replace(",", ".")),
                    TopSize = Convert.ToDecimal(holon.MetaData[nameof(IOLand.TopSize)].Replace(",", ".")),
                    UnitOfMeasure = holon.MetaData[nameof(IOLand.UnitOfMeasure)]
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<IOLand>> LoadOland(Guid olandId)
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
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var olandEntity = new Oland()
                {
                    Discount = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.Discount)].Replace(",", ".")),
                    Id = Guid.Parse(loadResult.Result.MetaData[nameof(IOLand.Id)]),
                    Price = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.Price)].Replace(",", ".")),
                    IsRemoved = bool.Parse(loadResult.Result.MetaData[nameof(IOLand.IsRemoved)]),
                    OlandsCount = int.Parse(loadResult.Result.MetaData[nameof(IOLand.OlandsCount)]),
                    PreviousId = Guid.Parse(loadResult.Result.MetaData[nameof(IOLand.PreviousId)]),
                    RightSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.RightSize)].Replace(",", ".")),
                    TopSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOLand.TopSize)].Replace(",", ".")),
                    UnitOfMeasure = loadResult.Result.MetaData[nameof(IOLand.UnitOfMeasure)]
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<bool>> DeleteOland(Guid olandId)
        {
            return await Data.DeleteHolonAsync(olandId);
        }

        public async Task<OASISResult<string>> SaveOland(IOLand request)
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
                var saveResult = await Data.SaveHolonAsync(olandHolon);
                if (saveResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = saveResult.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<string>> UpdateOland(IOLand request)
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
                
                var saveResult = await Data.SaveHolonAsync(olandHolon);
                if (saveResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = saveResult.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<string>> PurchaseOland(IOLandPurchase olandPurchase)
        {
            var response = new OASISResult<string>();
            
            try
            {
                olandPurchase.Id = new Guid();
                var olandHolon = new Holon
                {
                    IsNewHolon = true,
                    MetaData =
                    {
                        [nameof(IOLandPurchase.Id)] = olandPurchase.Id.ToString(),
                        [nameof(IOLandPurchase.OlandId)] = olandPurchase.OlandId.ToString(),
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
                
                var saveResult = await Data.SaveHolonAsync(olandHolon);
                if (saveResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Result = null;
                    response.Message = saveResult.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
        {
            var response = new OASISResult<int>();

            try
            {
                if (count <= 0)
                    ErrorHandling.HandleError(ref response, "Count property need's to be greater then zero!");
                else
                    response.Result = OlandByCountPrice.ContainsKey(count)
                    ? OlandByCountPrice[count]
                    : OlandUnitPrice * count;
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref response, e.Message, e);
            }

            return response;
        }

        public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request)
        {
            var response = new OASISResult<PurchaseOlandResponse>();
            try
            {
                if (request == null)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = "Request is NULL! Bad Request!";
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                //TODO: Re-write this so is generic.
                await _nftManager.CreateNftTransactionAsync(new NFTWalletTransaction()
                {
                    Amount = 0, //TODO: Need to derive price from the tiles selected. //request.Tiles
                    Date = DateTime.Now,
                    FromWalletAddress = null, //TODO: Need to either pre-mint OLAND NFT's and then use FromWalletAddress of the NFT or mint on the fly and then use the new address...
                    MemoText = "0 OLAND's", //TODO: Need to dervive from the tiles selected.
                    MintWalletAddress = null, //TODO: Need to either pre-mint OLAND NFT's and then use FromWalletAddress of the NFT or mint on the fly and then use the new address...
                    ToWalletAddress = request.WalletAddress,
                    ProviderType = request.ProviderType,
                    Token = "POLY" //TODO: Currently OLAND's are minted on Polgon via OpenSea, this may change in future... This will also be dynamic in future...
                }) ;

                //var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(new PurchaseRequestModel(request.CargoSaleId));
                //if (cargoPurchaseResponse.IsError)
                //{
                //    response.IsError = true;
                //    response.IsSaved = false;
                //    response.Message = cargoPurchaseResponse.Message;
                //    ErrorHandling.HandleError(ref response, response.Message);
                //    return response;
                //}

                var purchaseOlandResult = await PurchaseOland(new OlandPurchase()
                {
                    PurchaseDate = DateTime.Now,
                    Id = Guid.NewGuid(),
                    Tiles = request.Tiles,
                    AvatarId = request.AvatarId,
                    AvatarUsername = request.AvatarUsername,
                    WalletAddress = request.WalletAddress,
                    OlandId = request.OlandId,
                    TransactionHash = cargoPurchaseResponse.Result.TransactionHash,
                    ErrorMessage = cargoPurchaseResponse.Message,
                    CargoSaleId = request.CargoSaleId,
                    IsSucceedPurchase = !cargoPurchaseResponse.IsError
                });

                response.Result = new PurchaseOlandResponse(purchaseOlandResult.Result);
                
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsError = false;
                response.Message = e.Message;
                response.Exception = e;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }
    }
}