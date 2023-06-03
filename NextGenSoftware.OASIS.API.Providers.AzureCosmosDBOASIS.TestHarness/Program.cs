// See https://aka.ms/new-console-template for more information
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entites;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entities;

Console.WriteLine("NextGen Software Ltd AzureCosmosDBOASIS Provider Test Harness");

string url = "https://localhost:8081";
string authKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

AzureCosmosDBOASIS obj = new AzureCosmosDBOASIS(new Uri(url), authKey, "nextgen", 
                                                new List<string> { "avatarItems", "holonItems", "avatarDetailItems" });
obj.ActivateProvider();

Console.WriteLine("cosmos db connected successfully");

//TODO: NEED TO TEST THIS WORKS ASAP!
OASISResult<IAvatar> result = obj.LoadAvatarByEmail("davidellams@hotmail.com");

if (result != null && !result.IsError && result.Result != null)
    Console.WriteLine($"Avatar Loaded By Email Using Email {result.Result.Email}. Result: Id: {result.Result.Id}, Name: {result.Result.Name}");
else
    Console.WriteLine($"Error Loading Avatar By Email! Reason: {result.Result.Message}");

Avatar av = new Avatar()
{ 
    FirstName="Hiren",
    LastName="Bodhi",
    Email="hirenbodhi@gmail.com",
    Id=Guid.NewGuid(),
    AvatarId=Guid.NewGuid()
};

var avResult=await obj.SaveAvatarAsync(av);

if (avResult.IsSaved)
{
    Console.WriteLine("Avatar Saved Successfully");

    var loadObj = await obj.LoadAvatarAsync(avResult.Result.Id);

    if (loadObj.IsLoaded)
    {
        Console.WriteLine("Avatar Loaded Successfully");
        var delResult = await obj.DeleteAvatarAsync(loadObj.Result.AvatarId);
        if (!delResult.IsError)
        {
            Console.WriteLine("Avatar Deleted Successfully");
        }
        else
        {
            Console.WriteLine(delResult.Message);
        }
    }
    else
    {
        Console.WriteLine(loadObj.Message);
    }
}
else
{
    Console.WriteLine(avResult.Message);
}

AvatarDetail avatar = new AvatarDetail();
avatar.Name = "Hiren 456";
avatar.Username = "Hiren@xyz.com";
avatar.Address = "ahmedabad";
avatar.Country = "India";

avatar.Id = Guid.NewGuid();
var result = await obj.SaveAvatarDetailAsync(avatar);
if (result.IsSaved)
{
    Console.WriteLine("Avatar detail Saved Successfully");
}
else
{
    Console.WriteLine(result.Message);
}

var loadAv = await obj.LoadAvatarDetailAsync(result.Result.Id);

if (loadAv.IsLoaded)
{
    Console.WriteLine("Avatar details Loaded Successfully");    
}
else
{
    Console.WriteLine(loadAv.Message);
}

Holon holon = new Holon
{
   Name="Hiren Holon 345",
   Description="Hiren Description 345",
   HolonId=Guid.NewGuid(),
};

var holonResult = await obj.SaveHolonAsync(holon);
if (holonResult.IsSaved)
{
    Console.WriteLine("Holon Saved Successfully");

    var loadHolon = await obj.LoadHolonAsync(holonResult.Result.Id);
    if (loadHolon.IsLoaded)
    {
        Console.WriteLine("Holon Loaded Successfully");

        var delResult = await obj.DeleteHolonAsync(loadHolon.Result.Id);
        if (!delResult.IsError)
        {
            Console.WriteLine("Holon Deleted Successfully");
        }
        else
        {
            Console.WriteLine(delResult.Message);
        }
    }
    else
    {
        Console.WriteLine(loadHolon.Message);
    }
}
else
{
    Console.WriteLine(holonResult.Message);
}

