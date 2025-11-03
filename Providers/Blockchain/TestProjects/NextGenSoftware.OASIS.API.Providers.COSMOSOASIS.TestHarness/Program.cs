using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CMOSBCOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS V1.1");
            Console.WriteLine("");

            string hostUrl = "http://localhost:5001";
            CosmosBlockChainOASIS.CosmosBlockChainOASIS cosmos = new CosmosBlockChainOASIS.CosmosBlockChainOASIS(hostUrl);
            cosmos.ActivateProvider();


            Avatar a = new Avatar();
            a.Username = "farid";
            a.Password = "man";
            a.Email = "rttyu@test.com";
            a.FirstName = "znnn";

            cosmos.SaveAvatar(a);

            Avatar b = new Avatar();
            b.Username = "qqq";
            b.Password = "ssss";
            b.Email = "zzz@test.com";
            b.FirstName = "xxxwqs";

            cosmos.SaveAvatar(b);

            IEnumerable<IAvatar> avatars = await cosmos.LoadAllAvatarsAsync();

            Avatar avatar = (Avatar)await cosmos.LoadAvatarAsync("farid", "man");

            bool isdeleted = cosmos.DeleteAvatarByUsername("qqq");

            cosmos.DeActivateProvider();

            //  Console.WriteLine("Output: " + ci.Id);


        }
    }
}
