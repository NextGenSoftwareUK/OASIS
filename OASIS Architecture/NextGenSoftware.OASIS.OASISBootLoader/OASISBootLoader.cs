//using NextGenSoftware.OASIS.API.Providers.TONOASIS; // Not referenced in Core Only solution
//using NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS;
//using NextGenSoftware.OASIS.API.Providers.LineaOASIS;
//using NextGenSoftware.OASIS.API.Providers.ScrollOASIS;
//using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.NLogger;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using NextGenSoftware.OASIS.API.Providers.OptimismOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;
using NextGenSoftware.OASIS.API.Providers.RootstockOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS; // TODO: Fix TRONOASIS build errors
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
//using NextGenSoftware.OASIS.API.Providers.PolkaDotOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader
{
    public static partial class OASISBootLoader
    {
        //private static string _OASISVersion = null;
        private const string SYSTEM_EMAIL = "anorak@oasisomniverse.one";
        public static bool IsOASISBooted { get; private set; } = false;
        public static bool IsOASISBooting { get; private set; } = false;

        public delegate void OASISBootLoaderError(object sender, OASISErrorEventArgs e);
        public static event OASISBootLoaderError OnOASISBootLoaderError;

        public static string OASISRuntimeVersion { get; set; } = "5.0.0";
        public static string OASISAPIVersion { get; set; } = "5.0.0";
        public static string COSMICVersion { get; set; } = "2.2.2";
        public static string STARODKVersion { get; set; } = "4.0.0";
        public static string STARRuntimeVersion { get; set; } = "4.0.0";
        public static string STARNETVersion { get; set; } = "2.5.0";
        public static string STARAPIVersion { get; set; } = "2.0.0";
        public static string WEB6APIVersion { get; set; } = "2.0.0";
        public static string WEB7APIVersion { get; set; } = "1.0.0";
        public static string WEB8APIVersion { get; set; } = "1.0.0";
        public static string WEB9APIVersion { get; set; } = "1.0.0";
        public static string WEB10APIVersion { get; set; } = "1.0.0";

        public static string DotNetVersion
        {
            get
            {
                //return string.Concat(Environment.Version.ToString(), "(", RuntimeInformation.FrameworkDescription, ")");
                return Environment.Version.ToString();
            }
        }

        //public static string DotNetVersionDetailed
        //{
        //    get
        //    {
        //        return RuntimeInformation.FrameworkDescription;
        //    }
        //}

        //public static string OASISVersion
        //{
        //    get
        //    {
        //        if (_OASISVersion == null)
        //        {
        //            Assembly assembly = typeof(OASISBootLoader).Assembly;
        //            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        //            _OASISVersion = fvi.FileVersion;
        //        }

        //        return _OASISVersion;
        //    }
        //}

        public static string OASISDNAPath
        {
            get
            {
                return OASISDNAManager.OASISDNAPath;
            }
            set
            {
                OASISDNAManager.OASISDNAPath = value;
            }
        }

        public static OASISDNA OASISDNA
        {
            get
            {
                return OASISDNAManager.OASISDNA;
            }
            set
            {
                OASISDNAManager.OASISDNA = value;
            }
        }
    }
}
