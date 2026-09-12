using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Events;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public partial class SEEDSOASIS : OASISProvider, IOASISApplicationProvider, IOASISStorageProvider
    {
        private static Random _random = new Random();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private readonly HttpClient _httpClient;

        public const string ENDPOINT_TEST = "https://test.hypha.earth";
        public const string ENDPOINT_LIVE = "https://node.hypha.earth";
        public const string SEEDS_EOSIO_ACCOUNT_TEST = "seeds";
        public const string SEEDS_EOSIO_ACCOUNT_LIVE = "seed.seeds";
        public const string CHAINID_TEST = "1eaa0824707c8c16bd25145493bf062aecddfeb56c736f6ba6397f3195f33c9f";
        public const string CHAINID_LIVE = "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11";
        public const string PUBLICKEY_TEST = "EOS8MHrY9xo9HZP4LvZcWEpzMVv1cqSLxiN2QMVNy8naSi1xWZH29";
        public const string PUBLICKEY_TEST2 = "EOS8C9tXuPMkmB6EA7vDgGtzA99k1BN6UxjkGisC1QKpQ6YV7MFqm";
        public const string PUBLICKEY_LIVE = "EOS6kp3dm9Ug5D3LddB8kCMqmHg2gxKpmRvTNJ6bDFPiop93sGyLR";
        public const string PUBLICKEY_LIVE2 = "EOS6kp3dm9Ug5D3LddB8kCMqmHg2gxKpmRvTNJ6bDFPiop93sGyLR";
        public const string APIKEY_TEST = "EOS7YXUpe1EyMAqmuFWUheuMaJoVuY3qTD33WN4TrXbEt8xSKrdH9";
        public const string APIKEY_LIVE = "EOS7YXUpe1EyMAqmuFWUheuMaJoVuY3qTD33WN4TrXbEt8xSKrdH9";

        private TelosOASIS.TelosOASIS TelosOASIS { get; set; }


        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                {
                    if (TelosOASIS != null)
                        _avatarManager = new AvatarManager(TelosOASIS, OASISDNA);
                    else
                    {
                        if (!ProviderManager.Instance.IsProviderRegistered(Core.Enums.ProviderType.TelosOASIS))
                            throw new Exception("TelosOASIS Provider Not Registered. Please register and try again.");
                        else
                            throw new Exception("TelosOASIS Provider Is Registered But Was Not Injected Into SEEDSOASIS Provider.");
                    }
                }

                return _avatarManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this, OASISDNA);

                return _keyManager;
            }
        }

        public SEEDSOASIS(TelosOASIS.TelosOASIS telosOASIS)
        // public SEEDSOASIS(string telosConnectionString)
        {
            this.ProviderName = "SEEDSOASIS";
            this.ProviderDescription = "SEEDS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SEEDSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Application);
            TelosOASIS = telosOASIS;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(ENDPOINT_TEST);

            // TelosOASIS = new TelosOASIS.TelosOASIS(telosConnectionString);
        }

        //event EventDelegates.StorageProviderError IOASISStorageProvider.OnStorageProviderError
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

    }
}
