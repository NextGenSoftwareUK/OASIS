using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class NFTManager : OASISManager, INFTManager
    {
        private static NFTManager _instance = null;

        public static NFTManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NFTManager(ProviderManager.CurrentStorageProvider);

                return _instance;
            }
        }

        public NFTManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public NFTManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }





        //public NftService(ISolanaService solanaService, ICargoService cargoService)
        //{
        //    _solanaService = solanaService;
        //    _cargoService = cargoService;
        //    _olandManager = new OLANDManager();
        //}

        public async Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request)
        {
            OASISResult<TransactionRespone> result = new OASISResult<TransactionRespone>();
            IOASISNFTProvider provider = null;
            string errorMessage = "Error occured in CreateNftTransactionAsync in NFTManager. Reason:";

            try
            {
                IOASISProvider OASISProvider = ProviderManager.GetProvider(request.ProviderType);

                if (OASISProvider != null)
                {
                    if (!OASISProvider.IsProviderActivated)
                    {
                        OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

                        if (activateProviderResult.IsError)
                            ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating provider. Reason: {activateProviderResult.Message}");
                    }
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.ProviderType)} provider was not found.");


                if (!result.IsError)
                {
                    provider = OASISProvider as IOASISNFTProvider;

                    if (provider != null)
                        result = provider.SendNFT(request);
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.ProviderType)} provider is not a valid OASISNFTProvider.");
                }

                /*
                switch (request.NftProvider)
                {
                    case NftProvider.Cargo:
                        var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(request.CargoExchange);
                        if (cargoPurchaseResponse.IsError)
                        {
                            response.IsError = true;
                            response.Message = cargoPurchaseResponse.Message;
                            ErrorHandling.HandleError(ref response, response.Message);
                            return response;
                        }
                        nftTransaction.TransactionResult = cargoPurchaseResponse.Result.TransactionHash;
                        break;
                    case NftProvider.Solana:
                        var exchangeResult = await _solanaService.ExchangeTokens(request.SolanaExchange);
                        if (exchangeResult.IsError)
                        {
                            response.IsError = true;
                            response.Message = exchangeResult.Message;
                            ErrorHandling.HandleError(ref response, response.Message);
                            return response;
                        }
                        nftTransaction.TransactionResult = exchangeResult.Result.TransactionHash;
                        break;
                }
                response.IsError = false;*/


            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }




        //TODO: Lots more coming soon! ;-)
    }
}