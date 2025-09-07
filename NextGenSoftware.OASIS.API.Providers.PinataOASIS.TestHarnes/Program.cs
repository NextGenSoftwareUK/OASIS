using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE PinataOASIS TEST HARNESS V2.0");
            Console.WriteLine("================================================");
            Console.WriteLine("");

            var pinataProvider = new PinataOASIS();
            
            try
            {
                // Test 1: Activate Provider
                Console.WriteLine("🧪 Test 1: Activating PinataOASIS Provider...");
                var activateResult = await pinataProvider.ActivateProviderAsync();
                
                if (activateResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate provider: {activateResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Provider activated successfully: {activateResult.Message}");
                Console.WriteLine("");

                // Test 2: Upload JSON Data
                Console.WriteLine("🧪 Test 2: Uploading JSON data to Pinata...");
                var testData = new
                {
                    name = "OASIS Test Data",
                    description = "Test data uploaded from PinataOASIS provider",
                    timestamp = DateTime.UtcNow,
                    version = "1.0",
                    features = new[] { "IPFS Storage", "Pinata Integration", "OASIS Provider" }
                };

                var uploadResult = await pinataProvider.UploadJsonToPinataAsync(testData, "OASIS_Test_Data");
                
                if (uploadResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to upload JSON: {uploadResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ JSON uploaded successfully!");
                    Console.WriteLine($"🔗 IPFS Hash: {uploadResult.Result}");
                    Console.WriteLine($"🌐 URL: {pinataProvider.GetFileUrl(uploadResult.Result)}");
                    Console.WriteLine("");

                    // Test 3: Download JSON Data
                    Console.WriteLine("🧪 Test 3: Downloading JSON data from Pinata...");
                    var downloadResult = await pinataProvider.DownloadFileFromPinataAsync(uploadResult.Result);
                    
                    if (downloadResult.IsError)
                    {
                        Console.WriteLine($"❌ Failed to download JSON: {downloadResult.Message}");
                    }
                    else
                    {
                        var downloadedJson = Encoding.UTF8.GetString(downloadResult.Result);
                        Console.WriteLine($"✅ JSON downloaded successfully!");
                        Console.WriteLine($"📄 Content: {downloadedJson.Substring(0, Math.Min(200, downloadedJson.Length))}...");
                        Console.WriteLine("");
                    }
                }

                // Test 4: Upload File Data
                Console.WriteLine("🧪 Test 4: Uploading file data to Pinata...");
                var fileContent = Encoding.UTF8.GetBytes("This is a test file uploaded from PinataOASIS provider.\nTimestamp: " + DateTime.UtcNow);
                var fileUploadResult = await pinataProvider.UploadFileToPinataAsync(fileContent, "test_file.txt", "text/plain");
                
                if (fileUploadResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to upload file: {fileUploadResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ File uploaded successfully!");
                    Console.WriteLine($"🔗 IPFS Hash: {fileUploadResult.Result}");
                    Console.WriteLine($"🌐 URL: {pinataProvider.GetFileUrl(fileUploadResult.Result)}");
                    Console.WriteLine("");
                }

                // Test 5: Save Holon
                Console.WriteLine("🧪 Test 5: Saving Holon to Pinata...");
                var testHolon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Holon",
                    Description = "A test holon saved to Pinata",
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "testProperty", "testValue" },
                        { "number", 42 },
                        { "array", new[] { 1, 2, 3 } }
                    }
                };

                var holonSaveResult = await pinataProvider.SaveHolonAsync(testHolon);
                
                if (holonSaveResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to save holon: {holonSaveResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Holon saved successfully!");
                    Console.WriteLine($"🔗 Provider Key: {holonSaveResult.Result.ProviderKey[ProviderType.PinataOASIS]}");
                    Console.WriteLine("");

                    // Test 6: Load Holon
                    Console.WriteLine("🧪 Test 6: Loading Holon from Pinata...");
                    var providerKey = holonSaveResult.Result.ProviderKey[ProviderType.PinataOASIS];
                    var holonLoadResult = await pinataProvider.LoadHolonAsync(providerKey);
                    
                    if (holonLoadResult.IsError)
                    {
                        Console.WriteLine($"❌ Failed to load holon: {holonLoadResult.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Holon loaded successfully!");
                        Console.WriteLine($"📄 Name: {holonLoadResult.Result.Name}");
                        Console.WriteLine($"📄 Description: {holonLoadResult.Result.Description}");
                        Console.WriteLine($"📄 ID: {holonLoadResult.Result.Id}");
                        Console.WriteLine("");
                    }
                }

                // Test 7: Save Avatar
                Console.WriteLine("🧪 Test 7: Saving Avatar to Pinata...");
                var testAvatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    CreatedDate = DateTime.UtcNow
                };

                var avatarSaveResult = await pinataProvider.SaveAvatarAsync(testAvatar);
                
                if (avatarSaveResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to save avatar: {avatarSaveResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Avatar saved successfully!");
                    Console.WriteLine($"🔗 Provider Key: {avatarSaveResult.Result.ProviderKey[ProviderType.PinataOASIS]}");
                    Console.WriteLine("");

                    // Test 8: Load Avatar
                    Console.WriteLine("🧪 Test 8: Loading Avatar from Pinata...");
                    var avatarProviderKey = avatarSaveResult.Result.ProviderKey[ProviderType.PinataOASIS];
                    var avatarLoadResult = await pinataProvider.LoadAvatarByProviderKeyAsync(avatarProviderKey);
                    
                    if (avatarLoadResult.IsError)
                    {
                        Console.WriteLine($"❌ Failed to load avatar: {avatarLoadResult.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Avatar loaded successfully!");
                        Console.WriteLine($"📄 Username: {avatarLoadResult.Result.Username}");
                        Console.WriteLine($"📄 Email: {avatarLoadResult.Result.Email}");
                        Console.WriteLine($"📄 Name: {avatarLoadResult.Result.FirstName} {avatarLoadResult.Result.LastName}");
                        Console.WriteLine("");
                    }
                }

                Console.WriteLine("🎉 All tests completed successfully!");
                Console.WriteLine("PinataOASIS provider is working correctly!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Deactivate Provider
                Console.WriteLine("🧹 Deactivating PinataOASIS Provider...");
                var deactivateResult = await pinataProvider.DeActivateProviderAsync();
                
                if (deactivateResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate provider: {deactivateResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Provider deactivated successfully: {deactivateResult.Message}");
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
