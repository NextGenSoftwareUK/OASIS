using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS V1.1");
            Console.WriteLine("");

            IPFSOASIS ipfs = new IPFSOASIS(); 
            ipfs.ActivateProvider();
     

            Avatar a = new Avatar();
            a.Username = "farid";
            a.Password = "man";
            a.Email = "rttyu@test.com";
            a.FirstName = "znnn";

            ipfs.SaveAvatar(a);

            Avatar b = new Avatar();
            b.Username = "qqq";
            b.Password = "ssss";
            b.Email = "zzz@test.com";
            b.FirstName = "xxxwqs";

            ipfs.SaveAvatar(b);

            IEnumerable<IAvatar> avatars = await ipfs.LoadAllAvatarsAsync();

            Avatar avatar = (Avatar)await ipfs.LoadAvatarAsync("farid", "man");

            bool isdeleted = ipfs.DeleteAvatarByUsername("qqq");

            ipfs.DeActivateProvider();

            //  Console.WriteLine("Output: " + ci.Id);


        }
    }
}
