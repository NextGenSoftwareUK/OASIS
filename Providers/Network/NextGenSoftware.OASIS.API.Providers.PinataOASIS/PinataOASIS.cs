using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS
{
    public class PinataPinListResponse
    {
        public List<PinataPin> Rows { get; set; }
    }

    public class PinataPin
    {
        public string IpfsPinHash { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }


    public partial class PinataOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _secretKey;
        private string _jwt;
        private string _gatewayUrl;
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;
        private IPinataService _pinataService;

        public PinataOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            _pinataService = new PinataService(_apiKey, _secretKey, _jwt);
            Init();
        }

        public PinataOASIS(string OASISDNAPath)
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
            Init();
        }

        public PinataOASIS(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            Init();
        }

        public PinataOASIS(OASISDNA OASISDNA, string OASISDNAPath)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
            Init();
        }
    }
}
