using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    /// <summary>
    /// Hashgraph account information
    /// </summary>
    public class HashgraphAccountInfo
    {
        public string AccountId { get; set; }
        public long? Balance { get; set; }
        public long? AutoRenewPeriod { get; set; }
        public string Expiry { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string SeedPhrase { get; set; }
    }
}
