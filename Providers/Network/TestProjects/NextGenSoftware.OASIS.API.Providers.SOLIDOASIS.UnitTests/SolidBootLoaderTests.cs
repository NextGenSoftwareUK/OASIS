using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidBootLoaderTests
{
    private const string KeyName="OASIS_SOLID_FACTORY_TEST_KEY";
    private const string SecretName="OASIS_SOLID_FACTORY_TEST_SECRET";
    private static SolidOASISProviderSettings Settings()=>new(){PodRoot="https://pod.invalid/owned/",StorageKeyEnvironmentVariable=KeyName,ClientSecretEnvironmentVariable=SecretName,ClientId="synthetic-client",TokenEndpoint="https://issuer.invalid/token"};
    private sealed class EnvironmentValue(string name,string? value):IDisposable
    {
        private readonly string? _prior=Environment.GetEnvironmentVariable(name);
        public EnvironmentValue Set(){Environment.SetEnvironmentVariable(name,value);return this;}
        public void Dispose()=>Environment.SetEnvironmentVariable(name,_prior);
    }

    [Theory][InlineData("Anonymous")][InlineData("DpopClientCredentials")][InlineData("Bearer")]
    public void ExplicitModesConstructWithoutActivationOrNetwork(string mode)
    {
        using var key=new EnvironmentValue(KeyName,Convert.ToBase64String(new byte[32])).Set();using var secret=new EnvironmentValue(SecretName,"synthetic-value").Set();
        var settings=Settings();settings.AuthenticationMode=mode;settings.BearerTokenEnvironmentVariable=SecretName;
        if(mode!="DpopClientCredentials"){settings.ClientId=null;settings.TokenEndpoint=null;}
        using var provider=SolidProviderFactory.Create(settings);Assert.False(provider.IsProviderActivated);
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    public void MissingOrInvalidConfigurationNeverDowngradesAuthentication(int scenario)
    {
        using var key=new EnvironmentValue(KeyName,scenario==3?"not-base64":scenario==4?Convert.ToBase64String(new byte[31]):Convert.ToBase64String(new byte[32])).Set();
        using var secret=new EnvironmentValue(SecretName,scenario==0?null:"synthetic-value").Set();var settings=Settings();
        if(scenario==1)settings.AuthenticationMode="unknown";
        if(scenario==2)settings.PodRoot="http://public.invalid/pod/";
        if(scenario==5){settings.AuthenticationMode="Anonymous";}
        Assert.ThrowsAny<ArgumentException>(()=>SolidProviderFactory.Create(settings));
    }

    [Fact]
    public void ConnectionOverrideCannotRebindConfiguredCredentialsToAnotherPod()
    {
        using var key=new EnvironmentValue(KeyName,Convert.ToBase64String(new byte[32])).Set();using var secret=new EnvironmentValue(SecretName,"synthetic-value").Set();
        Assert.Throws<ArgumentException>(()=>{using var provider=SolidProviderFactory.Create(Settings(),"https://unrelated.invalid/owned/");});
    }

    [Fact]
    public void EquivalentNormalizedOverrideKeepsConfiguredPod()
    {
        using var key=new EnvironmentValue(KeyName,Convert.ToBase64String(new byte[32])).Set();using var secret=new EnvironmentValue(SecretName,"synthetic-value").Set();
        using var provider=SolidProviderFactory.Create(Settings(),"https://pod.invalid/owned");Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public void DnaJsonContainsSecretReferencesNotSecretValues()
    {
        var settings=Settings();var dna=new StorageProviderSettings{SOLIDOASIS=settings};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(dna);var restored=Newtonsoft.Json.JsonConvert.DeserializeObject<StorageProviderSettings>(json)!;
        Assert.Equal("DpopClientCredentials",restored.SOLIDOASIS.AuthenticationMode);Assert.Equal(KeyName,restored.SOLIDOASIS.StorageKeyEnvironmentVariable);
        Assert.Equal(SecretName,restored.SOLIDOASIS.ClientSecretEnvironmentVariable);Assert.DoesNotContain("synthetic-value",json);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void ExistingDnaVersionSettingFlowsThroughProductionFactory(bool enabled)
    {
        using var key=new EnvironmentValue(KeyName,Convert.ToBase64String(new byte[32])).Set();using var secret=new EnvironmentValue(SecretName,"synthetic-value").Set();
        var settings=Settings();settings.IsVersionControlEnabled=enabled;
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(settings);
        var restored=Newtonsoft.Json.JsonConvert.DeserializeObject<NextGenSoftware.OASIS.API.DNA.SolidOASISProviderSettings>(json)!;
        using var provider=SolidProviderFactory.Create(restored);
        Assert.Equal(enabled,provider.IsVersionControlEnabled);restored.IsVersionControlEnabled=!enabled;
        Assert.Equal(enabled,provider.IsVersionControlEnabled);Assert.False(provider.IsProviderActivated);
    }
}
