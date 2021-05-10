
using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    //public class OASISDNA
    //{
    //    public StarDNA StarDNA { get; set; }
    //    public string Secret { get; set; }
    //    public EmailSettings Email { get; set; }
    //    public StorageProviderSettings StorageProviders { get; set; }
    //}

    public class STARDNA
    {
        //Default values that are used to generate a new STARDNA.json file if it is not found.

        //TODO: Make the paths relative!
        //public string RustDNATemplateFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR\RustDNATemplates";
        //public string CSharpDNATemplateFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR\CSharpDNATemplates";
        //public string CelestialBodyDNA = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
        //public string GenesisRustFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\Genesis\Rust";
        //public string GenesisCSharpFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\Genesis\CSharp";
        public string RustDNAReduxTemplateFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR\RustDNATemplates\Redux";
        public string RustDNARSMTemplateFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR\RustDNATemplates\RSM";
        public string CSharpDNATemplateFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR\CSharpDNATemplates";
        public string CelestialBodyDNA = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
        //public string GenesisRustFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\Genesis\Rust";
        //public string GenesisCSharpFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\Genesis\CSharp";
        public string GenesisRustFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\Rust";
        public string GenesisCSharpFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\CSharp";
        public string GenesisNamespace = "NextGenSoftware.OASIS.STAR.TestHarness.Genesis";
        public string TemplateNamespace = "NextGenSoftware.OASIS.STAR.CSharpTemplates";
        public string RustTemplateLib = @"core\lib.rs";
        public string RustTemplateHolon = @"core\holon.rs";
        public string RustTemplateValidation = @"core\validation.rs";
        public string RustTemplateCreate = @"crud\create.rs";
        public string RustTemplateRead = @"crud\read.rs";
        public string RustTemplateUpdate = @"crud\update.rs";
        public string RustTemplateDelete = @"crud\delete.rs";
        public string RustTemplateList = @"crud\list.rs";
        public string RustTemplateInt = @"types\int.rs";
        public string RustTemplateString = @"types\string.rs";
        public string RustTemplateBool = @"types\bool.rs";
        public string CSharpTemplateIHolonDNA = @"Interfaces\IHolonDNATemplate.cs";
        public string CSharpTemplateHolonDNA = "HolonDNATemplate.cs";
        public string CSharpTemplateIZomeDNA = @"Interfaces\IZomeDNATemplate.cs";
        public string CSharpTemplateZomeDNA = "ZomeDNATemplate.cs";
        public string CSharpTemplateIStarDNA = @"Interfaces\IStarDNATemplate.cs";
        public string CSharpTemplateStarDNA = "StarDNATemplate.cs";
        public string CSharpTemplateIPlanetDNA = @"Interfaces\IPlanetDNATemplate.cs";
        public string CSharpTemplatePlanetDNA = "PlanetDNATemplate.cs";
        public string CSharpTemplateIMoonDNA = @"Interfaces\IMoonDNATemplate.cs";
        public string CSharpTemplateMoonDNA = "MoonDNATemplate.cs";
        public string CSharpTemplateICelestialBodyDNA = @"Interfaces\ICelestialBodyDNATemplate.cs";
        public string CSharpTemplateCelestialBodyDNA = "CelestialBodyDNATemplate.cs";
        public Dictionary<ProviderType, string> StarProviderKey = new Dictionary<ProviderType, string>();
        public Guid StarProviderId;
        public string OASISProviders = "HoloOASIS,MongoDBOASIS";
        //public string HolochainConductorURI = "ws://localhost:8888";
        //public string HoloNETClientType = "Desktop";
        public string HolochainVersion = "Redux"; //Valid values: Redux or RSM.
    }

    /*
    public class StorageProviderSettings
    {
        public string DefaultProviders { get; set; }

        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoDBOASISProviderSettings MongoDBOASIS { get; set; }

        public EOSIOASISProviderSettings EOSIOOASIS { get; set; }

        public ThreeFoldOASISProviderSettings ThreeFoldOASIS { get; set; }

        public EthereumOASISProviderSettings EthereumOASIS { get; set; }

        public SQLLiteDBOASISSettings SQLLiteDBOASIS { get; set; }
    }

    public class EmailSettings
    {
        public string EmailFrom { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
    }

    public class ProviderSettingsBase
    {
        public string ConnectionString { get; set; }
    }

    public class HoloOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class MongoDBOASISProviderSettings : ProviderSettingsBase
    {
        public string DBName { get; set; }
    }

    public class EOSIOASISProviderSettings : ProviderSettingsBase
    {
    }

    public class ThreeFoldOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class EthereumOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class SQLLiteDBOASISSettings : ProviderSettingsBase
    {
    }*/
}
