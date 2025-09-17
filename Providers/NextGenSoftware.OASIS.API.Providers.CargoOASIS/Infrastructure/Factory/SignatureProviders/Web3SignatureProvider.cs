using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public class Web3SignatureProvider : ISignatureProvider
    {
        public async Task<OASISResult<string>> GetSignature(string accountAddress, string singingMessage, string privateKey, string hostUrl)
        {
            var result = new OASISResult<string>();
            try
            {
                if (string.IsNullOrEmpty(privateKey))
                {
                    result.Message = "Cargo private key not set in configuration file!";
                    result.IsError = true;
                    return result;
                }

                if (string.IsNullOrEmpty(hostUrl))
                {
                    result.Message = "Host url not set in configuration file!";
                    result.IsError = true;
                    return result;
                }

                if (string.IsNullOrEmpty(singingMessage))
                {
                    result.Message = "Singing message not set in configuration file!";
                    result.IsError = true;
                    return result;
                }
                
                var account = new Account(privateKey);
                var web3 = new Web3(account, hostUrl);
                var signature = await web3.Eth.Sign.SendRequestAsync(singingMessage, accountAddress);
                result.Result = signature;
            }
            catch (Exception e)
            {
                result.Message = e.Message;
                result.IsError = true;
            }

            return result;
        }
    }
}