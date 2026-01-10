using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.IO;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class OASISMiddleware
    {
        private readonly RequestDelegate _next;

        //public OASISMiddleware(RequestDelegate next, IOptions<OASISDNA> OASISSettings)
        public OASISMiddleware(RequestDelegate next)
        {
            _next = next;

            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
            {
                Console.WriteLine("[OASISMiddleware] Starting OASIS BootLoader...");
                Console.WriteLine($"[OASISMiddleware] Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"[OASISMiddleware] Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                // Try to find OASIS_DNA.json in multiple locations
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "OASIS_DNA.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OASIS_DNA.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "OASIS Architecture", "NextGenSoftware.OASIS.API.DNA", "OASIS_DNA.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "OASIS Architecture", "NextGenSoftware.OASIS.API.DNA", "OASIS_DNA.json"),
                    "OASIS_DNA.json"
                };
                
                string dnaPath = null;
                foreach (var path in possiblePaths)
                {
                    string fullPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
                    Console.WriteLine($"[OASISMiddleware] Checking path: {fullPath}");
                    if (System.IO.File.Exists(fullPath))
                    {
                        dnaPath = fullPath;
                        Console.WriteLine($"[OASISMiddleware] ✅ Found OASIS_DNA.json at: {dnaPath}");
                        break;
                    }
                }
                
                if (dnaPath == null)
                {
                    Console.WriteLine("[OASISMiddleware] ⚠️ WARNING: Could not find OASIS_DNA.json, using default path");
                    dnaPath = "OASIS_DNA.json";
                }
                
                Console.WriteLine($"[OASISMiddleware] Using OASIS_DNA.json path: {dnaPath}");
                var bootResult = OASISBootLoader.OASISBootLoader.BootOASIS(dnaPath);
                
                if (bootResult.IsError)
                {
                    Console.WriteLine($"[OASISMiddleware] ❌ ERROR: BootLoader failed! Reason: {bootResult.Message}");
                    if (!string.IsNullOrEmpty(bootResult.DetailedMessage))
                        Console.WriteLine($"[OASISMiddleware] Detailed Error: {bootResult.DetailedMessage}");
                }
                else if (bootResult.Result)
                {
                    Console.WriteLine("[OASISMiddleware] ✅ OASIS BootLoader started successfully!");
                    Console.WriteLine($"[OASISMiddleware] IsOASISBooted: {OASISBootLoader.OASISBootLoader.IsOASISBooted}");
                }
                else
                {
                    Console.WriteLine($"[OASISMiddleware] ⚠️ WARNING: BootLoader returned false. Message: {bootResult.Message}");
                }
            }
            else
            {
                Console.WriteLine("[OASISMiddleware] OASIS is already booted.");
            }

            //OASISProviderManager.OASISSettings = OASISSettings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            //TODO: Try and make this more efficient, currently if they override provider in REST call and do not set Global flag to true then even if the next call is the same, it will switch back to default provider below and then have to switch back again to the override provider specified in the REST call...
            //if (!ProviderManager.IgnoreDefaultProviderTypes && ProviderManager.DefaultProviderTypes != null && ProviderManager.CurrentStorageProviderType != (ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]))
            //      ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default);

            await _next(context);
        }
    }
}