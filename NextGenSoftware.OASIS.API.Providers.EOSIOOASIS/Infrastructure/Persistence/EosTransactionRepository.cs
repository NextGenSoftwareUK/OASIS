using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Exceptions;
using EosSharp.Core.Providers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using Action = EosSharp.Core.Api.v1.Action;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence
{
    public class EosTransactionRepository : IEosTransactionRepository
    {
        private readonly Eos _eos;
        private readonly string _eosAccountName;
        
        public EosTransactionRepository(string eosAccountName, string eosChainUrl,
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
        
        public async Task<OASISResult<string>> SendTransaction(string fromAccountName, string toAccountName, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessageTemplate = "Error was occured while executing a transfer request! Reason: {0}";

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

                LoggingManager.Log(
                    "Transferring token request was sent. " +
                    $"Received transaction hash response: {pushTransactionResult}. " +
                    $"Transfer token from: from: {fromAccountName}, to: {toAccountName}, amount: {amount}",
                    LogType.Info);
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