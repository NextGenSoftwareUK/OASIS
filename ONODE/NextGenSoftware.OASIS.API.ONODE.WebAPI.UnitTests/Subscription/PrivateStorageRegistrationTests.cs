using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Boot=NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

[CollectionDefinition("PrivateStorageRegistration",DisableParallelization=true)]
public class PrivateStorageRegistrationCollection { }

[Collection("PrivateStorageRegistration")]
public class PrivateStorageRegistrationTests
{
    [Theory]
    [InlineData(ProviderType.CosmosBlockChainOASIS)] [InlineData(ProviderType.SuiOASIS)]
    public async Task CreatedProviderRetainsRegisteredKeyReferenceAfterDnaChanges(ProviderType type)
    {
        const string originalName="OASIS_TEST_MISSING_REGISTERED_PRIVATE_KEY";
        const string replacementName="OASIS_TEST_REPLACEMENT_PRIVATE_KEY";
        var originalValue=Environment.GetEnvironmentVariable(originalName);
        var replacementValue=Environment.GetEnvironmentVariable(replacementName);
        var priorDna=Boot.OASISDNA;
        var manager=ProviderManager.Instance;
        var priorProvider=manager.IsProviderRegistered(type)?manager.GetProvider(type):null;
        IOASISStorageProvider? created=null;
        try {
            Environment.SetEnvironmentVariable(originalName,null);
            Environment.SetEnvironmentVariable(replacementName,Convert.ToBase64String(new byte[32]));
            if(priorProvider!=null)manager.UnRegisterProvider(type);
            var settings=new StorageProviderSettings();
            settings.CosmosBlockChainOASIS=new CosmosBlockChainOASISProviderSettings { RpcEndpoint="http://127.0.0.1:1",StorageEncryptionKeyEnvironmentVariable=originalName };
            settings.SuiOASIS=new SuiOASISProviderSettings { RpcEndpoint="http://127.0.0.1:1",StorageEncryptionKeyEnvironmentVariable=originalName };
            Boot.OASISDNA=new OASISDNA { OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS { StorageProviders=settings } };
            var method=typeof(Boot).GetMethod("RegisterProviderInternal",BindingFlags.NonPublic|BindingFlags.Static)!;
            var registered=(OASISResult<IOASISStorageProvider>)method.Invoke(null,new object?[]{type,null,false})!;
            Assert.False(registered.IsError,registered.Message);created=registered.Result;Assert.NotNull(created);
            settings.CosmosBlockChainOASIS.StorageEncryptionKeyEnvironmentVariable=replacementName;
            settings.SuiOASIS.StorageEncryptionKeyEnvironmentVariable=replacementName;
            var activation=await created.ActivateProviderAsync();
            Assert.True(activation.IsError);
            Assert.Contains("configured separately",activation.Message);
            Assert.False(created.IsProviderActivated);
        } finally {
            manager.UnRegisterProvider(type);
            (created as IDisposable)?.Dispose();
            if(priorProvider!=null)manager.RegisterProvider(priorProvider);
            Boot.OASISDNA=priorDna;
            Environment.SetEnvironmentVariable(originalName,originalValue);
            Environment.SetEnvironmentVariable(replacementName,replacementValue);
        }
    }
}
