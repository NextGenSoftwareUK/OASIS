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

            Console.WriteLine($"***********************************************");
            Console.WriteLine($"NextGen Software Holochain HoloNET HDK TEST HARNESS v{versionString}");
            Console.WriteLine($"***********************************************");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  nethdk --build -classFolder");
            Console.WriteLine("  nethdk --convert -rusthAppRootFolder");
            Console.WriteLine($"***********************************************");

            string proxyClassFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\ProxyClasses";
            string cSharpGeneratedCodeFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\GeneratedCode\CSharp";
            string rustGeneratedCodeFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness\GeneratedCode\Rust";

            Litghter lighter = new Litghter();
            lighter.Spark(proxyClassFolder, cSharpGeneratedCodeFolder, rustGeneratedCodeFolder, "NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.ProxyClasses");
        }
    }
}
