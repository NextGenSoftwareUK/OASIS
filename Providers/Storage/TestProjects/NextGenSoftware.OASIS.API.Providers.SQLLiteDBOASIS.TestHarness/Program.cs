using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.TestHarness
{
    public static class Program
    {
        private static async Task Run_SaveHolonAndLoadHolon_Example()
        {
            var sqlLiteDbOasis = new SQLLiteDBOASIS(string.Empty);
            
            Console.WriteLine("Run_SaveHolonAndLoadHolon_Example-->ActivateProvider()");
            sqlLiteDbOasis.ActivateProvider();

            var holonEntity = new Holon()
            {
                Id = Guid.NewGuid(),
                Name = "Bob",
                Description = "Bob in SqlLite Provider :)",
                CreatedDate = DateTime.Now,
                DeletedDate = DateTime.Now,
                ParentHolonId = Guid.NewGuid(),
                IsActive = true,
                IsChanged = false,
                ModifiedDate = DateTime.Now,
                Version = 1
            };
            Console.WriteLine("Run_SaveHolonAndLoadHolon_Example-->SaveHolonAsync()");
            var saveHolonResult = await sqlLiteDbOasis.SaveHolonAsync(holonEntity);
            if (saveHolonResult.IsError)
            {
                Console.WriteLine("Saving failed! Reason: " + saveHolonResult.Message);
                return;
            }

            var loadHolonResult = await sqlLiteDbOasis.LoadHolonAsync(holonEntity.Id);
            if (loadHolonResult.IsError)
            {
                Console.WriteLine("Loading failed! Reason: " + loadHolonResult.Message);
                return;
            }
            
            Console.WriteLine("Name: " + loadHolonResult.Result.Name);

            Console.WriteLine("Run_SaveHolonAndLoadHolon_Example-->DeActivateProvider()");
            sqlLiteDbOasis.DeActivateProvider();
        }
        
        private static async Task Run_SaveAvatarAndLoadAvatar_Example()
        {
            var sqlLiteDbOasis = new SQLLiteDBOASIS(string.Empty);
            
            Console.WriteLine("Run_SaveAvatarAndLoadAvatar_Example-->ActivateProvider()");
            sqlLiteDbOasis.ActivateProvider();

            var avatarEntity = new Avatar()
            {
                Id = Guid.NewGuid(),
                Description = "Bob in SqlLite Provider :)",
                AvatarType = new EnumValue<AvatarType>(AvatarType.User)
            };
            Console.WriteLine("Run_SaveAvatarAndLoadAvatar_Example-->SaveAvatarAsync()");
            var saveAvatarAsyncResult = await sqlLiteDbOasis.SaveAvatarAsync(avatarEntity);
            if (saveAvatarAsyncResult.IsError)
            {
                Console.WriteLine("Saving failed! Reason: " + saveAvatarAsyncResult.Message);
                return;
            }

            var loadAvatarResult = await sqlLiteDbOasis.LoadAvatarAsync(avatarEntity.Id);
            if (loadAvatarResult.IsError)
            {
                Console.WriteLine("Loading failed! Reason: " + loadAvatarResult.Message);
                return;
            }
            
            Console.WriteLine("Description: " + loadAvatarResult.Result.Description);

            Console.WriteLine("Run_SaveAvatarAndLoadAvatar_Example-->DeActivateProvider()");
            sqlLiteDbOasis.DeActivateProvider();
        }

        private static async Task Run_SaveAvatarDetailAndLoadAvatarDetail_Example()
        {
            var sqlLiteDbOasis = new SQLLiteDBOASIS(string.Empty);
            
            Console.WriteLine("Run_SaveAvatarDetailAndLoadAvatarDetail_Example-->ActivateProvider()");
            sqlLiteDbOasis.ActivateProvider();

            var avatarDetailEntity = new AvatarDetail()
            {
                Id = Guid.NewGuid(),
                Description = "Bob in SqlLite Provider :)"
            };
            Console.WriteLine("Run_SaveAvatarDetailAndLoadAvatarDetail_Example-->SaveAvatarDetailAsync()");
            var saveAvatarDetailResult = await sqlLiteDbOasis.SaveAvatarDetailAsync(avatarDetailEntity);
            if (saveAvatarDetailResult.IsError)
            {
                Console.WriteLine("Saving failed! Reason: " + saveAvatarDetailResult.Message);
                return;
            }

            var loadAvatarDetailResult = await sqlLiteDbOasis.LoadAvatarDetailAsync(avatarDetailEntity.Id);
            if (loadAvatarDetailResult.IsError)
            {
                Console.WriteLine("Loading failed! Reason: " + loadAvatarDetailResult.Message);
                return;
            }
            
            Console.WriteLine("Description: " + loadAvatarDetailResult.Result.Description);

            Console.WriteLine("Run_SaveAvatarDetailAndLoadAvatarDetail_Example-->DeActivateProvider()");
            sqlLiteDbOasis.DeActivateProvider();
        }
        
        public static async Task Main(string[] args)
        {
            await Run_SaveHolonAndLoadHolon_Example();
            await Run_SaveAvatarAndLoadAvatar_Example();
            await Run_SaveAvatarDetailAndLoadAvatarDetail_Example();
        }
    }
}

//
// SQLLiteDBOASIS sQLLiteOASIS = new SQLLiteDBOASIS("");
// sQLLiteOASIS.ActivateProvider();
//
// Console.WriteLine("Loading Holon Details..");
//
// var holonIdAll = sQLLiteOASIS.LoadAllHolons();
// if (holonIdAll.IsLoaded)
// {
//     Console.WriteLine("Holon List Started.");
//     foreach (var item in holonIdAll.Result)
//     {
//         Console.WriteLine("Holon Description:{0}", item.Description);
//     }
//     Console.WriteLine("Holon List Ended.");
// }
// else
//     Console.WriteLine(holonIdAll.Message);
//
// var holonIdasyncAll = await sQLLiteOASIS.LoadAllHolonsAsync();
// if (holonIdasyncAll.IsLoaded)
// {
//     Console.WriteLine("Holon List Started.");
//     foreach (var item in holonIdasyncAll.Result)
//     {
//         Console.WriteLine("Holon Description:{0}", item.Description);
//     }
//     Console.WriteLine("Holon List Ended.");
// }
// else
//     Console.WriteLine(holonIdasyncAll.Message);
//
// Guid hidP = new Guid("5208DD86-1853-40EA-A83D-A8F702DDA300");
// var holonIdP = sQLLiteOASIS.LoadHolonsForParent(hidP);
// if (holonIdP.IsLoaded)
// {
//     Console.WriteLine("Holon List Started.");
//     foreach (var item in holonIdP.Result)
//     {
//         Console.WriteLine("Holon Description:{0}", item.Description);
//     }
//     Console.WriteLine("Holon List Ended.");
// }
// else
//     Console.WriteLine(holonIdP.Message);
//
// Guid hidasyncP = new Guid("5208DD86-1853-40EA-A83D-A8F702DDA300");
// var holonIdasyncP = await sQLLiteOASIS.LoadHolonsForParentAsync(hidasyncP);
// if (holonIdasyncP.IsLoaded)
// {
//     Console.WriteLine("Holon List Started.");
//     foreach (var item in holonIdasyncP.Result)
//     {
//         Console.WriteLine("Holon Description:{0}", item.Description);
//     }
//     Console.WriteLine("Holon List Ended.");
// }
// else
//     Console.WriteLine(holonIdasyncP.Message);
//
// Guid hid = new Guid("5208DD86-1853-40EA-A83D-A8F702DDA300");
// var holonId = sQLLiteOASIS.LoadHolon(hid);
// if (holonId.IsLoaded)
// {
//     Console.WriteLine(holonId.Message);
//     Console.WriteLine(holonId.Result.Description);
// }
// else
//     Console.WriteLine(holonId.Message);
//
// Guid hidasync = new Guid("5208DD86-1853-40EA-A83D-A8F702DDA300");
// var holonIdasync = await sQLLiteOASIS.LoadHolonAsync(hid);
// if (holonIdasync.IsLoaded)
// {
//     Console.WriteLine(holonIdasync.Message);
//     Console.WriteLine(holonIdasync.Result.Description);
// }
// else
//     Console.WriteLine(holonIdasync.Message);
//
// IHolon h1 = new Holon();
// h1.Description = "My Holon Async";
// h1.Name = "Holon Desc Async";
// h1.Version = 1;
// h1.Id = Guid.NewGuid();
// h1.PreviousVersionId = Guid.NewGuid();
//
// await sQLLiteOASIS.SaveHolonAsync(h1);
// h1.Id = Guid.NewGuid();
// var resHolonAsync = await sQLLiteOASIS.SaveHolonAsync(h1);
//
// if (resHolonAsync.IsSaved)
// {
//     Console.WriteLine("Holon ID:{0}", resHolonAsync.Message);
//     Console.WriteLine("Holon Created.");
// }
// else
// {
//     Console.WriteLine("Error Message:{0}", resHolonAsync.Message);
// }
//
// Console.WriteLine("Deleting Avatar...");
// Guid Aid = new Guid("4F224576-4F40-4AF7-85FA-84C38C76FBF0");
// var resultDeleteAsync = await sQLLiteOASIS.DeleteAvatarAsync(Aid);
// Console.WriteLine(resultDeleteAsync.Message);
//
// Console.WriteLine("Deleting Avatar...");
// Guid id = new Guid("4F224576-4F40-4AF7-85FA-84C38C76FBF0");
// var resultDelete = sQLLiteOASIS.DeleteAvatar(id);
// Console.WriteLine(resultDelete.Message);
//
//
// Console.WriteLine("Loading Avatar Details..");
//
// var avatarDetailsAllAsync = await sQLLiteOASIS.LoadAllAvatarDetailsAsync();
// if (avatarDetailsAllAsync.IsLoaded)
// {
//     Console.WriteLine("Avatar Detail List Started.");
//     foreach (var item in avatarDetailsAllAsync.Result)
//     {
//         Console.WriteLine("Avatar User Name:{0}", item.Username);
//         Console.WriteLine("Avatar Email:{0}", item.Email);
//     }
//     Console.WriteLine("Avatar List Ended.");
// }
// else
//     Console.WriteLine(avatarDetailsAllAsync.Message);
//
// var avatarDetailsAll = sQLLiteOASIS.LoadAllAvatarDetails();
// if (avatarDetailsAll.IsLoaded)
// {
//     Console.WriteLine("Avatar Detail List Started.");
//     foreach (var item in avatarDetailsAll.Result)
//     {
//         Console.WriteLine("Avatar User Name:{0}", item.Username);
//         Console.WriteLine("Avatar Email:{0}", item.Email);
//     }
//     Console.WriteLine("Avatar List Ended.");
// }
// else
//     Console.WriteLine(avatarDetailsAll.Message);
//
// var avdUsernameasync = "venketesh@gmail.com";
// var avatarDetailsUserasync = await sQLLiteOASIS.LoadAvatarDetailByUsernameAsync(avdUsernameasync);
// if (avatarDetailsUserasync.IsLoaded)
// {
//     Console.WriteLine(avatarDetailsUserasync.Message);
//     Console.WriteLine(avatarDetailsUserasync.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailsUserasync.Message);
//
// var avdEmailasync = "venketesh@gmail.com";
// var avatarDetailsGUIdAsync = await sQLLiteOASIS.LoadAvatarDetailByEmailAsync(avdEmailasync);
// if (avatarDetailsGUIdAsync.IsLoaded)
// {
//     Console.WriteLine(avatarDetailsGUIdAsync.Message);
//     Console.WriteLine(avatarDetailsGUIdAsync.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailsGUIdAsync.Message);
//
// Guid avdidasync = new Guid("ad14d22f-2fe1-4394-9797-b3b588a2869e");
// var avatarDetailGUIdAsync = await sQLLiteOASIS.LoadAvatarDetailAsync(avdidasync);
// if (avatarDetailGUIdAsync.IsLoaded)
// {
//     Console.WriteLine(avatarDetailGUIdAsync.Message);
//     Console.WriteLine(avatarDetailGUIdAsync.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailGUIdAsync.Message);
//
// var avdUsername = "venketesh@gmail.com";
// var avatarDetailsUser = sQLLiteOASIS.LoadAvatarDetailByUsername(avdUsername);
// if (avatarDetailsUser.IsLoaded)
// {
//     Console.WriteLine(avatarDetailsUser.Message);
//     Console.WriteLine(avatarDetailsUser.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailsUser.Message);
//
// var avdEmail = "venketesh@gmail.com";
// var avatarDetailsGUId = sQLLiteOASIS.LoadAvatarDetailByEmail(avdEmail);
// if (avatarDetailsGUId.IsLoaded)
// {
//     Console.WriteLine(avatarDetailsGUId.Message);
//     Console.WriteLine(avatarDetailsGUId.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailsGUId.Message);
//
//
//
// Guid avdid = new Guid("B65FBB47-928F-4D1A-8870-CFF80A39D515");
// var avatarDetailGUId = sQLLiteOASIS.LoadAvatarDetail(avdid);
// if (avatarDetailGUId.IsLoaded)
// {
//     Console.WriteLine(avatarDetailGUId.Message);
//     Console.WriteLine(avatarDetailGUId.Result.Username);
// }
// else
//     Console.WriteLine(avatarDetailGUId.Message);
//
//
// IHolon h = new Holon();
// h.Description = "My Holon";
// h.Name = "Holon Desc";
// h.Version = 1;
// h.Id = Guid.NewGuid();
// h.PreviousVersionId = Guid.NewGuid();
//
// sQLLiteOASIS.SaveHolon(h);
// h.Id = Guid.NewGuid();
// var resHolon = sQLLiteOASIS.SaveHolon(h);
//
// if (resHolon.IsSaved)
// {
//     Console.WriteLine("Avatar ID:{0}", resHolon.Message);
//     Console.WriteLine("Avatar Created.");
// }
// else
// {
//     Console.WriteLine("Error Message:{0}", resHolon.Message);
// }
//
// IAvatarDetail avatar = new AvatarDetail()
// {
//     Email = "Iyer@gmail.com",
//     Username = "Iyer@gmail.com",
// };
//
// var resAvatar = sQLLiteOASIS.SaveAvatarDetail(avatar);
//
// if (resAvatar.IsSaved)
// {
//     Console.WriteLine("Avatar ID:{0}", resAvatar.Message);
//     Console.WriteLine("Avatar Created.");
// }
// else
// {
//     Console.WriteLine("Error Message:{0}", resAvatar.Message);
// }
//
// Guid GuidID = new Guid("76B778E6-DD47-40EA-B164-80945FE5452E");
// var avatarGUId = sQLLiteOASIS.LoadAvatar(GuidID);
// if (avatarGUId.IsLoaded)
// {
//     Console.WriteLine(avatarGUId.Message);
//     Console.WriteLine(avatarGUId.Result.FirstName);
// }
// else
//     Console.WriteLine(avatarGUId.Message);
//
// Console.WriteLine("Loading all Avatar...");
// var AvatarList1 = sQLLiteOASIS.LoadAllAvatars();
// if (AvatarList1.IsLoaded)
// {
//     Console.WriteLine("Avatar List Started.");
//     foreach (var item in AvatarList1.Result)
//     {
//         Console.WriteLine("Avatar First Name:{0}", item.FirstName);
//         Console.WriteLine("Avatar Last Name:{0}", item.LastName);
//     }
//     Console.WriteLine("Avatar List Ended.");
// }
// Console.WriteLine("Creating New Avatar...");
//
// Avatar avatar12 = new Avatar()
// {
//     Title = "Shreyas1",
//     FirstName = "Shreyas1",
//     LastName = "Iyer",
//     Email = "Iyer@gmail.com",
//     Username = "Iyer@gmail.com",
//     Password = "password",
// };
//
// var resAvatar1 = await sQLLiteOASIS.SaveAvatarAsync(avatar12);
//
// if (resAvatar.IsSaved)
// {
//     Console.WriteLine("Avatar ID:{0}", resAvatar1.Result.Name);
//     Console.WriteLine("Avatar Created.");
// }
// else
// {
//     Console.WriteLine("Error Message:{0}", resAvatar1.Message);
// }
//
//
//
// Console.WriteLine("Loading Avatar by Email...");
// var retAvatar = await sQLLiteOASIS.LoadAvatarByEmailAsync("alpeshsharma@gmail.com");
//
// if (retAvatar.IsLoaded)
// {
//     Console.WriteLine("Avatar Loaded.");
//     Console.WriteLine("Avatar First Name:{0}", retAvatar.Result.FirstName);
//     Console.WriteLine("Avatar Last Name:{0}", retAvatar.Result.LastName);
// }
// else
// {
//     Console.WriteLine(retAvatar.Message);
// }
//
//
// Console.WriteLine("Loading Avatar by Username and Password...");
// var uAvatar = await sQLLiteOASIS.LoadAvatarAsync("devangpatel@gmail.com");
//
// if (uAvatar.IsLoaded)
// {
//     Console.WriteLine("Avatar Loaded.");
//     Console.WriteLine("Avatar First Name:{0}", uAvatar.Result.FirstName);
//     Console.WriteLine("Avatar Last Name:{0}", uAvatar.Result.LastName);
// }
// else
// {
//     Console.WriteLine(uAvatar.Message);
// }
//
//
// Console.WriteLine("Loading Avatar by Username...");
// var userAvatar = await sQLLiteOASIS.LoadAvatarByUsernameAsync("devangpatel@gmail.com");
//
// if (userAvatar.IsLoaded)
// {
//     Console.WriteLine("Avatar Loaded.");
//     Console.WriteLine("Avatar First Name:{0}", userAvatar.Result.FirstName);
//     Console.WriteLine("Avatar Last Name:{0}", userAvatar.Result.LastName);
// }
// else
// {
//     Console.WriteLine(userAvatar.Message);
// }
//
// Console.WriteLine("Loading all Avatar...");
// var AvatarList = await sQLLiteOASIS.LoadAllAvatarsAsync();
// if (AvatarList.IsLoaded)
// {
//     Console.WriteLine("Avatar List Started.");
//     foreach (var item in AvatarList.Result)
//     {
//         Console.WriteLine("Avatar First Name:{0}", item.FirstName);
//         Console.WriteLine("Avatar Last Name:{0}", item.LastName);
//     }
//     Console.WriteLine("Avatar List Ended.");
// }
//
// Console.WriteLine("Deleting Avatar...");
// var result = await sQLLiteOASIS.DeleteAvatarByEmailAsync("harshalpatel@gmail.com");
// Console.WriteLine(result.Message);