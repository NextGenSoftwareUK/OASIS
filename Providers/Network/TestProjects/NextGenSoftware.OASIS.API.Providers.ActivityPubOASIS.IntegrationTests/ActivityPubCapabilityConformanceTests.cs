using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public class ActivityPubCapabilityConformanceTests
{
    // Observation guard over real HTTP, not a fake service. Public GETs only; no token or posting.
    private sealed class PublicReadTransport:DelegatingHandler
    {
        public int Requests;
        public PublicReadTransport():base(new HttpClientHandler{AllowAutoRedirect=false}){}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var uri=request.RequestUri!;
            if(request.Method!=HttpMethod.Get||request.Headers.Authorization!=null||uri.Scheme!="https"||uri.Host!="mastodon.social"||uri.Port!=443||!string.IsNullOrEmpty(uri.UserInfo)
                ||!(uri.AbsolutePath=="/api/v2/instance"&&uri.Query==""||uri.AbsolutePath=="/api/v1/accounts/lookup"&&uri.Query=="?acct=Mastodon"))
                throw new InvalidOperationException("Public capability fixture permits only fixed-origin instance/account GETs.");
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(15));
            Requests++;return await base.SendAsync(request,deadline.Token);
        }
    }

    [Fact]
    public async Task SharedCapabilitySuiteAgainstPublicMastodon()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var work=Path.Combine(root!.FullName,"TestResults","ProviderConformanceCurrent","ActivityPubOASIS","async");
        Directory.CreateDirectory(work);var path=Path.Combine(work,"report.json");
        Assert.False(File.Exists(path)||File.Exists(path+".next"),"Preserve original capability report before another run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(NonStorageConformance.Report report)
        {
            await using(var file=new FileStream(path+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,report);file.Flush(true);
            }
            File.Move(path+".next",path,overwrite:true);
        }
        var providerTransport=new PublicReadTransport();var nativeTransport=new PublicReadTransport();
        using var native=new HttpClient(nativeTransport){Timeout=TimeSpan.FromSeconds(15)};
        using var provider=new ActivityPubOASIS("https://mastodon.social","",providerTransport);
        var result=await NonStorageConformance.RunAsync(provider,"ActivityPubOASIS","native",async()=>{
            var before=providerTransport.Requests+nativeTransport.Requests;
            using var response=await native.GetAsync("https://mastodon.social/api/v1/accounts/lookup?acct=Mastodon");
            response.EnsureSuccessStatusCode();using var expected=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var loaded=await provider.LoadAvatarByUsernameAsync("Mastodon").WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(loaded.IsError);Assert.True(loaded.IsLoaded);Assert.Equal(1,loaded.LoadedCount);Assert.NotNull(loaded.Result);
            Assert.Equal(expected.RootElement.GetProperty("id").GetString(),loaded.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
            Assert.Equal(expected.RootElement.GetProperty("acct").GetString(),loaded.Result.Username);
            Assert.NotEqual(Guid.Empty,loaded.Result.Id);Assert.True(string.IsNullOrEmpty(loaded.Result.Email));Assert.True(string.IsNullOrEmpty(loaded.Result.Password));
            return new NonStorageConformance.Probe("mastodon.social","PublicAccountLookup",providerTransport.Requests+nativeTransport.Requests-before,true,true);
        },Checkpoint);
        Assert.True(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }
}
