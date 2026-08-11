using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class KeysController : OASISControllerBase
    {
        private KeyManager _keyManager = null;

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _keyManager = new KeyManager(result.Result);
                }

                return _keyManager;
            }
        }

        /// <summary>
        ///     Clear's the KeyManager's internal cache of keys.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("clear-cache")]
        public OASISResult<bool> ClearCache()
        {
            return KeyManager.ClearCache();
        }
    }

    /// <summary>
    /// Key information model
    /// </summary>
    public class KeyInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Create key request model
    /// </summary>
    public class CreateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Update key request model
    /// </summary>
    public class UpdateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
