using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Exceptions;
using EosSharp.Core.Providers;
//using EosSharp.Core;
//using EosSharp.Core.Api.v1;
//using EosSharp.Core.Exceptions;
//using EosSharp.Core.Providers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
//using Action = EosSharp.Core.Api.v1.Action;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence
{
    public class EosTransferRepository : IEosTransferRepository
    {
        private readonly Eos _eos;
        private readonly string _eosAccountName;
        
        public EosTransferRepository(string eosAccountName, string eosChainUrl,
            string eosChainId, string eosAccountPk)
        {
            _eosAccountName = eosAccountName;
            _eos = new Eos(new EosConfigurator
            {
                HttpEndpoint = eosChainUrl,
                ChainId = eosChainId,
                ExpireSeconds = 60,
                SignProvider = new DefaultSignProvider(eosAccountPk)
            });
        }
        
        public async Task<OASISResult<TransactionRespone>> TransferEosToken(string fromAccountName, string toAccountName, decimal amount)
        {
            var result = new OASISResult<TransactionRespone>();
            string errorMessageTemplate = "Error occured while executing a transfer request! Reason: {0}";

            try
            {
                var pushTransactionResult = await _eos.CreateTransaction(new Transaction
                {
                    actions = new List<Action>
                    {
                        new()
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new List<PermissionLevel>
                            {
                                new()
                                {
                                    actor = _eosAccountName,
                                    permission = "active"
                                }
                            },
                            data = new
                            {
                                from = fromAccountName,
                                to = toAccountName,
                                quantity = $"{amount} EOS",
                                memo = "OASIS transferring"
                            }
                        }
                    }
                });
                
                result.Result.TransactionResult = pushTransactionResult;
                ErrorHandling.CheckForTransactionErrors(ref result);

                if (!result.IsError)
                {
                    result.Message = "Transferring token request was successful. " +
                        $"Received transaction hash response: {pushTransactionResult}. " +
                        $"Transfer token from: from: {fromAccountName}, to: {toAccountName}, amount: {amount}";

                    LoggingManager.Log(result.Message, LogType.Info);
                }
            }
            catch (ApiException e)
            {
                var apiErrorMessage = $"{e.Message} Status: {e.StatusCode}, Content: {e.Content}.";
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (ApiErrorException e)
            {
                var apiErrorMessage = $"{e.Message} Code: {e.code}, Message: {e.message}, Details: {string.Join(',', e.error.details)}.";
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, e.Message), e);
            }
            
            return result;
        }

        public async Task<OASISResult<TransactionRespone>> TransferEosNft(string fromAccountName, string toAccountName, decimal amount, string nftSymbol)
        {            
            var result = new OASISResult<TransactionRespone>();
            string errorMessageTemplate = "Error occured whilst executing a transfer nft request! Reason: {0}";

            try
            {
                var pushNftTransactionResult = await _eos.CreateTransaction(new Transaction
                {
                    actions = new List<Action>
                    {
                        new()
                        {
                            account = "eosio.nft",
                            name = "transfer",
                            authorization = new List<PermissionLevel>
                            {
                                new()
                                {
                                    actor = _eosAccountName,
                                    permission = "active"
                                }
                            },
                            data = new
                            {
                                from = fromAccountName,
                                to = toAccountName,
                                quantity = $"{amount} {nftSymbol}",
                                memo = "OASIS nft transferring"
                            }
                        }
                    }
                });

                result.Result.TransactionResult = pushNftTransactionResult;
                ErrorHandling.CheckForTransactionErrors(ref result, true, errorMessageTemplate.Substring(0, errorMessageTemplate.Length - 4));

                if (!result.IsError)
                {
                    result.Message = "Transferring nft request was successful. " +
                    $"Received transaction hash response: {pushNftTransactionResult}. " +
                    $"Transfer nft {nftSymbol} from: from: {fromAccountName}, to: {toAccountName}, amount: {amount}";

                    LoggingManager.Log(result.Message, LogType.Info);
                }
            }
            catch (ApiException e)
            {
                var apiErrorMessage = $"{e.Message} Status: {e.StatusCode}, Content: {e.Content}.";
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (ApiErrorException e)
            {
                var apiErrorMessage = $"{e.Message} Code: {e.code}, Message: {e.message}, Details: {string.Join(',', e.error.details)}.";
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, e.Message), e);
            }
            
            return result;
        }
    }
}