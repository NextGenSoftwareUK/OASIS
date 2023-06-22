//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System;

//namespace NextGenSoftware.OASIS.API.Core.Managers
//{
//    public class NFTManager : OASISManager
//    {
//        private static NFTManager _instance = null;

//        public static NFTManager Instance
//        {
//            get
//            {
//                if (_instance == null)
//                    _instance = new NFTManager(ProviderManager.CurrentStorageProvider);

//                return _instance;
//            }
//        }

//        public NFTManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
//        {

//        }

//        //private readonly ISolanaService _solanaService;
//        //private readonly ICargoService _cargoService;

//        private readonly OLANDManager _olandManager;

//        private const int OlandUnitPrice = 17;

//        /// <summary>
//        /// Key: OLAND Count
//        /// Value: Price
//        /// </summary>
//        private readonly Dictionary<int, int> OlandByCountPrice = new Dictionary<int, int>()
//        {
//            { 5, 80 },
//            { 10, 160 },
//            { 20, 325 },
//            { 25, 405 },
//            { 50, 820 },
//            { 100, 1665 },
//            { 200, 3360 },
//            { 400, 6740 },
//            { 500, 8435 },
//            { 800, 13530 },
//            { 1600, 27100 },
//            { 3200, 54000 },
//            { 6400, 108000 },
//            { 12800, 216000 },
//            { 25600, 432000 },
//            { 51200, 864000 },
//            { 102400, 1728000 },
//            { 204800, 3456000 },
//            { 409600, 6912000 },
//            { 819200, 13824000 },
//        };

//        public NftService(ISolanaService solanaService, ICargoService cargoService)
//        {
//            _solanaService = solanaService;
//            _cargoService = cargoService;
//            _olandManager = new OLANDManager();
//        }

//        public async Task<OASISResult<NftTransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request)
//        {
//            var response = new OASISResult<NftTransactionRespone>();
//            try
//            {
//                var nftTransaction = new NftTransactionRespone();
//                switch (request.NftProvider)
//                {
//                    case NftProvider.Cargo:
//                        var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(request.CargoExchange);
//                        if (cargoPurchaseResponse.IsError)
//                        {
//                            response.IsError = true;
//                            response.Message = cargoPurchaseResponse.Message;
//                            ErrorHandling.HandleError(ref response, response.Message);
//                            return response;
//                        }
//                        nftTransaction.TransactionResult = cargoPurchaseResponse.Result.TransactionHash;
//                        break;
//                    case NftProvider.Solana:
//                        var exchangeResult = await _solanaService.ExchangeTokens(request.SolanaExchange);
//                        if (exchangeResult.IsError)
//                        {
//                            response.IsError = true;
//                            response.Message = exchangeResult.Message;
//                            ErrorHandling.HandleError(ref response, response.Message);
//                            return response;
//                        }
//                        nftTransaction.TransactionResult = exchangeResult.Result.TransactionHash;
//                        break;
//                }
//                response.IsError = false;
//            }
//            catch (Exception e)
//            {
//                response.IsError = true;
//                response.Exception = e;
//                response.Message = e.Message;
//                ErrorHandling.HandleError(ref response, e.Message);
//            }
//            return response;
//        }

//        public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
//        {
//            var response = new OASISResult<int>();
//            try
//            {
//                if (count <= 0)
//                {
//                    response.IsError = true;
//                    response.Message = "Count property need to be greater then zero!";
//                    ErrorHandling.HandleError(ref response, response.Message);
//                    return response;
//                }

//                response.Result = OlandByCountPrice.ContainsKey(count)
//                    ? OlandByCountPrice[count]
//                    : OlandUnitPrice * count;
//            }
//            catch (Exception e)
//            {
//                response.IsError = true;
//                response.Message = e.Message;
//                response.Exception = e;
//                ErrorHandling.HandleError(ref response, e.Message);
//            }
//            return response;
//        }

//        public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request)
//        {
//            var response = new OASISResult<PurchaseOlandResponse>();
//            try
//            {
//                if (request == null)
//                {
//                    response.IsError = true;
//                    response.IsSaved = false;
//                    response.Message = "Request is NULL! Bad Request!";
//                    ErrorHandling.HandleError(ref response, response.Message);
//                    return response;
//                }

//                var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(new PurchaseRequestModel(request.CargoSaleId));
//                if (cargoPurchaseResponse.IsError)
//                {
//                    response.IsError = true;
//                    response.IsSaved = false;
//                    response.Message = cargoPurchaseResponse.Message;
//                    ErrorHandling.HandleError(ref response, response.Message);
//                    return response;
//                }

//                var purchaseOlandResult = await _olandManager.PurchaseOland(new OlandPurchase()
//                {
//                    PurchaseDate = DateTime.Now,
//                    Id = Guid.NewGuid(),
//                    Tiles = request.Tiles,
//                    AvatarId = request.AvatarId,
//                    AvatarUsername = request.AvatarUsername,
//                    WalletAddress = request.WalletAddress,
//                    OlandId = request.OlandId,
//                    TransactionHash = cargoPurchaseResponse.Result.TransactionHash,
//                    ErrorMessage = cargoPurchaseResponse.Message,
//                    CargoSaleId = request.CargoSaleId,
//                    IsSucceedPurchase = !cargoPurchaseResponse.IsError
//                });
//                response.Result = new PurchaseOlandResponse(purchaseOlandResult.Result);
//            }
//            catch (Exception e)
//            {
//                response.IsError = true;
//                response.IsError = false;
//                response.Message = e.Message;
//                response.Exception = e;
//                ErrorHandling.HandleError(ref response, e.Message);
//            }
//            return response;
//        }


//        //TODO: Lots more coming soon! ;-)
//    }
//}