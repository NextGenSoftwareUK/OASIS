using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class NftService : INftService
    {
        private readonly ISolanaService _solanaService;
        private readonly ICargoService _cargoService;

        private const int OlandUnitPrice = 17;

        /// <summary>
        /// Key: OLAND Count
        /// Value: Discount Amount
        /// </summary>
        private readonly Dictionary<int, int> OlandByCountDiscount = new Dictionary<int, int>()
        {
            { 5, 5 },
            { 10, 10 },
            { 20, 15 },
            { 25, 20 },
            { 50, 30 },
            { 100, 35 },
            { 200, 50 },
            { 400, 60 },
            { 500, 65 },
            { 800, 70 },
            { 1600, 100 },
            { 3200, 400 },
            { 6400, 500 },
            { 12800, 600 },
            { 25600, 700 },
            { 51200, 800 },
            { 102400, 800 },
            { 204800, 900 },
            { 409600, 1000 },
            { 819200, 1100 },
        };
        
        public NftService(ISolanaService solanaService, ICargoService cargoService)
        {
            _solanaService = solanaService;
            _cargoService = cargoService;
        }

        public async Task<OASISResult<NftTransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request)
        {
            var response = new OASISResult<NftTransactionRespone>();
            try
            {
                var nftTransaction = new NftTransactionRespone();
                switch (request.NftProvider)
                {
                    case NftProvider.Cargo:
                        var cargoPurchaseResponse = await _cargoService.PurchaseCargoSale(request.CargoExchange);
                        if (cargoPurchaseResponse.IsError)
                        {
                            response.IsError = true;
                            response.Message = cargoPurchaseResponse.Message;
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
                            return response;
                        }

                        nftTransaction.TransactionResult = exchangeResult.Result.TransactionHash;
                        break;
                }
                response.IsError = false;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Exception = e;
                response.Message = e.Message;
            }
            return response;
        }

        public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
        {
            var response = new OASISResult<int>();
            try
            {
                if (count <= 0)
                {
                    response.IsError = true;
                    response.Message = "Count property need to be greater then zero!";
                    return response;
                }
                
                var priceResult = OlandUnitPrice * count;

                if (OlandByCountDiscount.TryGetValue(count, out int olandByCountDiscount))
                {
                    response.Result = priceResult - olandByCountDiscount;
                }
                else
                {
                    response.Result = priceResult;
                }
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                response.Exception = e;
            }
            return response;
        }
    }
}