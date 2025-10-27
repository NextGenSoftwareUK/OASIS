using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 SOLID OASIS Provider TestHarness");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            // Initialize the SOLID provider
            var solidProvider = new SOLIDOASIS();
            
            try
            {
                // Activate the provider
                Console.WriteLine("🔌 Activating SOLID Provider...");
                var result = await solidProvider.ActivateProviderAsync();
                Console.WriteLine($"✅ Provider Activated: {result.IsSuccess}");
                Console.WriteLine();

                if (result.IsSuccess)
                {
                    // Test Avatar operations
                    Console.WriteLine("👤 Testing Avatar Operations...");
                    
                    // Create a test avatar
                    var avatar = new Avatar
                    {
                        Username = "testuser",
                        Email = "test@example.com",
                        FirstName = "Test",
                        LastName = "User"
                    };

                    // Save avatar
                    Console.WriteLine("💾 Saving Avatar...");
                    var saveResult = await solidProvider.SaveAvatarAsync(avatar);
                    Console.WriteLine($"✅ Avatar Saved: {saveResult.IsSuccess}");
                    if (saveResult.IsSuccess)
                        Console.WriteLine($"📝 Avatar ID: {saveResult.Result.Id}");

                    // Load avatar
                    Console.WriteLine("📖 Loading Avatar...");
                    var loadResult = await solidProvider.LoadAvatarAsync(avatar.Id);
                    Console.WriteLine($"✅ Avatar Loaded: {loadResult.IsSuccess}");
                    if (loadResult.IsSuccess)
                        Console.WriteLine($"👤 Username: {loadResult.Result.Username}");

                    // Test Holon operations
                    Console.WriteLine("🔗 Testing Holon Operations...");
                    
                    var holon = new Holon
                    {
                        Name = "Test Holon",
                        Description = "A test holon for SOLID storage"
                    };

                    // Save holon
                    Console.WriteLine("💾 Saving Holon...");
                    var saveHolonResult = await solidProvider.SaveHolonAsync(holon);
                    Console.WriteLine($"✅ Holon Saved: {saveHolonResult.IsSuccess}");
                    if (saveHolonResult.IsSuccess)
                        Console.WriteLine($"📝 Holon ID: {saveHolonResult.Result.Id}");

                    // Load holon
                    Console.WriteLine("📖 Loading Holon...");
                    var loadHolonResult = await solidProvider.LoadHolonAsync(holon.Id);
                    Console.WriteLine($"✅ Holon Loaded: {loadHolonResult.IsSuccess}");
                    if (loadHolonResult.IsSuccess)
                        Console.WriteLine($"🔗 Holon Name: {loadHolonResult.Result.Name}");
                }

                // Deactivate provider
                Console.WriteLine("🔌 Deactivating SOLID Provider...");
                var deactivateResult = await solidProvider.DeActivateProviderAsync();
                Console.WriteLine($"✅ Provider Deactivated: {deactivateResult.IsSuccess}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("🏁 TestHarness Complete!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}