using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OLANDManager : OASISManager
    {
        public OLANDManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public OLANDManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

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
    }
}