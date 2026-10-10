using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Ipfs.Http;
using Ipfs;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.Utilities;
using Ipfs.CoreApi;
using System.Xml.Linq;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public partial class IPFSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        public IpfsClient IPFSClient;
        //public IpfsEngine IPFSEngine; //= new IpfsEngine();

        private List<IAvatarDetail> AvatarsDetailsList;
        private string avatarDetailsFileAddress;
        private Dictionary<string, HolonResume> _idLookup = new Dictionary<string, HolonResume>();
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;

        public IPFSOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;

            Init();
        }

        public IPFSOASIS(string OASISDNAPath)
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA, string OASISDNAPath)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
            Init();
        }

        private void Init()
        {
            this.ProviderName = "IPFSOASIS";
            this.ProviderDescription = "IPFS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IPFSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }
    }
}
