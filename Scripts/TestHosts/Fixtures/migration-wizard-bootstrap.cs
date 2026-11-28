using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;

string stage="OWNED_CONFIGURATION";
MongoDBOASIS provider=null;
try
{
    if(args.Length!=3 || !Regex.IsMatch(args[0],"^oasis_startup_[0-9]+_[0-9]+$") || !Regex.IsMatch(args[1],"^startup_[a-f0-9]{20}$") || !Guid.TryParse(args[2],out var id) || id==Guid.Empty) throw new InvalidOperationException();
    var dna=JsonConvert.DeserializeObject<OASISDNA>(Environment.GetEnvironmentVariable("OASIS_DNA_JSON") ?? throw new InvalidOperationException());
    var storage=dna?.OASIS?.StorageProviders?.MongoDBOASIS;
    if(storage?.DBName!=args[0]+"_avatars" || storage.ConnectionString!="mongodb://mongo:27017/?replicaSet=rs0") throw new InvalidOperationException();
    provider=new MongoDBOASIS(storage.ConnectionString,storage.DBName,dna);
    stage="NATIVE_ACTIVATION";
    var active=await provider.ActivateProviderAsync();if(active.IsError) throw new InvalidOperationException();
    stage="OWNED_AVATAR_LOAD";
    var loaded=await provider.LoadAvatarAsync(id);
    if(loaded.IsError || loaded.Result==null || loaded.Result.Id!=id || loaded.Result.Username!=args[1] || loaded.Result.AvatarType?.Value!=AvatarType.User) throw new InvalidOperationException();
    stage="NATIVE_ROLE_SEED";
    loaded.Result.AvatarType=new EnumValue<AvatarType>(AvatarType.Wizard);
    var saved=await provider.SaveAvatarAsync(loaded.Result);
    if(saved.IsError || !saved.IsSaved || saved.Result?.Id!=id || saved.Result.AvatarType?.Value!=AvatarType.Wizard) throw new InvalidOperationException();
    stage="NATIVE_DEACTIVATION";
    var closed=await provider.DeActivateProviderAsync();if(closed.IsError) throw new InvalidOperationException();provider=null;
    Console.WriteLine("OWNED_WIZARD_NATIVE_PROVIDER_SEED");return 0;
}
catch
{
    Console.Error.WriteLine("MIGRATION_BOOTSTRAP_FAILURE:"+stage);return 1;
}
finally
{
    if(provider!=null)await provider.DeActivateProviderAsync();
}
