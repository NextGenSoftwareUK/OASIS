using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class OLANDManager : OASISManager
    {
        public OLANDManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public OLANDManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }
        
        public async Task<OASISResult<IEnumerable<IOland>>> LoadAllOlands()
        {
            var response = new OASISResult<IEnumerable<IOland>>();
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
                    Discount = Convert.ToDecimal(holon.MetaData[nameof(IOland.Discount)].Replace(",", ".")),
                    Id = Guid.Parse(holon.MetaData[nameof(IOland.Id)]),
                    Price = Convert.ToDecimal(holon.MetaData[nameof(IOland.Price)].Replace(",", ".")),
                    IsRemoved = bool.Parse(holon.MetaData[nameof(IOland.IsRemoved)]),
                    OlandsCount = int.Parse(holon.MetaData[nameof(IOland.OlandsCount)]),
                    PreviousId = Guid.Parse(holon.MetaData[nameof(IOland.PreviousId)]),
                    RightSize = Convert.ToDecimal(holon.MetaData[nameof(IOland.RightSize)].Replace(",", ".")),
                    TopSize = Convert.ToDecimal(holon.MetaData[nameof(IOland.TopSize)].Replace(",", ".")),
                    UnitOfMeasure = holon.MetaData[nameof(IOland.UnitOfMeasure)]
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

        public async Task<OASISResult<IOland>> LoadOland(Guid olandId)
        {
            var response = new OASISResult<IOland>();
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
                    Discount = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOland.Discount)].Replace(",", ".")),
                    Id = Guid.Parse(loadResult.Result.MetaData[nameof(IOland.Id)]),
                    Price = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOland.Price)].Replace(",", ".")),
                    IsRemoved = bool.Parse(loadResult.Result.MetaData[nameof(IOland.IsRemoved)]),
                    OlandsCount = int.Parse(loadResult.Result.MetaData[nameof(IOland.OlandsCount)]),
                    PreviousId = Guid.Parse(loadResult.Result.MetaData[nameof(IOland.PreviousId)]),
                    RightSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOland.RightSize)].Replace(",", ".")),
                    TopSize = Convert.ToDecimal(loadResult.Result.MetaData[nameof(IOland.TopSize)].Replace(",", ".")),
                    UnitOfMeasure = loadResult.Result.MetaData[nameof(IOland.UnitOfMeasure)]
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

        public async Task<OASISResult<string>> SaveOland(IOland request)
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
                        [nameof(IOland.UnitOfMeasure)] = request.UnitOfMeasure,
                        [nameof(IOland.Discount)] = request.Discount.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOland.Id)] = request.Id.ToString(),
                        [nameof(IOland.Price)] = request.Price.ToString(CultureInfo.CurrentCulture).Replace(".", ","),
                        [nameof(IOland.IsRemoved)] = request.IsRemoved.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOland.OlandsCount)] = request.OlandsCount.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOland.PreviousId)] = request.PreviousId.ToString(),
                        [nameof(IOland.RightSize)] = request.RightSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOland.TopSize)] = request.TopSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
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

        public async Task<OASISResult<string>> UpdateOland(IOland request)
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
                        [nameof(IOland.UnitOfMeasure)] = request.UnitOfMeasure,
                        [nameof(IOland.Discount)] = request.Discount.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOland.Id)] = request.Id.ToString(),
                        [nameof(IOland.Price)] = request.Price.ToString(CultureInfo.CurrentCulture).Replace(".", ","),
                        [nameof(IOland.IsRemoved)] = request.IsRemoved.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOland.OlandsCount)] = request.OlandsCount.ToString(CultureInfo.CurrentCulture),
                        [nameof(IOland.PreviousId)] = request.PreviousId.ToString(),
                        [nameof(IOland.RightSize)] = request.RightSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
                        [nameof(IOland.TopSize)] = request.TopSize.ToString(CultureInfo.InvariantCulture).Replace(".", ","),
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

        public async Task<OASISResult<string>> PurchaseOland(IOlandPurchase olandPurchase)
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
                        [nameof(IOlandPurchase.Id)] = olandPurchase.Id.ToString(),
                        [nameof(IOlandPurchase.OlandId)] = olandPurchase.OlandId.ToString(),
                        [nameof(IOlandPurchase.AvatarId)] = olandPurchase.AvatarId.ToString(),
                        [nameof(IOlandPurchase.AvatarUsername)] = olandPurchase.AvatarUsername,
                        [nameof(IOlandPurchase.Tiles)] = olandPurchase.Tiles,
                        [nameof(IOlandPurchase.WalletAddress)] = olandPurchase.WalletAddress,
                        [nameof(IOlandPurchase.PurchaseDate)] = olandPurchase.PurchaseDate.ToString(CultureInfo.InvariantCulture),
                        [nameof(IOlandPurchase.TransactionHash)] = olandPurchase.TransactionHash,
                        [nameof(IOlandPurchase.CargoSaleId)] = olandPurchase.CargoSaleId,
                        [nameof(IOlandPurchase.IsSucceedPurchase)] = olandPurchase.IsSucceedPurchase.ToString(),
                        [nameof(IOlandPurchase.ErrorMessage)] = olandPurchase.ErrorMessage
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