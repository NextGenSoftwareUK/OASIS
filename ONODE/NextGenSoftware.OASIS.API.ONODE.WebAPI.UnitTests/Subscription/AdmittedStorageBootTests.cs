using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

[CollectionDefinition("PassiveBoot",DisableParallelization=true)]
public class PassiveBootCollection { }

[Collection("PassiveBoot")]
public class AdmittedStorageBootTests
{
    private sealed class NoIoProvider : NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS
    {
        public int Activations,Reads;
        public override OASISResult<bool> ActivateProvider(){Activations++;throw new Exception("Boot attempted provider activation.");}
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;throw new Exception("Boot attempted provider activation.");}
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)
        {Reads++;throw new Exception("Boot attempted system account storage I/O.");}
    }
    [Theory]
    [InlineData("Warm",true,true,false)]
    [InlineData("Hot",true,true,true)]
    [InlineData("Cold",true,true,true)]
    [InlineData("Warm",false,true,true)]
    [InlineData("Warm",true,false,true)]
    [InlineData("Active",true,true,true)]
    [InlineData("Missing",true,true,true)]
    public async Task ExplicitAdmissionBootNeverActivatesOrTouchesSystemAccount(string mode,bool v2,bool key,bool denied)
    {
        var managerField=typeof(ProviderManager).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic)!;
        var previousManager=managerField.GetValue(null);var previousDna=OASISDNAManager.OASISDNA;
        var booted=typeof(Boot).GetProperty(nameof(Boot.IsOASISBooted))!;
        var booting=typeof(Boot).GetProperty(nameof(Boot.IsOASISBooting))!;
        var previousBooted=booted.GetValue(null);var previousBooting=booting.GetValue(null);
        var dna=new OASISDNA();dna.OASIS.HyperDriveMode=v2?HyperDriveModes.V2:HyperDriveModes.Legacy;
        dna.OASIS.Logging=new LoggingSettings{LogToConsole=false,LogToFile=false};dna.OASIS.ErrorHandling=new ErrorHandlingSettings();
        dna.OASIS.Security=new SecuritySettings{SecretKey=key?new string('t',64):null};
        dna.OASIS.OASISHyperDriveConfig=new(){EnabledProviders=new(){"LocalFileOASIS"},AutoFailoverProviders=new(){"LocalFileOASIS"},AutoReplicationProviders=new(){"LocalFileOASIS"},LoadBalancingProviders=new(){"LocalFileOASIS"}};
        dna.OASIS.StorageProviders=new StorageProviderSettings{OASISProviderBootType=mode is "Active" or "Missing"?"Warm":mode,
            AutoFailOverProviders="LocalFileOASIS",AutoReplicationProviders="LocalFileOASIS",AutoLoadBalanceProviders="LocalFileOASIS",
            AutoFailOverProvidersForAvatarLogin=mode=="Missing"?"MongoDBOASIS":"LocalFileOASIS",AutoFailOverProvidersForCheckIfEmailAlreadyInUse="LocalFileOASIS",
            AutoFailOverProvidersForCheckIfUsernameAlreadyInUse="LocalFileOASIS",AutoFailOverProvidersForCheckIfOASISSystemAccountExists="LocalFileOASIS",
            AutoFailOverLocalProviders="LocalFileOASIS"};
        var provider=new NoIoProvider{IsProviderActivated=mode=="Active"};var manager=new ProviderManager(null,dna);manager.RegisterProvider(provider);
        try{
            managerField.SetValue(null,manager);booted.SetValue(null,false);booting.SetValue(null,false);
            var result=await Boot.BootOASISForAdmittedStorageAsync(dna);
            Assert.True(denied==result.IsError,result.Message);Assert.Equal(!denied,result.Result);
            Assert.Equal(0,provider.Activations);Assert.Equal(0,provider.Reads);Assert.Equal(mode=="Active",provider.IsProviderActivated);
            Assert.Same(provider,Assert.Single(manager.GetAllRegisteredProviders()));
            Assert.Null(dna.OASIS.OASISSystemAccountId);
            if(!denied){var repeated=await Boot.BootOASISForAdmittedStorageAsync(dna);Assert.True(repeated.IsError);
                Assert.Equal(0,provider.Activations);Assert.Equal(0,provider.Reads);}
        }finally{
            managerField.SetValue(null,previousManager);OASISDNAManager.OASISDNA=previousDna;
            booted.SetValue(null,previousBooted);booting.SetValue(null,previousBooting);
        }
    }
}
