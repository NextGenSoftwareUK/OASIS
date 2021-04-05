using EOSNewYork.EOSCore.Response.API;
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

            string balance = seedsOASIS.GetBalanceForTelosAccount("davidsellams");
            Account account = seedsOASIS.EOSIOOASIS.GetEOSAccount("davidsellams");

            Console.WriteLine(string.Concat("Balance Before: ", balance));
            Console.WriteLine(string.Concat("Account.account_name: ", account.account_name));
            Console.WriteLine(string.Concat("Account.core_liquid_balance Before: ", account.core_liquid_balance));

            seedsOASIS.PayWithSeedsUsingTelosAccount("test.account", "test.account2", 7, Core.Enums.KarmaSourceType.API, "test", "test", "test", "test memo");
            balance = seedsOASIS.GetBalanceForTelosAccount("test.account");
            account = seedsOASIS.EOSIOOASIS.GetEOSAccount("test.account");

            Console.WriteLine(string.Concat("Balance After: ", balance));
            Console.WriteLine(string.Concat("Account.core_liquid_balance After: ", account.core_liquid_balance));

            string orgs = seedsOASIS.GetAllOrganisationsAsJSON();
            Console.WriteLine(string.Concat("Organisations: ", orgs));

            string qrCode = seedsOASIS.GenerateSignInQRCode("davidsellams");
            Console.WriteLine(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            SendInviteResult result = seedsOASIS.SendInviteToJoinSeedsUsingTelosAccount("test.account", "test.account", 5, 5, Core.Enums.KarmaSourceType.API, "test", "test", "test");
            Console.WriteLine(string.Concat("Invite Sent To Join SEEDS. Invite Secrert: ", result.InviteSecret, ". Transction ID: ", result.TransactionId));

            string transactionID = seedsOASIS.AcceptInviteToJoinSeedsUsingTelosAccount("test.account2", result.InviteSecret, Core.Enums.KarmaSourceType.API, "test", "test", "test");
            Console.WriteLine(string.Concat("Invite Accepted To Join SEEDS. Invite Secrert: ", result.InviteSecret, ". Transction ID: ", transactionID));

            Console.ReadKey();
        }
    }
}
