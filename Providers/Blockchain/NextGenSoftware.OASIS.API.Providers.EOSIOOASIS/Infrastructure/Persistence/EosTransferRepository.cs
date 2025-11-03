using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Exceptions;
using EosSharp.Core.Providers;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
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
        
        public async Task<OASISResult<ITransactionRespone>> TransferEosToken(string fromAccountName, string toAccountName, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessageTemplate = "Error occured while executing a transfer request! Reason: {0}";

            try
            {
                var pushTransactionResult = await _eos.CreateTransaction(new EosSharp.Core.Api.v1.Transaction
                {
                    actions = new List<EosSharp.Core.Api.v1.Action>
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
                TransactionHelper.CheckForTransactionErrors(ref result);

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
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (ApiErrorException e)
            {
                var apiErrorMessage = $"{e.Message} Code: {e.code}, Message: {e.message}, Details: {string.Join(',', e.error.details)}.";
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, e.Message), e);
            }
            
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> TransferEosNft(string fromAccountName, string toAccountName, decimal amount, string nftSymbol)
        {            
            var result = new OASISResult<ITransactionRespone>();
            string errorMessageTemplate = "Error occured whilst executing a transfer nft request! Reason: {0}";

            try
            {
                var pushNftTransactionResult = await _eos.CreateTransaction(new EosSharp.Core.Api.v1.Transaction
                {
                    actions = new List<EosSharp.Core.Api.v1.Action>
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
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessageTemplate.Substring(0, errorMessageTemplate.Length - 4));

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
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (ApiErrorException e)
            {
                var apiErrorMessage = $"{e.Message} Code: {e.code}, Message: {e.message}, Details: {string.Join(',', e.error.details)}.";
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, apiErrorMessage), e);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, string.Format(errorMessageTemplate, e.Message), e);
            }
            
            return result;
        }
    }
}