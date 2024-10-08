using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness
{
    internal static class Program
    {
        private static string hostUri = "https://api.devnet.solana.com";
        private static PublicKey publicKey = new("8pBKg1JCoa2cFAB9Bu5nLStaHzRADJujLVyjKEfDgbKT");
        private static PrivateKey privateKey = new("5VCRWersYAwwhzGoFB7ye82AXrjnY2Jn9v4FTr1wC2eGSfhSPRygBW6FLCAnQzXxZZBgDeb1roQ2uV74772eRV8R");

        private static async Task Run_SaveAndLoadAvatar()
        {
            SolanaOASIS solanaOasis = new(hostUri, privateKey.Key, publicKey.Key);
            Console.WriteLine("Run_SaveAndLoadAvatar()->ActivateProvider()");
            solanaOasis.ActivateProvider();

            Console.WriteLine("Run_SaveAndLoadAvatar()->SaveAvatarAsync()");
            var saveAvatarResult = await solanaOasis.SaveAvatarAsync(new Avatar()
            {
                Username = "@bob",
                Password = "P@ssw0rd!",
                Email = "bob@mail.ru",
                Id = Guid.NewGuid(),
                AvatarId = Guid.NewGuid()
            });

            if (saveAvatarResult.IsError)
            {
                Console.WriteLine(saveAvatarResult.Message);
                return;
            }

            await Task.Delay(5000);

            Console.WriteLine("Run_SaveAndLoadAvatar()->LoadAvatarAsync()");
            var transactionHashProviderKey = saveAvatarResult.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
            var loadAvatarResult = await solanaOasis.LoadAvatarByProviderKeyAsync(transactionHashProviderKey);

            if (loadAvatarResult.IsError)
            {
                Console.WriteLine(loadAvatarResult.Message);
                return;
            }

            Console.WriteLine("Avatar UserName: " + loadAvatarResult.Result.Username);
            Console.WriteLine("Avatar Password: " + loadAvatarResult.Result.Password);
            Console.WriteLine("Avatar Email: " + loadAvatarResult.Result.Email);
            Console.WriteLine("Avatar Id: " + loadAvatarResult.Result.Id);
            Console.WriteLine("Avatar AvatarId: " + loadAvatarResult.Result.AvatarId);

            Console.WriteLine("Run_SaveAndLoadAvatar()->DeActivateProvider()");
            solanaOasis.DeActivateProvider();
        }

        private static async Task Run_MintNFTAsync()
        {
            SolanaOASIS solanaOasis = new(hostUri, privateKey.Key, publicKey.Key);

            Console.WriteLine("Run_MintNFTAsync()->ActivateProvider()");
            solanaOasis.ActivateProvider();

            IMintNFTTransactionRequestForProvider mintNFTRequest = new MintNFTTransactionRequestForProvider()
            {
                JSONUrl = "https://example.com/metadata.json-lallalaall",
                Title = "Test NFT One more time!!!",
                Price = 2000,
                Symbol = "MUNFT",
                MintWalletAddress = publicKey.Key
            };

            Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Sending...");
            var mintNftResult = await solanaOasis.MintNFTAsync(mintNFTRequest);
            if (mintNftResult.IsError)
            {
                Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Failed...");
                Console.WriteLine(mintNftResult.Message);
                return;
            }
            Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Completed...");

            Console.WriteLine("Run_MintNFTAsync()->DeActivateProvider()");
            solanaOasis.DeActivateProvider();
        }

        private static async Task Main()
        {
            // Transferring Examples
            await Run_MintNFTAsync();

            // Solana Provider Examples
            // await Run_SaveAndLoadAvatar();
        }
    }
}