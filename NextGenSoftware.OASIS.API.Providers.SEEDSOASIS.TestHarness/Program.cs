using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.ParamObjects;
using System;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE SEEDSOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            SEEDSManager seedsManager = new SEEDSManager();

             string balance = seedsManager.GetBalance("davidsellams");
             Account account = seedsManager.GetUser("davidsellams");

            Console.WriteLine(string.Concat("Balance Before: ", balance));
            Console.WriteLine(string.Concat("Account.account_name: ", account.account_name));
            Console.WriteLine(string.Concat("Account.core_liquid_balance Before: ", account.core_liquid_balance));

            seedsManager.PayWithSeeds("test.account", "test.account2", 7, "test memo");
            balance = seedsManager.GetBalance("test.account");
            account = seedsManager.GetUser("test.account");

            Console.WriteLine(string.Concat("Balance After: ", balance));
            Console.WriteLine(string.Concat("Account.core_liquid_balance After: ", account.core_liquid_balance));

            string orgs = seedsManager.GetAllOrganisationsAsJSON();
            Console.WriteLine(string.Concat("Organisations: ", orgs));

            string qrCode = seedsManager.GenerateSignInQRCode();
            Console.WriteLine(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            SendInviteResult result = seedsManager.SendInviteToJoinSeeds("test.account", "test.account", 5, 5);
            Console.WriteLine(string.Concat("Invite Sent To Join SEEDS. Invite Secrert: ", result.InviteSecret, ". Transction ID: ", result.TransactionId));

            string transactionID = seedsManager.AcceptInviteToJoinSeeds("test.account2", result.InviteSecret);
            Console.WriteLine(string.Concat("Invite Accepted To Join SEEDS. Invite Secrert: ", result.InviteSecret, ". Transction ID: ", transactionID));

            Console.ReadKey();
        }
    }
}
