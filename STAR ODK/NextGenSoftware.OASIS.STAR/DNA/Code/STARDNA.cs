using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public class STARDNA
    {
        //Default values that are used to generate a new STARDNA.json file if it is not found.
        public string BasePath { get; set; } = @"C:\Users\\USER\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK";
        public string OASISRunTimePath { get; set; } = @"Runtimes\OASIS Runtime\OASIS Runtime (With Holochain Conductors Embedded) v3.3.1";
        public string STARRunTimePath { get; set; } = @"Runtimes\STAR Runtime\STAR ODK Runtime v2.2.0 (With OASIS Runtime v3.3.1 HC Conductor Embedded)";
        public string OAPPDNATemplatePath { get; set; } = @"STAR OAPP DNA Templates";
        public string RustDNARSMTemplateFolder { get; set; } = @"NextGenSoftware.OASIS.STAR\DNATemplates\RustDNATemplates\RSM";
        public string CSharpDNATemplateFolder { get; set; } = @"NextGenSoftware.OASIS.STAR\DNATemplates\CSharpDNATemplates";
        public string CelestialBodyDNA { get; set; } = @"NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
        public string GenesisFolder { get; set; } = @"NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net8.0\Genesis";
        public string GenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.TestHarness.Genesis";
        public string TemplateNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates";
        public string RustTemplateLib { get; set; } = @"core\lib.rs";
        public string RustTemplateHolon { get; set; } = @"core\holon.rs";
        public string RustTemplateValidation { get; set; } = @"core\validation.rs";
        public string RustTemplateCreate { get; set; } = @"crud\create.rs";
        public string RustTemplateRead { get; set; } = @"crud\read.rs"; 
        public string RustTemplateUpdate { get; set; } = @"crud\update.rs";
        public string RustTemplateDelete { get; set; } = @"crud\delete.rs";
        public string RustTemplateList { get; set; } = @"crud\list.rs";
        public string RustTemplateInt { get; set; } = @"types\int.rs";
        public string RustTemplateString { get; set; } = @"types\string.rs";
        public string RustTemplateBool { get; set; } = @"types\bool.rs";
        public string CSharpTemplateIHolonDNA { get; set; } = @"Interfaces\IHolonDNATemplate.cs";
        public string CSharpTemplateHolonDNA { get; set; } = "HolonDNATemplate.cs";
        public string CSharpTemplateIZomeDNA { get; set; } = @"Interfaces\IZomeDNATemplate.cs";
        public string CSharpTemplateZomeDNA { get; set; } = "ZomeDNATemplate.cs";
        public string CSharpTemplateICelestialBodyDNA { get; set; } = @"Interfaces\ICelestialBodyDNATemplate.cs";
        public string CSharpTemplateCelestialBodyDNA { get; set; } = "CelestialBodyDNATemplate.cs";
        public string CSharpTemplateLoadHolonDNA { get; set; } = "LoadHolonDNATemplate.cs";
        public string CSharpTemplateSaveHolonDNA { get; set; } = "SaveHolonDNATemplate.cs";
        public string CSharpTemplateILoadHolonDNA { get; set; } = @"Interfaces\ILoadHolonDNATemplate.cs";
        public string CSharpTemplateISaveHolonDNA { get; set; } = @"Interfaces\ISaveHolonDNATemplate.cs";
        public string CSharpTemplateInt { get; set; } = @"types\int.cs";
        public string CSharpTemplateString { get; set; } = @"types\string.cs";
        public string CSharpTemplateBool { get; set; } = @"types\bool.cs";
        public string OAPPConsoleTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Console.DLL";
        public string OAPPBlazorTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor";
        public string OAPPWebMVCTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebMVC";
        public string OAPPMAUITemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.MAUI";
        public string OAPPUnityTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Unity";
        public string OAPPWinFormsTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WinForms";
        public string OAPPWPFTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WPF";
        public string OAPPWindowsServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WindowsService";
        public string OAPPRESTServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.RESTService";
        public string OAPPgRPCServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.gRPCService";
        public string OAPPGraphQLServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.GraphQLService";
        public string OAPPCustomTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Custom";
        public string OAPPGeneratedCodeFolder { get; set; } = "Generated Code";
        public Dictionary<ProviderType, string> StarProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId { get; set; }
        public string DefaultGrandSuperStarId { get; set; }
        public string DefaultSuperStarId { get; set; }
        public string DefaultStarId { get; set; }
        public string DefaultPlanetId { get; set; }
        public string DefaultPublishedOAPPsPath { get; set; }
        public string DefaultInstalledOAPPsPath { get; set; }
    }
}