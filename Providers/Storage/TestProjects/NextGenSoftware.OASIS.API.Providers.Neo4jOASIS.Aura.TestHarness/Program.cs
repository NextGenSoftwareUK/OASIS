using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE Neo4JOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            Console.WriteLine("Connecting To Aura DB...");
            //Neo4jOASIS neo = new Neo4jOASIS("http://localhost:7474", "neo4j", "letthereb@light!");
            //Neo4jOASIS neo = new Neo4jOASIS("<neo4j bolt URL>", "<username>", "<password>");
            Neo4jOASIS neo = new Neo4jOASIS("neo4j+s://099535d5.databases.neo4j.io:7687", "neo4j", "buBFnYBmEe0p8wFl6si95_k0QnbAaIlxN5Eeuc3S5A4");

            neo.ActivateProvider();
            
            Console.WriteLine("Connected To Aura DB.");

            Console.WriteLine("Deleting Holon...");
            var resultHolon = neo.DeleteHolon("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            Console.WriteLine(resultHolon.Message);
            return;

            Console.WriteLine("Deleting Holon...");
            var resultHolonasync= await neo.DeleteHolonAsync("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            Console.WriteLine(resultHolonasync.Message);
            return;


            Console.WriteLine("Loading Holon Details..");

            var holonIdAll = neo.LoadAllHolons();
            if (holonIdAll.IsLoaded)
            {
                Console.WriteLine("Holon List Started.");
                foreach (var item in holonIdAll.Result)
                {
                    Console.WriteLine("Holon Description:{0}", item.Description);
                }
                Console.WriteLine("Holon List Ended.");
            }
            else
                Console.WriteLine(holonIdAll.Message);
            return;

            var holonIdasyncAll = await neo.LoadAllHolonsAsync();
            if (holonIdasyncAll.IsLoaded)
            {
                Console.WriteLine("Holon List Started.");
                foreach (var item in holonIdasyncAll.Result)
                {
                    Console.WriteLine("Holon Description:{0}", item.Description);
                }
                Console.WriteLine("Holon List Ended.");
            }
            else
                Console.WriteLine(holonIdasyncAll.Message);
            return;

            Guid hidP = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var holonIdP = neo.LoadHolonsForParent(hidP);
            if (holonIdP.IsLoaded)
            {
                Console.WriteLine("Holon List Started.");
                foreach (var item in holonIdP.Result)
                {
                    Console.WriteLine("Holon Description:{0}", item.Description);
                }
                Console.WriteLine("Holon List Ended.");
            }
            else
                Console.WriteLine(holonIdP.Message);
            return;

            Guid hidasyncP = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var holonIdasyncP = await neo.LoadHolonsForParentAsync(hidasyncP);
            if (holonIdasyncP.IsLoaded)
            {
                Console.WriteLine("Holon List Started.");
                foreach (var item in holonIdasyncP.Result)
                {
                    Console.WriteLine("Holon Description:{0}", item.Description);
                }
                Console.WriteLine("Holon List Ended.");
            }
            else
                Console.WriteLine(holonIdasyncP.Message);
            return;

            Guid hid = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var holonId = neo.LoadHolon(hid);
            if (holonId.IsLoaded)
            {
                Console.WriteLine(holonId.Message);
                Console.WriteLine(holonId.Result.Description);
            }
            else
                Console.WriteLine(holonId.Message);
            return;

            Guid hidasync = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var holonIdasync = await neo.LoadHolonAsync(hid);
            if (holonIdasync.IsLoaded)
            {
                Console.WriteLine(holonIdasync.Message);
                Console.WriteLine(holonIdasync.Result.Description);
            }
            else
                Console.WriteLine(holonIdasync.Message);
            return;

            Holon h1 = new Holon();
            h1.Description = "My Holon Async";
            h1.Name = "Holon Desc Async";
            h1.Version = 1;
            h1.Id = Guid.NewGuid();
            h1.PreviousVersionId = Guid.NewGuid();

            await neo.SaveHolonAsync(h1);

            var resHolonAsync = await neo.SaveHolonAsync(h1);

            if (resHolonAsync.IsSaved)
            {
                Console.WriteLine("Holon ID:{0}", resHolonAsync.Message);
                Console.WriteLine("Holon Created.");
            }
            else
            {
                Console.WriteLine("Error Message:{0}", resHolonAsync.Message);
            }
            return;

            Console.WriteLine("Deleting Avatar...");
            var resultDeleteAsync = await neo.DeleteAvatarAsync("dce55d0b-f62d-4694-9c5b-2f95bb8fc611");
            Console.WriteLine(resultDeleteAsync.Message);
            return;

            Console.WriteLine("Deleting Avatar...");
            var resultDelete = neo.DeleteAvatar("dce55d0b-f62d-4694-9c5b-2f95bb8fc611");
            Console.WriteLine(resultDelete.Message);
            return;          
           

            Console.WriteLine("Loading Avatar Details..");

            var avatarDetailsAllAsync = await neo.LoadAllAvatarDetailsAsync();
            if (avatarDetailsAllAsync.IsLoaded)
            {
                Console.WriteLine("Avatar Detail List Started.");
                foreach (var item in avatarDetailsAllAsync.Result)
                {
                    Console.WriteLine("Avatar User Name:{0}", item.Username);
                    Console.WriteLine("Avatar Email:{0}", item.Email);
                }
                Console.WriteLine("Avatar List Ended.");
            }
            else
                Console.WriteLine(avatarDetailsAllAsync.Message);
            return;

            var avatarDetailsAll = neo.LoadAllAvatarDetails();
            if (avatarDetailsAll.IsLoaded)
            {
                Console.WriteLine("Avatar Detail List Started.");
                foreach (var item in avatarDetailsAll.Result)
                {
                    Console.WriteLine("Avatar User Name:{0}", item.Username);
                    Console.WriteLine("Avatar Email:{0}", item.Email);
                }
                Console.WriteLine("Avatar List Ended.");
            }
            else
                Console.WriteLine(avatarDetailsAll.Message);
            return;

            var avdUsernameasync = "venketesh@gmail.com";
            var avatarDetailsUserasync = await neo.LoadAvatarDetailByUsernameAsync(avdUsernameasync);
            if (avatarDetailsUserasync.IsLoaded)
            {
                Console.WriteLine(avatarDetailsUserasync.Message);
                Console.WriteLine(avatarDetailsUserasync.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailsUserasync.Message);
            return;

            var avdEmailasync = "venketesh@gmail.com";
            var avatarDetailsGUIdAsync = await neo.LoadAvatarDetailByEmailAsync(avdEmailasync);
            if (avatarDetailsGUIdAsync.IsLoaded)
            {
                Console.WriteLine(avatarDetailsGUIdAsync.Message);
                Console.WriteLine(avatarDetailsGUIdAsync.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailsGUIdAsync.Message);
            return;

            Guid avdidasync = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var avatarDetailGUIdAsync = await neo.LoadAvatarDetailAsync(avdidasync);
            if (avatarDetailGUIdAsync.IsLoaded)
            {
                Console.WriteLine(avatarDetailGUIdAsync.Message);
                Console.WriteLine(avatarDetailGUIdAsync.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailGUIdAsync.Message);
            return;

            var avdUsername = "venketesh@gmail.com";
            var avatarDetailsUser = neo.LoadAvatarDetailByUsername(avdUsername);
            if (avatarDetailsUser.IsLoaded)
            {
                Console.WriteLine(avatarDetailsUser.Message);
                Console.WriteLine(avatarDetailsUser.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailsUser.Message);
            return;

            var avdEmail = "venketesh@gmail.com";
            var avatarDetailsGUId = neo.LoadAvatarDetailByEmail(avdEmail);
            if (avatarDetailsGUId.IsLoaded)
            {
                Console.WriteLine(avatarDetailsGUId.Message);
                Console.WriteLine(avatarDetailsGUId.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailsGUId.Message);
            return;



            Guid avdid = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
            var avatarDetailGUId = neo.LoadAvatarDetail(avdid);
            if (avatarDetailGUId.IsLoaded)
            {
                Console.WriteLine(avatarDetailGUId.Message);
                Console.WriteLine(avatarDetailGUId.Result.Username);
            }
            else
                Console.WriteLine(avatarDetailGUId.Message);
            return;
           

            Holon h = new Holon();
            h.Description = "My Holon";
            h.Name = "Holon Desc";
            h.Version = 1;
            h.Id = Guid.NewGuid();
            h.PreviousVersionId = Guid.NewGuid();

            neo.SaveHolon(h);

            var resHolon = neo.SaveHolon(h);

            if (resHolon.IsSaved)
            {
                Console.WriteLine("Avatar ID:{0}", resHolon.Message);
                Console.WriteLine("Avatar Created.");
            }
            else
            {
                Console.WriteLine("Error Message:{0}", resHolon.Message);
            }
            return;

            AvatarDetail avatar = new AvatarDetail()
            {
                Country = "India 14324",
                Address = "India 234254",                
                Email = "venketesh@gmail.com",
                Username = "venketesh@gmail.com",
                Attributes =new AvatarAttributes {Strength=1, Speed =1, Dexterity =1},
            };

            var resAvatar = neo.SaveAvatarDetail(avatar);

            if (resAvatar.IsSaved)
            {
                Console.WriteLine("Avatar ID:{0}", resAvatar.Message);
                Console.WriteLine("Avatar Created.");
            }
            else
            {
                Console.WriteLine("Error Message:{0}", resAvatar.Message);
            }
            return;

            Guid id =new Guid ("560f21ac-090f-4aeb-aba5-9d93ccd4df36");
            var avatarGUId = neo.LoadAvatar(id);
            if (avatarGUId.IsLoaded)
            {
                Console.WriteLine(avatarGUId.Message);
                Console.WriteLine(avatarGUId.Result.FirstName);
            }
            else
                Console.WriteLine(avatarGUId.Message);

            Console.WriteLine("Loading all Avatar...");
            var AvatarList1 = neo.LoadAllAvatars();
            if (AvatarList1.IsLoaded)
            {
                Console.WriteLine("Avatar List Started.");
                foreach (var item in AvatarList1.Result)
                {
                    Console.WriteLine("Avatar First Name:{0}", item.FirstName);
                    Console.WriteLine("Avatar Last Name:{0}", item.LastName);
                }
                Console.WriteLine("Avatar List Ended.");
            }
            //return;
            Console.WriteLine("Creating New Avatar...");
            
            Avatar avatar12 = new Avatar()
            {
                Title = "Shreyas1",
                FirstName = "Shreyas1",
                LastName = "Iyer",
                Email = "Iyer@gmail.com",
                Username = "Iyer@gmail.com",
                Password = "password",                
            };
            
            var resAvatar1 = await neo.SaveAvatarAsync(avatar12);

            if (resAvatar.IsSaved)
            {
                Console.WriteLine("Avatar ID:{0}", resAvatar.Result.Name);
                Console.WriteLine("Avatar Created.");
            }
            else
            {
                Console.WriteLine("Error Message:{0}", resAvatar.Message);
            }
            return;
                      
                        

            Console.WriteLine("Loading Avatar by Email...");
            var retAvatar = await neo.LoadAvatarByEmailAsync("alpeshsharma@gmail.com");
            
            if (resAvatar.IsLoaded)
            {
                Console.WriteLine("Avatar Loaded.");
                Console.WriteLine("Avatar First Name:{0}", retAvatar.Result.FirstName);
                Console.WriteLine("Avatar Last Name:{0}", retAvatar.Result.LastName);
            }
            else
            {
                Console.WriteLine(retAvatar.Message);
            }
            

            Console.WriteLine("Loading Avatar by Username and Password...");
            var uAvatar = await neo.LoadAvatarAsync("devangpatel@gmail.com","password");
            
            if (resAvatar.IsLoaded)
            {
                Console.WriteLine("Avatar Loaded.");
                Console.WriteLine("Avatar First Name:{0}", retAvatar.Result.FirstName);
                Console.WriteLine("Avatar Last Name:{0}", retAvatar.Result.LastName);
            }
            else
            {
                Console.WriteLine(retAvatar.Message);
            }
            

            Console.WriteLine("Loading Avatar by Username...");
            var userAvatar = await neo.LoadAvatarByUsernameAsync("devangpatel@gmail.com");
            
            if (resAvatar.IsLoaded)
            {
                Console.WriteLine("Avatar Loaded.");
                Console.WriteLine("Avatar First Name:{0}", retAvatar.Result.FirstName);
                Console.WriteLine("Avatar Last Name:{0}", retAvatar.Result.LastName);
            }
            else
            {
                Console.WriteLine(retAvatar.Message);
            }            

            Console.WriteLine("Loading all Avatar...");
            var AvatarList = await neo.LoadAllAvatarsAsync();
            if (AvatarList.IsLoaded)
            {
                Console.WriteLine("Avatar List Started.");
                foreach (var item in AvatarList.Result)
                {
                    Console.WriteLine("Avatar First Name:{0}", item.FirstName);
                    Console.WriteLine("Avatar Last Name:{0}", item.LastName);
                }
                Console.WriteLine("Avatar List Ended.");                
            }

            Console.WriteLine("Deleting Avatar...");
            var result = await neo.DeleteAvatarByEmailAsync("harshalpatel@gmail.com");
            Console.WriteLine(result.Message);            
        }
    }
}
