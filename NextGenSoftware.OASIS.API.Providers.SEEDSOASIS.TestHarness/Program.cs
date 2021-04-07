using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.ParamObjects;
using System;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE SEEDSOASIS TEST HARNESS V1.1");
            Console.WriteLine("");

            SEEDSOASIS seedsOASIS = new SEEDSOASIS(new EOSIOOASIS.EOSIOOASIS("https://node.hypha.earth"));
            OASISDNAManager.GetAndActivateProvider(); //TODO: TEMP - Take out once EOSIOOASIS has rest of AvatarManager methods implemented.

            Console.WriteLine("Getting Balance for account davidsellams...");
            string balance = seedsOASIS.GetBalanceForTelosAccount("davidsellams");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Account for account davidsellams...");
            Account account = seedsOASIS.EOSIOOASIS.GetEOSIOAccount("davidsellams");
            Console.WriteLine(string.Concat("Account.account_name: ", account.account_name));
            Console.WriteLine(string.Concat("Account.created: ", account.created));
            Console.WriteLine(string.Concat("Account.created: ", account.created_datetime.ToString()));
            Console.WriteLine(string.Concat("Account.created: ", account.created_datetime.ToShortDateString()));
            Console.WriteLine(string.Concat("Account.core_liquid_balance: ", account.core_liquid_balance));

            Console.WriteLine("Sending SEEDS to davidsellams...");
            OASISResult<string> result = seedsOASIS.PayWithSeedsUsingTelosAccount("davidsellams", "davidsellams", 1, Core.Enums.KarmaSourceType.API, "test", "test", "test", "test memo");
            Console.WriteLine(string.Concat("Success: ", result.IsError ? "false" : "true"));

            if (result.IsError)
                Console.WriteLine(string.Concat("Error Message: ", result.ErrorMessage));

            Console.WriteLine(string.Concat("Result: ", result.Result));
            
            Console.WriteLine("Getting Balance for account davidsellams...");
            balance = seedsOASIS.GetBalanceForTelosAccount("davidsellams");
            Console.WriteLine(string.Concat("Balance: ", balance));

            Console.WriteLine("Getting Organsiations...");
            string orgs = seedsOASIS.GetAllOrganisationsAsJSON();
            Console.WriteLine(string.Concat("Organisations: ", orgs));

            Console.WriteLine("Generating QR Code for davidsellams...");
            string qrCode = seedsOASIS.GenerateSignInQRCode("davidsellams");
            Console.WriteLine(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            Console.WriteLine("Sending invite to davidsellams...");
            OASISResult<SendInviteResult> sendInviteResult = seedsOASIS.SendInviteToJoinSeedsUsingTelosAccount("davidsellams", "davidsellams", 5, 5, Core.Enums.KarmaSourceType.API, "test", "test", "test");
            Console.WriteLine(string.Concat("Success: ", sendInviteResult.IsError ? "false" : "true"));

            if (sendInviteResult.IsError)
                Console.WriteLine(string.Concat("Error Message: ", sendInviteResult.ErrorMessage));
            else
            {
                Console.WriteLine(string.Concat("Invite Sent To Join SEEDS. Invite Secret: ", sendInviteResult.Result.InviteSecret, ". Transction ID: ", sendInviteResult.Result.TransactionId));

                Console.WriteLine("Accepting invite to davidsellams...");
                OASISResult<string> acceptInviteResult = seedsOASIS.AcceptInviteToJoinSeedsUsingTelosAccount("davidsellams", sendInviteResult.Result.InviteSecret, Core.Enums.KarmaSourceType.API, "test", "test", "test");
                Console.WriteLine(string.Concat("Success: ", acceptInviteResult.IsError ? "false" : "true"));

                if (acceptInviteResult.IsError)
                    Console.WriteLine(string.Concat("Error Message: ", acceptInviteResult.ErrorMessage));
                else
                    Console.WriteLine(string.Concat("Invite Accepted To Join SEEDS. Transction ID: ", acceptInviteResult.Result));
            }

            Console.ReadKey();
        }
    }
}
