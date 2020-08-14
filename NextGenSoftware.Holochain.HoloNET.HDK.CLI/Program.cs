using System;
using System.Reflection;

namespace NextGenSoftware.Holochain.HoloNET.HDK.CLI
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
                Console.WriteLine($"NextGen Software Holochain HoloNET HDK CLI v{versionString}");
                Console.WriteLine($"***********************************************");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  nethdk --build -classFolder");
                Console.WriteLine("  nethdk --convert -rusthAppRootFolder");
                Console.WriteLine($"***********************************************");
                return;
            }


            if (args[0].ToLower() == "build")
                Build(args[1]);
        }

        static void Build(string classFolder)
        {
            Core.NETHDK hdk = new Core.NETHDK();
            hdk.Build(classFolder);
        }
    }
}
