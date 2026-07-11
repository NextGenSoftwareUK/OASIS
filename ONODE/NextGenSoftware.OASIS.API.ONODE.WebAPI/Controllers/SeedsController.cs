// Original SEEDS controller kept for reference (from user code, fully commented):
//using System;
//using Microsoft.AspNetCore.Mvc;
//using EOSNewYork.EOSCore.Response.API;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
//using NextGenSoftware.OASIS.Common;
//
//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("api/seeds")]
//    public class SeedsController : OASISControllerBase
//    {
//        SEEDSOASIS _SEEDSOASIS = null;
//
//        SEEDSOASIS SEEDSOASIS
//        {
//            get
//            {
//                if (_SEEDSOASIS == null)
//                {
//                    /*
//                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString, true);
//
//                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
//                    if (result.IsError)
//                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS). Error details: ", result.Message), true, false, true);
//                    */
//
//                    // TODO: Currently SEEDSOASIS is injected in with a TelosOASIS provider with a SEEDS connectionstring and will unregister the TelosOASIS Provider first if it is already registered so it uses the SEEDS connection string instead.
//                    // Not sure if we want SEEDSOASIS to use its own seperate private instance of the TelosOASIS provider using the SEEDS connection string allowing others to use the existing TelosOASIS Provider on the default Telos connectionstring?
//                    // If that is the case then uncomment the bottom line and comment the top line.
//
//                   // _SEEDSOASIS = new SEEDSOASIS((TelosOASIS)result.Result);
//                    _SEEDSOASIS = new SEEDSOASIS(new TelosOASIS(
//                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
//                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
//                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
//                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey
//                        ));
//                }
//
//                return _SEEDSOASIS;
//            }
//        }
//
//        public SeedsController()
//        {
//
//        }
//
//        /// <summary>
//        /// Get's all of the SEEDS Organisations.
//        /// </summary>
//        /// <returns></returns>
//        [Authorize]
//        [HttpGet("get-all-organisations")]
//        public OASISResult<string> GetAllOrganisations()
//        {
//            return new(SEEDSOASIS.GetAllOrganisationsAsJSON());
//        }
//
//        // ... (rest of original SEEDS endpoints)
//    }
//}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/seeds")]
    public class SeedsController : OASISControllerBase
    {
        private SeedsManager _seedsManager;

        private SeedsManager SeedsManager
        {
            get
            {
                if (_seedsManager == null)
                {
                    var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, $"Error activating default storage provider. Details: {result.Message}");

                    _seedsManager = new SeedsManager(result.Result, AvatarId);
                }

                return _seedsManager;
            }
        }

        /// <summary>
        /// Saves a seed transaction for an avatar.
        /// </summary>
        [Authorize]
        [HttpPost("save-seed-transaction")]
        public async Task<OASISResult<SeedTransaction>> SaveSeedTransaction([FromBody] SaveSeedTransactionRequest request)
        {
            if (request == null)
                return new OASISResult<SeedTransaction> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with AvatarUserName, Amount, and optional AvatarId, Memo." };
            var targetAvatarId = request.AvatarId == Guid.Empty ? AvatarId : request.AvatarId;
            return await SeedsManager.SaveSeedTransactionAsync(targetAvatarId, request.AvatarUserName, request.Amount, request.Memo);
        }

        /// <summary>
        /// Gets all seed transactions for the specified avatar.
        /// </summary>
        [Authorize]
        [HttpGet("avatar/{avatarId}/transactions")]
        public async Task<OASISResult<IEnumerable<SeedTransaction>>> GetSeedTransactionsForAvatar(Guid avatarId)
        {
            return await SeedsManager.LoadSeedTransactionsForAvatarAsync(avatarId);
        }

        /// <summary>
        /// Gets all seed transactions for the current avatar.
        /// </summary>
        [Authorize]
        [HttpGet("me/transactions")]
        public async Task<OASISResult<IEnumerable<SeedTransaction>>> GetMySeedTransactions()
        {
            return await SeedsManager.LoadSeedTransactionsForAvatarAsync(AvatarId);
        }
    }

    public class SaveSeedTransactionRequest
    {
        public Guid AvatarId { get; set; }
        public string AvatarUserName { get; set; }
        public int Amount { get; set; }
        public string Memo { get; set; }
    }
}

