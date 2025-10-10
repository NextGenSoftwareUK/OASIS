using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("TELEGRAM OASIS TEST HARNESS V1.0");
            Console.WriteLine("=================================");
            Console.WriteLine("");

            try
            {
                // Configuration for Telegram Provider
                string botToken = "7927576561:AAEFHa3k1t6kj0t6wOu6QtU61KRsNxOoeMo";
                string webhookUrl = "https://oasisweb4.one/api/telegram/webhook";
                string mongoConnectionString = "mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4";

                // Initialize provider
                Console.WriteLine("Initializing TelegramOASIS provider...");
                var provider = new TelegramOASIS(botToken, webhookUrl, mongoConnectionString);

                // Test provider activation
                Console.WriteLine("Testing provider activation...");
                var activationResult = provider.ActivateProvider();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Activation failed: {activationResult.Message}");
                    if (activationResult.Exception != null)
                    {
                        Console.WriteLine($"Exception: {activationResult.Exception}");
                    }
                    return;
                }
                
                Console.WriteLine("✅ Provider activated successfully");
                Console.WriteLine($"Provider Name: {provider.ProviderName}");
                Console.WriteLine($"Provider Description: {provider.ProviderDescription}");
                Console.WriteLine("");

                // Run all tests
                await TestAvatarLinking(provider);
                await TestGroupOperations(provider);
                await TestAchievementOperations(provider);
                await TestTelegramMessaging(provider);

                // Test cleanup
                Console.WriteLine("Testing provider deactivation...");
                var deactivationResult = provider.DeActivateProvider();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Deactivation failed: {deactivationResult.Message}");
                }
                else
                {
                    Console.WriteLine("✅ Provider deactivated successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test harness error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("");
            Console.WriteLine("Test harness completed. Press any key to exit...");
            Console.ReadKey();
        }

        static async Task TestAvatarLinking(TelegramOASIS provider)
        {
            Console.WriteLine("--- Testing Avatar Linking ---");
            
            try
            {
                // Test data
                var testAvatarId = Guid.NewGuid();
                long testTelegramId = 123456789;
                string testUsername = "test_user_" + DateTime.Now.Ticks;

                // Test linking Telegram to Avatar
                Console.WriteLine($"Linking Telegram user @{testUsername} (telegramId: {testTelegramId}) to avatar {testAvatarId}...");
                var linkResult = await provider.LinkTelegramToAvatarAsync(testTelegramId, testUsername, "Test", "User", testAvatarId);
                
                if (linkResult.IsError)
                {
                    Console.WriteLine($"❌ Linking failed: {linkResult.Message}");
                    return;
                }
                
                Console.WriteLine("✅ Avatar linked successfully");

                // Test retrieving linked avatar
                Console.WriteLine($"Retrieving avatar by Telegram ID {testTelegramId}...");
                var getAvatarResult = await provider.GetTelegramAvatarByTelegramIdAsync(testTelegramId);
                
                if (getAvatarResult.IsError)
                {
                    Console.WriteLine($"❌ Retrieval failed: {getAvatarResult.Message}");
                    return;
                }
                
                if (getAvatarResult.Result != null)
                {
                    Console.WriteLine($"✅ Avatar retrieved: ID={getAvatarResult.Result.Id}, Username={getAvatarResult.Result.TelegramUsername}");
                }
                else
                {
                    Console.WriteLine("⚠️  No avatar found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Avatar linking test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }

        static async Task TestGroupOperations(TelegramOASIS provider)
        {
            Console.WriteLine("--- Testing Group Operations ---");
            
            try
            {
                var testCreatorId = Guid.NewGuid();
                long testTelegramChatId = -1001234567890; // Example Telegram group chat ID
                string testGroupName = "Test Accountability Group " + DateTime.Now.Ticks;
                string testDescription = "This is a test group for accountability buddies";

                // Test creating a group
                Console.WriteLine($"Creating group '{testGroupName}'...");
                var createResult = await provider.CreateGroupAsync(
                    testGroupName,
                    testDescription,
                    testCreatorId,
                    testTelegramChatId
                );
                
                if (createResult.IsError)
                {
                    Console.WriteLine($"❌ Group creation failed: {createResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Group created: ID={createResult.Result.Id}");

                // Test retrieving the group
                Console.WriteLine($"Retrieving group {createResult.Result.Id}...");
                var getGroupResult = await provider.GetGroupAsync(createResult.Result.Id.ToString());
                
                if (getGroupResult.IsError)
                {
                    Console.WriteLine($"❌ Group retrieval failed: {getGroupResult.Message}");
                    return;
                }
                
                if (getGroupResult.Result != null)
                {
                    Console.WriteLine($"✅ Group retrieved: Name={getGroupResult.Result.Name}, Description={getGroupResult.Result.Description}");
                }

                // Test getting all groups for user
                Console.WriteLine($"Getting all groups for user {testCreatorId}...");
                var getAllResult = await provider.GetUserGroupsAsync(testTelegramChatId);
                
                if (getAllResult.IsError)
                {
                    Console.WriteLine($"❌ Get all groups failed: {getAllResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Found {getAllResult.Result.Count} groups for user");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Group operations test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }

        static async Task TestAchievementOperations(TelegramOASIS provider)
        {
            Console.WriteLine("--- Testing Achievement Operations ---");
            
            try
            {
                var testUserId = Guid.NewGuid();
                var testGroupId = Guid.NewGuid();

                // Test creating an achievement
                Console.WriteLine("Creating test achievement...");
                var achievement = new Achievement
                {
                    UserId = testUserId,
                    GroupId = testGroupId.ToString(),
                    TelegramUserId = 123456789,
                    Description = "Complete first project milestone",
                    Type = AchievementType.Manual,
                    KarmaReward = 100,
                    TokenReward = 50.0m,
                    Status = AchievementStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await provider.CreateAchievementAsync(achievement);
                
                if (createResult.IsError)
                {
                    Console.WriteLine($"❌ Achievement creation failed: {createResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Achievement created: {achievement.Description}");

                // Test retrieving achievements for user
                Console.WriteLine($"Getting achievements for user {testUserId}...");
                var getUserAchievements = await provider.GetUserAchievementsAsync(testUserId);
                
                if (getUserAchievements.IsError)
                {
                    Console.WriteLine($"❌ Get user achievements failed: {getUserAchievements.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Found {getUserAchievements.Result.Count} achievements for user");
                }

                // Test retrieving achievements for group
                Console.WriteLine($"Getting achievements for group {testGroupId}...");
                var getGroupAchievements = await provider.GetGroupAchievementsAsync(testGroupId.ToString());
                
                if (getGroupAchievements.IsError)
                {
                    Console.WriteLine($"❌ Get group achievements failed: {getGroupAchievements.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Found {getGroupAchievements.Result.Count} achievements for group");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Achievement operations test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }

        static async Task TestTelegramMessaging(TelegramOASIS provider)
        {
            Console.WriteLine("--- Testing Telegram Messaging ---");
            
            try
            {
                // Note: This will only work if you have a valid chat ID from an actual Telegram user
                // who has started a conversation with your bot
                Console.WriteLine("⚠️  Messaging test skipped - requires real Telegram chat ID");
                Console.WriteLine("   To test: Start a chat with your bot and use a real chat ID");
                
                // Example code for when you have a real chat ID:
                // long realChatId = YOUR_CHAT_ID_HERE;
                // string testMessage = "Hello from TelegramOASIS Test Harness!";
                // var sendResult = await provider.SendTelegramMessage(realChatId, testMessage);
                // 
                // if (sendResult.IsError)
                // {
                //     Console.WriteLine($"❌ Message send failed: {sendResult.Message}");
                // }
                // else
                // {
                //     Console.WriteLine("✅ Message sent successfully");
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Telegram messaging test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }
    }
}

