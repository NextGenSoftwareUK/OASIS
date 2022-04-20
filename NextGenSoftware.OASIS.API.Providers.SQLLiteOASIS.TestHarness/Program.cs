// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories;



var serviceProvider = new ServiceCollection()
    .AddDbContext<DataContext>()
    .AddSingleton<IAvtarDetailRepository, AvtarDetailRepository>()
    .AddSingleton<IAvtarRepository, AvtarRepository>()
    .AddSingleton<IHolonRepository, HolonRepository>()
    .BuildServiceProvider();

var avatarDetail = serviceProvider.GetService<IAvtarDetailRepository>();
var avatar = serviceProvider.GetService<IAvtarRepository>();
var holon = serviceProvider.GetService<IHolonRepository>();



AvatarEntity avatar12 = new AvatarEntity()
{
    Title = "Shreyas1",
    FirstName = "Shreyas1",
    LastName = "Iyer",
    Email = "Iyer@gmail.com",
    Username = "Iyer@gmail.com",
    Password = "password",
    VerificationToken = "test",
    ResetToken = "ResetToken",
    JwtToken = "JWtToken",
    RefreshToken = "RefreshToekn",
    Name = "Shreyas2",
    Description = "Avatar page description",
    PreviousVersionId = Guid.NewGuid(),
    CreatedByAvatarId = Guid.NewGuid().ToString(),
    ModifiedByAvatarId = Guid.NewGuid().ToString(),
    DeletedByAvatarId = Guid.NewGuid().ToString(),
};

Console.WriteLine("Creating New Avatar...");
var resAvatar1 = await avatar.SaveAvatarAsync(avatar12);
Console.WriteLine("Avatar Created.");

HolonEntity h1 = new HolonEntity();
h1.Description = "My Holon Async";
h1.Name = "Holon Desc Async";
h1.Version = 0;
h1.Id = Guid.NewGuid();
h1.PreviousVersionId = Guid.NewGuid();
h1.CreatedByAvatarId = Guid.NewGuid().ToString();
h1.ModifiedByAvatarId = Guid.NewGuid().ToString();
h1.DeletedByAvatarId = Guid.NewGuid().ToString();
var resHolonAsync = await holon.SaveHolonAsync(h1);
Console.WriteLine("Holon Created.");
//return;

AvatarDetailEntity avatarDetailEntity = new AvatarDetailEntity()
{
    Id = Guid.NewGuid(),
    Country = "India 14324",
    Address = "India 234254",
    Email = "venketesh@gmail.com",
    Username = "venketesh@gmail.com",
    Version = 0,
    PreviousVersionId = Guid.NewGuid(),
    CreatedByAvatarId = Guid.NewGuid().ToString(),
    ModifiedByAvatarId = Guid.NewGuid().ToString(),
    DeletedByAvatarId = Guid.NewGuid().ToString(),
    UmaJson = "Test",
    Image2D = "Test",
    Postcode = "Test",
    Town = "test",
    Landline = "Test",
    Mobile ="9978945612",
    Name = "TestName",
    Description = "TestDesc"
};

var resAvatar = avatarDetail.SaveAvatarDetail(avatarDetailEntity);
Console.WriteLine("Avatar Detail Created.");

var holonIdAll = holon.LoadAllHolons();
var holonID = holonIdAll.FirstOrDefault().Id;
Console.WriteLine("Holon List Started.");
foreach (var item in holonIdAll)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}
Console.WriteLine("Holon List Ended.");

Console.WriteLine("Deleting Holon...");
var resultHolon = holon.DeleteHolon(holonID);
Console.WriteLine(resultHolon);
//return;

Console.WriteLine("Deleting Holon...");
var resultHolonasync = await holon.DeleteHolonAsync(holonID);
Console.WriteLine(resultHolonasync);
//return;


Console.WriteLine("Loading Holon Details..");

var holonIdasyncAll = await holon.LoadAllHolonsAsync();
Console.WriteLine("Holon List Started.");
foreach (var item in holonIdasyncAll)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}
Console.WriteLine("Holon List Ended.");


Guid hidP = holonID;
var holonIdP = holon.LoadHolonsForParent(hidP);
Console.WriteLine("Holon List Started.");
foreach (var item in holonIdP)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}
Console.WriteLine("Holon List Ended.");


Guid hidasyncP = holonID;
var holonIdasyncP = await holon.LoadHolonsForParentAsync(hidasyncP);
Console.WriteLine("Holon List Started.");
foreach (var item in holonIdasyncP)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}
Console.WriteLine("Holon List Ended.");

Guid hid = holonID;
var holonIds = holon.LoadHolon(hid);
foreach (var item in holonIds)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}

Guid hidasync = holonID;
var holonIdasync = await holon.LoadHolonAsync(hidasync);
foreach (var item in holonIdasync)
{
    Console.WriteLine("Holon Description:{0}", item.Description);
}


Console.WriteLine("Loading Avatar Details..");

var avatarDetailsAllAsync = await avatarDetail.LoadAllAvatarDetailsAsync();
var AvatarDetailID = avatarDetailsAllAsync.FirstOrDefault().Id;
Console.WriteLine("Avatar Detail List Started.");
foreach (var item in avatarDetailsAllAsync)
{
    Console.WriteLine("Avatar User Name:{0}", item.Username);
    Console.WriteLine("Avatar Email:{0}", item.Email);
}
Console.WriteLine("Avatar List Ended.");
//return;

var avatarDetailsAll = avatarDetail.LoadAllAvatarDetails();
Console.WriteLine("Avatar Detail List Started.");
foreach (var item in avatarDetailsAll)
{
    Console.WriteLine("Avatar User Name:{0}", item.Username);
    Console.WriteLine("Avatar Email:{0}", item.Email);
}
Console.WriteLine("Avatar List Ended.");


var avdUsernameasync = "venketesh@gmail.com";
var avatarDetailsUserasync = await avatarDetail.LoadAvatarDetailByUsernameAsync(avdUsernameasync);
Console.WriteLine(avatarDetailsUserasync.FirstOrDefault().Username);

var avdEmailasync = "venketesh@gmail.com";
var avatarDetailsGUIdAsync = await avatarDetail.LoadAvatarDetailByEmailAsync(avdEmailasync);
Console.WriteLine(avatarDetailsGUIdAsync.FirstOrDefault().Username);

Guid avdidasync = AvatarDetailID;
var avatarDetailGUIdAsync = await avatarDetail.LoadAvatarDetailAsync(avdidasync);
Console.WriteLine(avatarDetailGUIdAsync.FirstOrDefault().Username);

var avdUsername = "venketesh@gmail.com";
var avatarDetailsUser = avatarDetail.LoadAvatarDetailByUsername(avdUsername);
Console.WriteLine(avatarDetailsUser.FirstOrDefault().Username);

var avdEmail = "venketesh@gmail.com";
var avatarDetailsGUId = avatarDetail.LoadAvatarDetailByEmail(avdEmail);
Console.WriteLine(avatarDetailsGUId.FirstOrDefault().Username);

Guid avdid = AvatarDetailID;
var avatarDetailGUId = avatarDetail.LoadAvatarDetail(avdid);
Console.WriteLine(avatarDetailGUId.FirstOrDefault().Username);


Console.WriteLine("Loading all Avatar...");
var AvatarList1 = avatar.LoadAllAvatars();
var avatarID = AvatarList1.FirstOrDefault().Id;
Console.WriteLine("Avatar List Started.");
foreach (var item in AvatarList1)
{
    Console.WriteLine("Avatar First Name:{0}", item.FirstName);
    Console.WriteLine("Avatar Last Name:{0}", item.LastName);
}
Console.WriteLine("Avatar List Ended.");
//return;

Guid id = avatarID;
var avatarGUId = avatar.LoadAvatar(id);
Console.WriteLine(avatarGUId.FirstOrDefault().FirstName);

Console.WriteLine("Loading Avatar by Email...");
var retAvatar = await avatar.LoadAvatarByEmailAsync("alpeshsharma@gmail.com");
Console.WriteLine("Avatar Loaded.");
if (retAvatar.Count > 0)
{
    Console.WriteLine("Avatar First Name:{0}", retAvatar.FirstOrDefault().FirstName);
    Console.WriteLine("Avatar Last Name:{0}", retAvatar.FirstOrDefault().LastName);
}

Console.WriteLine("Loading Avatar by Username and Password...");
var uAvatar = await avatar.LoadAvatarAsync("devangpatel@gmail.com", "password");

Console.WriteLine("Avatar Loaded.");
if (retAvatar.Count > 0)
{
    Console.WriteLine("Avatar First Name:{0}", uAvatar.FirstOrDefault().FirstName);
    Console.WriteLine("Avatar Last Name:{0}", uAvatar.FirstOrDefault().LastName);
}

Console.WriteLine("Loading Avatar by Username...");
var userAvatar = await avatar.LoadAvatarByUsernameAsync("devangpatel@gmail.com");
Console.WriteLine("Avatar Loaded.");
if (retAvatar.Count > 0)
{
    Console.WriteLine("Avatar First Name:{0}", userAvatar.FirstOrDefault().FirstName);
    Console.WriteLine("Avatar Last Name:{0}", userAvatar.FirstOrDefault().LastName);
}
Console.WriteLine("Loading all Avatar...");
var AvatarList = await avatar.LoadAllAvatarsAsync();
Console.WriteLine("Avatar List Started.");
foreach (var item in AvatarList)
{
    Console.WriteLine("Avatar First Name:{0}", item.FirstName);
    Console.WriteLine("Avatar Last Name:{0}", item.LastName);
}
Console.WriteLine("Avatar List Ended.");

Console.WriteLine("Deleting Avatar...");
var result = await avatar.DeleteAvatarByEmailAsync("harshalpatel@gmail.com");
Console.WriteLine("Deleted" + result);

//Console.WriteLine("Deleting Avatar...");
//var resultDeleteAsync = await avatar.DeleteAvatarAsync(avatarID);
//Console.WriteLine(resultDeleteAsync);

Console.WriteLine("Deleting Avatar...");
var resultDelete = avatar.DeleteAvatar(avatarID);
Console.WriteLine(resultDelete);
