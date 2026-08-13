using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

using NextGenSoftware.OASIS.STAR.Interfaces;
using SevenZip.Buffer;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace NextGenSoftware.OASIS.STAR
{
    public static partial class STAR
    {
        // const string STAR_DNA_DEFAULT_PATH = "DNA\\STAR_DNA.json";
        // const string OASIS_DNA_DEFAULT_PATH = "DNA\\OASIS_DNA.json";
        const string STAR_DNA_DEFAULT_PATH = "DNA/STAR_DNA.json";
        const string OASIS_DNA_DEFAULT_PATH = "DNA/OASIS_DNA.json";

        private static StarStatus _status;
        private static Guid _starId = Guid.Empty;
        private static OASISAPI _OASISAPI = null;
        private static STARAPI _STARAPI = null;
        private static IPlanet _defaultPlanet = null;
        private static ISuperStar _defaultSuperStar = null;
        private static IGrandSuperStar _defaultGrandSuperStar = null;
        private static IGreatGrandSuperStar _defaultGreatGrandSuperStar = null;

        public static string STARDNAPath { get; set; } = STAR_DNA_DEFAULT_PATH;
        public static string OASISDNAPath { get; set; } = OASIS_DNA_DEFAULT_PATH;





        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public static event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public static event DataReceived OnDataReceived;
















        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}




        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType,  string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}














        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<ICelestialBody> celstialBodyResult = null)
        //{
        //    string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

        //    if (celstialBodyResult != null)
        //        msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

        //    OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        //}

        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, OASISResult<ICelestialBody> celstialBodyResult)
        //{
        //    HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist. Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist, in this case remove the {name}Id from STARDNA.json and then try again. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //}









        //private void ReplaceInTemplate(string OAPPFolder, string fileExtention)
        //{
        //    foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles($"*.{fileExtention}"))
        //    {
        //        int lineNumber = 1;
        //        string line = null;

        //        using (TextReader tr = File.OpenText(file.FullName))
        //        using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
        //        {
        //            while ((line = tr.ReadLine()) != null)
        //            {
        //                line = line.Replace("<Compile Remove=\"Program.cs\" />", "");

        //                tw.WriteLine(line);
        //                lineNumber++;
        //            }
        //        }

        //        File.Delete(file.FullName);
        //        File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
        //    }
        //}

    }
}
