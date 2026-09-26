using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MonadOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MonadOASIS.TestHarness
{
    /// <summary>
    /// Console harness: activates the provider, reports its metadata, deactivates.
    /// Configure with MONADOASIS_&lt;PARAM&gt; environment variables.
    /// </summary>
    public static class MonadOASISTestHarness
    {
        public static async Task Main()
        {
            Console.WriteLine("MonadOASIS Test Harness");
            Console.WriteLine(new string('-', 60));

            var provider = MonadOASISTestFactory.Create();

            Console.WriteLine($"      name    : {provider.ProviderName}");
            Console.WriteLine($"      type    : {provider.ProviderType.Value}");

            var activated = await provider.ActivateProviderAsync();
            Console.WriteLine(activated.IsError
                ? $"FAIL  activate: {activated.Message}"
                : "PASS  activate");

            if (!activated.IsError)
            {
                var deactivated = await provider.DeActivateProviderAsync();
                Console.WriteLine(deactivated.IsError ? $"FAIL  deactivate: {deactivated.Message}" : "PASS  deactivate");
            }
            else
            {
                Console.WriteLine($"\nSet the MONADOASIS_* environment variables and try again.");
            }

            Console.WriteLine(new string('-', 60));
            Console.WriteLine("Done.");
        }
    }
}
