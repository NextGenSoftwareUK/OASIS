using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Reflection;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                        .InformationalVersion
                                        .ToString();

                Console.WriteLine($"***********************************************");
                Console.WriteLine($"NextGen Software STAR ODK CLI v{versionString}");
                Console.WriteLine($"***********************************************");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  star --light -classFolder");
                Console.WriteLine("  star --convert -rusthAppRootFolder");
                Console.WriteLine($"***********************************************");
                return;
            }


            if (args[0].ToLower() == "light")
                Build(args[1]);


           // NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates. _ssss = new SuperZome("http://www.localhost:8888", HolochainBaseZome.HoloNETClientType.Desktop);
            //  _ssss.
       
    }

        static void Build(string classFolder)
        {
            STAR.LightAsync(GenesisType.Planet, "Our World", classFolder);
        }
    }
}
