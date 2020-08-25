using System;
using System.Reflection;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var versionString = Assembly.GetEntryAssembly()
                                       .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                       .InformationalVersion
                                       .ToString();

            Console.WriteLine($"********************************************************************");
            Console.WriteLine($"NextGen Software Holochain HoloNET HDK/ODK TEST HARNESS v{versionString}");
            Console.WriteLine($"********************************************************************");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  star genesis -dnaFolder -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given OAPP DNA.");
            Console.WriteLine("  star genesis -transmute -hAppDNA -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given hApp DNA.");
            Console.WriteLine("  star light -planetName = Build a planet (OAPP).");
            Console.WriteLine("  star shine -planetName = Launch & activate a planet (OAPP) by shining the star's light upon it...");
            Console.WriteLine("  star dim -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("  star seed -planetName = Deploy a planet (OAPP).");
            Console.WriteLine("  star twinkle -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("  star dust -planetName = Delete a planet (OAPP).");
            Console.WriteLine("  star radiate -planetName = Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*");
            Console.WriteLine("  star emit -planetName = Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)");
            Console.WriteLine("  star reflect -planetName = Show stats of the Planet (OAPP).");
            Console.WriteLine("  star evolve -planetName = Upgrade/update a Planet (OAPP).");
            Console.WriteLine("  star mutate -planetName = Import/Export hApp, dApp & others.");
            Console.WriteLine("  star love -planetName = Send/Receive Love.");
            Console.WriteLine("  star super - Reserved For Future Use...");
            Console.WriteLine($"********************************************************************");

            string dnaFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\DNA";
            string cSharpGeneisFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\Genesis\CSharp";
            string rustGenesisFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\Genesis\Rust";

            // Create Planet (OAPP) by generating dynamic template/scaffolding code.
            IPlanet ourWorld = Star.Genesis("Our World", dnaFolder, cSharpGeneisFolder, rustGenesisFolder, "NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis");
            
            /*
            string firstEntryHCHash = ourWorld.Zomes[0].Holons[0].ProviderKey;
           // ourWorld.Zomes[0].

            // Build
            Star.Light(ourWorld);

            // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
            Star.Shine(ourWorld);

            // Deactivate the planet (OAPP)
            Star.Dim(ourWorld);

            // Deploy the planet (OAPP)
            Star.Seed(ourWorld);

            // Run Tests
            Star.Twinkle(ourWorld);

            // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
            Star.Radiate(ourWorld);

            // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
            Star.Emit(ourWorld);

            // Show stats of the Planet (OAPP).
            Star.Reflect(ourWorld);

            // Upgrade/update a Planet (OAPP).
            Star.Evolve(ourWorld);

            // Import/Export hApp, dApp & others.
            Star.Mutate(ourWorld);

            // Send/Receive Love
            Star.Love(ourWorld);

            // Reserved For Future Use...
            Star.Super(ourWorld);

            // Delete a planet (OAPP).
            Star.Dust(ourWorld);
            */
        }
    }
}
