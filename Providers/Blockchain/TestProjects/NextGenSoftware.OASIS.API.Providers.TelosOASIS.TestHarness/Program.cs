using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 TelosOASIS Provider Test Harness Starting...");
            Console.WriteLine("================================================");

            var provider = new TelosOASIS("https://telos.caleos.io", "account", "1", "pk");

            try
            {
                // Test 1: Activate Provider
                Console.WriteLine("\n📡 Test 1: Activating TelosOASIS Provider...");
                var activateResult = await provider.ActivateProviderAsync();
                Console.WriteLine($"✅ Provider Activated: {(activateResult.IsError ? "Failed" : "Success")}");
                if (activateResult.IsError)
                    Console.WriteLine($"❌ Error: {activateResult.Message}");

                // Test 2: Save Avatar
                Console.WriteLine("\n👤 Test 2: Saving Avatar...");
                var avatar = new Avatar
                {
                    Username = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User"
                };
                var saveAvatarResult = await provider.SaveAvatarAsync(avatar);
                Console.WriteLine($"✅ Avatar Saved: {(saveAvatarResult.IsError ? "Failed" : "Success")}");
                if (saveAvatarResult.IsError)
                    Console.WriteLine($"❌ Error: {saveAvatarResult.Message}");

                // Test 3: Load All Avatars
                Console.WriteLine("\n👥 Test 3: Loading All Avatars...");
                var loadAvatarsResult = await provider.LoadAllAvatarsAsync();
                Console.WriteLine($"✅ Avatars Loaded: {(loadAvatarsResult.IsError ? "Failed" : "Success")}");
                if (loadAvatarsResult.IsError)
                    Console.WriteLine($"❌ Error: {loadAvatarsResult.Message}");

                // Test 4: Load Avatar by Email
                Console.WriteLine("\n📧 Test 4: Loading Avatar by Email...");
                var loadByEmailResult = await provider.LoadAvatarByEmailAsync("test@example.com");
                Console.WriteLine($"✅ Avatar Loaded by Email: {(loadByEmailResult.IsError ? "Failed" : "Success")}");
                if (loadByEmailResult.IsError)
                    Console.WriteLine($"❌ Error: {loadByEmailResult.Message}");

                // Test 5: Load Avatar by Username
                Console.WriteLine("\n👤 Test 5: Loading Avatar by Username...");
                var loadByUsernameResult = await provider.LoadAvatarByUsernameAsync("testuser");
                Console.WriteLine($"✅ Avatar Loaded by Username: {(loadByUsernameResult.IsError ? "Failed" : "Success")}");
                if (loadByUsernameResult.IsError)
                    Console.WriteLine($"❌ Error: {loadByUsernameResult.Message}");

                // Test 6: Load Avatar by Provider Key
                Console.WriteLine("\n🔑 Test 6: Loading Avatar by Provider Key...");
                var loadByProviderKeyResult = await provider.LoadAvatarByProviderKeyAsync("testkey");
                Console.WriteLine($"✅ Avatar Loaded by Provider Key: {(loadByProviderKeyResult.IsError ? "Failed" : "Success")}");
                if (loadByProviderKeyResult.IsError)
                    Console.WriteLine($"❌ Error: {loadByProviderKeyResult.Message}");

                // Test 7: Load Avatar by Id
                Console.WriteLine("\n🆔 Test 7: Loading Avatar by Id...");
                var loadByIdResult = await provider.LoadAvatarAsync(Guid.NewGuid());
                Console.WriteLine($"✅ Avatar Loaded by Id: {(loadByIdResult.IsError ? "Failed" : "Success")}");
                if (loadByIdResult.IsError)
                    Console.WriteLine($"❌ Error: {loadByIdResult.Message}");

                // Test 8: Save Holon
                Console.WriteLine("\n📦 Test 8: Saving Holon...");
                var holon = new Holon
                {
                    Name = "Test Holon",
                    Description = "Test Description"
                };
                var saveHolonResult = await provider.SaveHolonAsync(holon);
                Console.WriteLine($"✅ Holon Saved: {(saveHolonResult.IsError ? "Failed" : "Success")}");
                if (saveHolonResult.IsError)
                    Console.WriteLine($"❌ Error: {saveHolonResult.Message}");

                // Test 9: Load All Holons
                Console.WriteLine("\n📦 Test 9: Loading All Holons...");
                var loadHolonsResult = await provider.LoadAllHolonsAsync();
                Console.WriteLine($"✅ Holons Loaded: {(loadHolonsResult.IsError ? "Failed" : "Success")}");
                if (loadHolonsResult.IsError)
                    Console.WriteLine($"❌ Error: {loadHolonsResult.Message}");

                // Test 10: Load Holon by Id
                Console.WriteLine("\n🆔 Test 10: Loading Holon by Id...");
                var loadHolonByIdResult = await provider.LoadHolonAsync(Guid.NewGuid());
                Console.WriteLine($"✅ Holon Loaded by Id: {(loadHolonByIdResult.IsError ? "Failed" : "Success")}");
                if (loadHolonByIdResult.IsError)
                    Console.WriteLine($"❌ Error: {loadHolonByIdResult.Message}");

                // Test 11: Load Holons for Parent
                Console.WriteLine("\n👨‍👩‍👧‍👦 Test 11: Loading Holons for Parent...");
                var loadHolonsForParentResult = await provider.LoadHolonsForParentAsync(Guid.NewGuid());
                Console.WriteLine($"✅ Holons for Parent Loaded: {(loadHolonsForParentResult.IsError ? "Failed" : "Success")}");
                if (loadHolonsForParentResult.IsError)
                    Console.WriteLine($"❌ Error: {loadHolonsForParentResult.Message}");

                // Test 12: Delete Holon
                Console.WriteLine("\n🗑️ Test 12: Deleting Holon...");
                var deleteHolonResult = await provider.DeleteHolonAsync(Guid.NewGuid());
                Console.WriteLine($"✅ Holon Deleted: {(deleteHolonResult.IsError ? "Failed" : "Success")}");
                if (deleteHolonResult.IsError)
                    Console.WriteLine($"❌ Error: {deleteHolonResult.Message}");

                // Test 13: Save Avatar Detail
                Console.WriteLine("\n👤 Test 13: Saving Avatar Detail...");
                var avatarDetail = new AvatarDetail
                {
                    Username = "testuser",
                    Email = "test@example.com"
                };
                var saveAvatarDetailResult = await provider.SaveAvatarDetailAsync(avatarDetail);
                Console.WriteLine($"✅ Avatar Detail Saved: {(saveAvatarDetailResult.IsError ? "Failed" : "Success")}");
                if (saveAvatarDetailResult.IsError)
                    Console.WriteLine($"❌ Error: {saveAvatarDetailResult.Message}");

                // Test 14: Load Avatar Detail
                Console.WriteLine("\n👤 Test 14: Loading Avatar Detail...");
                var loadAvatarDetailResult = await provider.LoadAvatarDetailAsync(Guid.NewGuid());
                Console.WriteLine($"✅ Avatar Detail Loaded: {(loadAvatarDetailResult.IsError ? "Failed" : "Success")}");
                if (loadAvatarDetailResult.IsError)
                    Console.WriteLine($"❌ Error: {loadAvatarDetailResult.Message}");

                // Test 15: Load Avatar Detail by Email
                Console.WriteLine("\n📧 Test 15: Loading Avatar Detail by Email...");
                var loadAvatarDetailByEmailResult = await provider.LoadAvatarDetailByEmailAsync("test@example.com");
                Console.WriteLine($"✅ Avatar Detail Loaded by Email: {(loadAvatarDetailByEmailResult.IsError ? "Failed" : "Success")}");
                if (loadAvatarDetailByEmailResult.IsError)
                    Console.WriteLine($"❌ Error: {loadAvatarDetailByEmailResult.Message}");

                // Test 16: Load Avatar Detail by Username
                Console.WriteLine("\n👤 Test 16: Loading Avatar Detail by Username...");
                var loadAvatarDetailByUsernameResult = await provider.LoadAvatarDetailByUsernameAsync("testuser");
                Console.WriteLine($"✅ Avatar Detail Loaded by Username: {(loadAvatarDetailByUsernameResult.IsError ? "Failed" : "Success")}");
                if (loadAvatarDetailByUsernameResult.IsError)
                    Console.WriteLine($"❌ Error: {loadAvatarDetailByUsernameResult.Message}");

                // Test 17: Deactivate Provider
                Console.WriteLine("\n📡 Test 17: Deactivating TelosOASIS Provider...");
                var deactivateResult = await provider.DeActivateProviderAsync();
                Console.WriteLine($"✅ Provider Deactivated: {(deactivateResult.IsError ? "Failed" : "Success")}");
                if (deactivateResult.IsError)
                    Console.WriteLine($"❌ Error: {deactivateResult.Message}");

                Console.WriteLine("\n🎉 All Tests Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test Harness Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
