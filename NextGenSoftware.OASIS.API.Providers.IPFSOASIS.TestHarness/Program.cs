//using Ipfs;
//using Ipfs.Http;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness
//{
//    class Program
//    {
//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS V1.O");
//            Console.WriteLine("");

//           // var ip = new IpfsClient();
//            IPFSOASIS ipfs = new IPFSOASIS("http://localhost:5001"); //TODO: Pass in valid host.
//            ipfs.ActivateProvider();

//            //   var ci = await ip.FileSystem.AddTextAsync("hello world 2023");

//            // string text = await ip.FileSystem.ReadAllTextAsync(ci.Id);

//            //  ipfs.avatarFileAddress = "QmYs2NQg9XvzgEu3nmUx33ieP9DmsvEZW4uMMrhU4vcoNF";           

//            Avatar a = new Avatar();
//            a.Username = "farid";
//            a.Password = "man";
//            a.Email = "rttyu@test.com";
//            a.FirstName = "znnn";

//            ipfs.SaveAvatar(a);

//            Avatar b = new Avatar();
//            b.Username = "qqq";
//            b.Password = "ssss";
//            b.Email = "zzz@test.com";
//            b.FirstName = "xxxwqs";

//            ipfs.SaveAvatar(b);

//            IEnumerable<IAvatar> avatars = await ipfs.LoadAllAvatarsAsync();

//            Avatar avatar = (Avatar)await ipfs.LoadAvatarAsync("farid", "man");

//            bool isdeleted = ipfs.DeleteAvatarByUsername("qqq");

//            ipfs.DeActivateProvider();

//            //  Console.WriteLine("Output: " + ci.Id);


//        }
//    }
//}
