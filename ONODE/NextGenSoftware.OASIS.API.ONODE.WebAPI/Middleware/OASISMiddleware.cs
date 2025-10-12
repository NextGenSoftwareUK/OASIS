using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;

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
                // Determine network from hostname or environment
                string network = DetermineNetworkFromRequest();
                System.Console.WriteLine($"🌐 Detected network: {network}");
                
                // Boot OASIS with appropriate configuration
                if (network == "devnet")
                {
                    OASISBootLoader.OASISBootLoader.BootOASIS("OASIS_DNA_devnet.json");
                    System.Console.WriteLine("🔧 Booting OASIS with devnet configuration");
                }
                else
                {
                    OASISBootLoader.OASISBootLoader.BootOASIS(); // Default to mainnet OASIS_DNA.json
                    System.Console.WriteLine("🔧 Booting OASIS with mainnet configuration");
                }
                
                // Override Solana wallet configuration from environment variables
                OverrideSolanaConfigurationFromEnvironment();
                
                // Register and activate SolanaOASIS provider
                RegisterSolanaOASISProvider();
            }
                //OASISBootLoader.OASISBootLoader.BootOASIS("OASIS_DNA.json");

            //OASISProviderManager.OASISSettings = OASISISSettings.Value;
        }

        private void OverrideSolanaConfigurationFromEnvironment()
        {
            try
            {
                var oasisDNA = OASISDNAManager.OASISDNA;
                if (oasisDNA?.OASIS?.StorageProviders?.SolanaOASIS != null)
                {
                    // Override Solana wallet configuration from environment variables
                    var privateKey = System.Environment.GetEnvironmentVariable("SolanaOASIS__PrivateKey");
                    var publicKey = System.Environment.GetEnvironmentVariable("SolanaOASIS__PublicKey");
                    var mnemonicWords = System.Environment.GetEnvironmentVariable("SolanaOASIS__WalletMnemonicWords");
                    var connectionString = System.Environment.GetEnvironmentVariable("ConnectionStrings__SolanaOASIS");

                    if (!string.IsNullOrEmpty(privateKey))
                    {
                        oasisDNA.OASIS.StorageProviders.SolanaOASIS.PrivateKey = privateKey;
                        System.Console.WriteLine("🔧 Overridden SolanaOASIS PrivateKey from environment variable");
                    }

                    if (!string.IsNullOrEmpty(publicKey))
                    {
                        oasisDNA.OASIS.StorageProviders.SolanaOASIS.PublicKey = publicKey;
                        System.Console.WriteLine("🔧 Overridden SolanaOASIS PublicKey from environment variable");
                    }

                    if (!string.IsNullOrEmpty(mnemonicWords))
                    {
                        oasisDNA.OASIS.StorageProviders.SolanaOASIS.WalletMnemonicWords = mnemonicWords;
                        System.Console.WriteLine("🔧 Overridden SolanaOASIS WalletMnemonicWords from environment variable");
                    }

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        oasisDNA.OASIS.StorageProviders.SolanaOASIS.ConnectionString = connectionString;
                        System.Console.WriteLine("🔧 Overridden SolanaOASIS ConnectionString from environment variable");
                    }

                           System.Console.WriteLine($"✅ SolanaOASIS configuration override completed. Using wallet: {oasisDNA.OASIS.StorageProviders.SolanaOASIS.PublicKey}");
                       }
                   }
                   catch (System.Exception ex)
                   {
                       System.Console.WriteLine($"❌ Error overriding Solana configuration from environment variables: {ex.Message}");
                   }
               }

               private void RegisterSolanaOASISProvider()
               {
                   try
                   {
                       System.Console.WriteLine("🔧 Registering SolanaOASIS provider...");
                       
                       // Register the SolanaOASIS provider type
                       var registerResult = OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.SolanaOASIS);
                       if (registerResult != null)
                       {
                           System.Console.WriteLine("✅ SolanaOASIS provider registered successfully");
                       }
                       else
                       {
                           System.Console.WriteLine("ℹ️ SolanaOASIS provider registration returned null (may already be registered)");
                       }
                       
                       // Activate the SolanaOASIS provider
                       var activateResult = ProviderManager.Instance.ActivateProvider(ProviderType.SolanaOASIS);
                       if (activateResult != null && !activateResult.IsError)
                       {
                           System.Console.WriteLine("✅ SolanaOASIS provider activated successfully");
                       }
                       else
                       {
                           System.Console.WriteLine($"ℹ️ SolanaOASIS provider activation result: {(activateResult?.IsError == true ? activateResult.Message : "Provider may already be activated")}");
                       }
                   }
                   catch (System.Exception ex)
                   {
                       System.Console.WriteLine($"❌ Error registering/activating SolanaOASIS provider: {ex.Message}");
                   }
               }

               private string DetermineNetworkFromRequest()
               {
                   try
                   {
                       // First check environment variable
                       var envNetwork = System.Environment.GetEnvironmentVariable("OASIS_NETWORK");
                       if (!string.IsNullOrEmpty(envNetwork))
                       {
                           System.Console.WriteLine($"🔧 Using network from environment variable: {envNetwork}");
                           return envNetwork.ToLower();
                       }

                       // Check if we're running in a web context (HttpContext available)
                       // Note: In constructor, HttpContext might not be available yet
                       // This is a fallback for runtime detection
                       return "mainnet"; // Default to mainnet
                   }
                   catch (System.Exception ex)
                   {
                       System.Console.WriteLine($"❌ Error determining network: {ex.Message}");
                       return "mainnet"; // Default to mainnet
                   }
               }

               private string DetermineNetworkFromHostname(HttpContext context)
               {
                   try
                   {
                       // First check environment variable (highest priority)
                       var envNetwork = System.Environment.GetEnvironmentVariable("OASIS_NETWORK");
                       if (!string.IsNullOrEmpty(envNetwork))
                       {
                           System.Console.WriteLine($"🔧 Using network from OASIS_NETWORK env var for request: {envNetwork}");
                           return envNetwork.ToLower();
                       }
                       
                       var host = context.Request.Host.Host.ToLower();
                       System.Console.WriteLine($"🌐 Request hostname: {host}");
                       
                       // Check for localhost/127.0.0.1 (devnet for local development)
                       if (host == "localhost" || host == "127.0.0.1")
                       {
                           System.Console.WriteLine("🔧 Detected localhost - using devnet");
                           return "devnet";
                       }
                       
                       if (host.StartsWith("devnet."))
                       {
                           System.Console.WriteLine("🔧 Detected devnet subdomain");
                           return "devnet";
                       }
                       else
                       {
                           System.Console.WriteLine("🔧 Detected mainnet domain");
                           return "mainnet";
                       }
                   }
                   catch (System.Exception ex)
                   {
                       System.Console.WriteLine($"❌ Error determining network from hostname: {ex.Message}");
                       return "mainnet"; // Default to mainnet
                   }
               }

        public async Task Invoke(HttpContext context)
        {
            // Check if we need to switch networks based on the current request
            string requestNetwork = DetermineNetworkFromHostname(context);
            string currentNetwork = System.Environment.GetEnvironmentVariable("OASIS_CURRENT_NETWORK") ?? "mainnet";
            
            // If the request is for a different network, we need to handle this
            // For now, we'll log the detection but keep the current configuration
            // In a future enhancement, we could implement dynamic network switching
            if (requestNetwork != currentNetwork)
            {
                System.Console.WriteLine($"🔄 Network mismatch detected: Request={requestNetwork}, Current={currentNetwork}");
                System.Console.WriteLine($"ℹ️ Using current configuration ({currentNetwork}). Dynamic switching not implemented yet.");
            }
            
            //TODO: Try and make this more efficient, currently if they override provider in REST call and do not set Global flag to true then even if the next call is the same, it will switch back to default provider below and then have to switch back again to the override provider specified in the REST call...
            //if (!ProviderManager.IgnoreDefaultProviderTypes && ProviderManager.DefaultProviderTypes != null && ProviderManager.CurrentStorageProviderType != (ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]))
            //      ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default);

            await _next(context);
        }
    }
}