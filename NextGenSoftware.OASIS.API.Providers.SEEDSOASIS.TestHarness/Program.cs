using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            string privateKey = ""; //Set to privatekey when testing BUT remember to remove again before checking in code! Better to use avatar methods so private key is retreived from avatar and then no need to pass them in.
            Console.WriteLine("NEXTGEN SOFTWARE SEEDSOASIS TEST HARNESS V1.2");
            Console.WriteLine("");

            SEEDSOASIS seedsOASIS = new SEEDSOASIS(new TelosOASIS.TelosOASIS("https://node.hypha.earth", "", "", ""));

            // Will initialize the default OASIS Provider defined OASIS_DNA config file.
            OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result; //TODO: TEMP - Take out once EOSIOOASIS has rest of AvatarManager methods implemented.

            Console.WriteLine("Getting Balance for account davidsellams...");
            string balance = seedsOASIS.GetBalanceForTelosAccount("davidsellams");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Balance for account nextgenworld...");
            balance = seedsOASIS.GetBalanceForTelosAccount("nextgenworld");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Account for account davidsellams...");
            GetAccountResponseDto account = seedsOASIS.TelosOASIS.GetTelosAccount("davidsellams");
            Console.WriteLine(string.Concat("Account.account_name: ", account.AccountName));
            Console.WriteLine(string.Concat("Account.created: ", account.Created.ToString()));

            Console.WriteLine("Getting Account for account nextgenworld...");
            account = seedsOASIS.TelosOASIS.GetTelosAccount("nextgenworld");
            Console.WriteLine(string.Concat("Account.account_name: ", account.AccountName));
            Console.WriteLine(string.Concat("Account.created: ", account.Created.ToString()));

            AvatarManager avatarManager = new AvatarManager(ProviderManager.CurrentStorageProvider);
            var loadAvatarResult = avatarManager.LoadAvatar("davidellams@hotmail.com");

            // Check that the Telos account name is linked to the avatar and link it if it is not (PayWithSeeds will fail if it is not linked when it tries to add the karma points).
            // if (!loadAvatarResult.Result.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.TelosOASIS))
            //     KeyManager.Instance.LinkProviderPrivateKeyToAvatar(loadAvatarResult);

            Console.WriteLine("Sending SEEDS from nextgenworld to davidsellams...");
            OASISResult<string> payResult = seedsOASIS.PayWithSeedsUsingTelosAccount("davidsellams", privateKey, "nextgenworld",  1, Core.Enums.KarmaSourceType.API, "test", "test", "test", "test memo");
            Console.WriteLine(string.Concat("Success: ", result.IsError ? "false" : "true"));

            if (result.IsError)
                Console.WriteLine(string.Concat("Error Message: ", result.Message));

            Console.WriteLine(string.Concat("Result: ", result.Result));
            
            Console.WriteLine("Getting Balance for account davidsellams...");
            balance = seedsOASIS.GetBalanceForTelosAccount("davidsellams");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Balance for account nextgenworld...");
            balance = seedsOASIS.GetBalanceForTelosAccount("nextgenworld");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Organsiations...");
            string orgs = seedsOASIS.GetAllOrganisationsAsJSON();
            Console.WriteLine(string.Concat("Organisations: ", orgs));

            //Console.WriteLine("Getting nextgenworld organsiation...");
            //string org = seedsOASIS.GetOrganisation("nextgenworld");
            //Console.WriteLine(string.Concat("nextgenworld org: ", org));

            Console.WriteLine("Generating QR Code for davidsellams...");
            string qrCode = seedsOASIS.GenerateSignInQRCode("davidsellams");
            Console.WriteLine(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            Console.WriteLine("Sending invite to davidsellams...");
            OASISResult<SendInviteResult> sendInviteResult = seedsOASIS.SendInviteToJoinSeedsUsingTelosAccount("davidsellams", privateKey, "davidsellams",1 , 1, Core.Enums.KarmaSourceType.API, "test", "test", "test");
            Console.WriteLine(string.Concat("Success: ", sendInviteResult.IsError ? "false" : "true"));

            if (sendInviteResult.IsError)
                Console.WriteLine(string.Concat("Error Message: ", sendInviteResult.Message));
            else
            {
                Console.WriteLine(string.Concat("Invite Sent To Join SEEDS. Invite Secret: ", sendInviteResult.Result.InviteSecret, ". Transction ID: ", sendInviteResult.Result.TransactionId));

                Console.WriteLine("Accepting invite to davidsellams...");
                OASISResult<string> acceptInviteResult = seedsOASIS.AcceptInviteToJoinSeedsUsingTelosAccount("davidsellams", sendInviteResult.Result.InviteSecret, Core.Enums.KarmaSourceType.API, "test", "test", "test");
                Console.WriteLine(string.Concat("Success: ", acceptInviteResult.IsError ? "false" : "true"));

                if (acceptInviteResult.IsError)
                    Console.WriteLine(string.Concat("Error Message: ", acceptInviteResult.Message));
                else
                    Console.WriteLine(string.Concat("Invite Accepted To Join SEEDS. Transction ID: ", acceptInviteResult.Result));
            }

            Console.ReadKey();
        }
    }
}
