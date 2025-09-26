//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces;

//namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness
//{
//    class Program
//    {
//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS V1.1");
//            Console.WriteLine("");

//            IPFSOASIS ipfs = new IPFSOASIS(); 
//            ipfs.ActivateProvider();
     
//            Avatar a = new Avatar();
//            a.Username = "farid";
//            a.Password = "man";
//            a.Email = "rttyu@test.com";
//            a.FirstName = "znnn";
//            a.Id = Guid.NewGuid();

//            ipfs.SaveAvatar(a);

//            Avatar b = new Avatar();
//            b.Username = "qqq";
//            b.Password = "ssss";
//            b.Email = "zzz@test.com";
//            b.FirstName = "xxxwqs";           
//            b.Id = Guid.NewGuid();
//            ipfs.SaveAvatar(b);

//            IEnumerable<IAvatar> avatars = ipfs.LoadAllAvatars();
//            Avatar avatar = (Avatar) ipfs.LoadAvatar("farid", "man");

//            Avatar avatar1 = (Avatar)  ipfs.LoadAvatarByEmail("zzz@test.com");

//            Avatar avatar2 = (Avatar)ipfs.LoadAvatarByUsername("qqq");

//            Avatar avatar3 = (Avatar)ipfs.LoadAvatarByProviderKey("QmS21Kp5q2FMN5GwZhUpwsdGwRiaTp5T77ARDgbzxE5jVV");

//            Avatar avatar4 = (Avatar)ipfs.LoadAvatar(Guid.Parse("f4d209e7-d985-4687-b04c-d29520aadcff"));
            
       
//          //  bool isdeleted = ipfs.DeleteAvatarByUsername("qqq");
          
//            Holon h = new Holon();
//            h.Description = "rrr";
//            h.Name = "ffff";
//            h.Version =1;
//            h.Id = Guid.NewGuid();

//            ipfs.SaveHolon(h);

//            List<IHolon> holons = ipfs.LoadAllHolons().ToList();

//            IHolon holon = ipfs.LoadHolon(h.Id);

//            IEnumerable<IHolon> holons1 = ipfs.LoadHolonsForParent(h.Id);

//            ipfs.DeleteHolon(h.Id);
          
//            AvatarDetail ad = new AvatarDetail();
//            ad.Username = "farid";
//            ad.Address = "man";
//            ad.Email = "rttyu@test.com";
//            ad.FirstName = "znnn";
//            ad.Id = Guid.NewGuid();

//            ipfs.SaveAvatarDetail(ad);

//          var avatarDetail1=  ipfs.LoadAvatarDetail(ad.Id);
//          var avatarDetail2 = ipfs.LoadAvatarDetailByEmail("rttyu@test.com");
//          var avatarDetail3 = ipfs.LoadAvatarDetailByUsername("farid");
                   
//            ipfs.DeActivateProvider();

//            //  Console.WriteLine("Output: " + ci.Id);


//        }
//    }
//}
