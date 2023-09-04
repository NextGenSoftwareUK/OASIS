
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public class STARDNA
    {
        //Default values that are used to generate a new STARDNA.json file if it is not found.

        public string BasePath = @"C:\Users\\USER\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK";
        //public string RustDNAReduxTemplateFolder = @"NextGenSoftware.OASIS.STAR\RustDNATemplates\Redux";
        public string RustDNARSMTemplateFolder = @"NextGenSoftware.OASIS.STAR\DNATemplates\RustDNATemplates\RSM";
        public string CSharpDNATemplateFolder = @"NextGenSoftware.OASIS.STAR\DNATemplates\CSharpDNATemplates";
        public string CelestialBodyDNA = @"NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
        //public string GenesisRustFolder = @"NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\Rust";
        //public string GenesisCSharpFolder = @"NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\CSharp";
        public string GenesisFolder = @"NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net7.0\Genesis";
        public string GenesisNamespace = "NextGenSoftware.OASIS.STAR.TestHarness.Genesis";
        public string TemplateNamespace = "NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates";
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
        public string CSharpTemplateICelestialBodyDNA = @"Interfaces\ICelestialBodyDNATemplate.cs";
        public string CSharpTemplateCelestialBodyDNA = "CelestialBodyDNATemplate.cs";
        public string CSharpTemplateLoadHolonDNA = "LoadHolonDNATemplate.cs";
        public string CSharpTemplateSaveHolonDNA = "SaveHolonDNATemplate.cs";
        public string CSharpTemplateInt = @"types\int.cs";
        public string CSharpTemplateString = @"types\string.cs";
        public string CSharpTemplateBool = @"types\bool.cs";
        //public string OAPPConsoleTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Console";
        //public string OAPPBlazorTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor";
        //public string OAPPWebMVCTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebMVC";
        //public string OAPPMAUITemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.MAUI";
        //public string OAPPUnityTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Unity";
        //public string OAPPWinFormsTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WinForms";
        //public string OAPPWPFTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WPF";
        //public string OAPPWindowsServiceTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WindowsService";
        //public string OAPPRESTServiceTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.RESTService";
        //public string OAPPgRPCServiceTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.gRPCService";
        //public string OAPPGraphQLServiceTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.GraphQLService";
        //public string OAPPCustomTemplateDNA = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Custom";

        public string OAPPConsoleTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Console.DLL";
        public string OAPPBlazorTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor";
        public string OAPPWebMVCTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebMVC";
        public string OAPPMAUITemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.MAUI";
        public string OAPPUnityTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Unity";
        public string OAPPWinFormsTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WinForms";
        public string OAPPWPFTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WPF";
        public string OAPPWindowsServiceTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WindowsService";
        public string OAPPRESTServiceTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.RESTService";
        public string OAPPgRPCServiceTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.gRPCService";
        public string OAPPGraphQLServiceTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.GraphQLService";
        public string OAPPCustomTemplateDNA = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Custom";
        public string OAPPCelestialBodiesFolder = "CelestialBodies";
        public Dictionary<ProviderType, string> StarProviderKey = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId;
        public string DefaultGrandSuperStarId;
        public string DefaultSuperStarId;
        public string DefaultStarId;
        public string DefaultPlanetId;
    }
}
