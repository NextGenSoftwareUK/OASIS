using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidRecordHttpTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    internal sealed class Transport : HttpMessageHandler
    {
        public readonly Dictionary<string,string> Records=new();public int Writes;public string Failure="";
        public int RejectWrite;public int UncertainWrite;public int Reads;
        public bool RequireConditionalWrites;
        public TaskCompletionSource<bool>? ReadGate;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsoluteUri==Root.AbsoluteUri.TrimEnd('/')+"/")return Reply(HttpStatusCode.OK,"{}");
            if(request.Method==HttpMethod.Put){
                if(RequireConditionalWrites)Assert.Equal("\"stored-revision\"",Assert.Single(request.Headers.IfMatch).ToString());
                Writes++;if(Failure=="transport"||Writes==UncertainWrite)throw new HttpRequestException("controlled uncertain PUT");
                if(Writes==RejectWrite)return Reply(HttpStatusCode.Forbidden,"");
                if(Failure=="reject")return Reply(HttpStatusCode.Forbidden,"");
                if(Failure=="500")return Reply(HttpStatusCode.InternalServerError,"");
                var rdf=await request.Content!.ReadAsStringAsync(token);Assert.DoesNotContain("private-canary",rdf);
                Records[request.RequestUri.AbsoluteUri]=rdf;return Reply(HttpStatusCode.Created,"");
            }
            Assert.Equal(HttpMethod.Get,request.Method);
            Reads++;
            if(ReadGate!=null)await ReadGate.Task.WaitAsync(token);
            if(Failure=="read")return Reply(HttpStatusCode.ServiceUnavailable,"");
            if(request.RequestUri.AbsolutePath.EndsWith('/')){
                var members=Records.Keys.Where(key=>key.StartsWith(request.RequestUri.AbsoluteUri,StringComparison.Ordinal)).ToArray();
                var body="@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer"+(members.Length==0?" .":"; ldp:contains "+string.Join(",",members.Select(uri=>"<"+uri+">"))+" .");
                return Reply(HttpStatusCode.OK,body);
            }
            if(!Records.TryGetValue(request.RequestUri.AbsoluteUri,out var stored))return Reply(HttpStatusCode.NotFound,"");
            if(Failure=="different"){
                var id=Guid.Parse(Path.GetFileNameWithoutExtension(request.RequestUri.AbsolutePath));
                using var codec=new SolidPrivateRecordCodec(Root,Key());stored=codec.Encode<IHolon>(new Holon{Id=id,Name="different acknowledged payload"});
            }
            var response=Reply(HttpStatusCode.OK,stored);
            response.Headers.ETag=new System.Net.Http.Headers.EntityTagHeaderValue("\"stored-revision\"");
            return response;
        }
        private static HttpResponseMessage Reply(HttpStatusCode code,string body)=>new(code){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
    }

    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task FullCanonicalRecordSurvivesEncryptedHttpWriteAndRead(int kind,bool synchronous)
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        if(kind==0){
            IAvatar record=new Avatar{Email="private-canary@example.invalid",Password="private-canary",Username="private-canary"};
            var saved=synchronous?provider.SaveAvatar(record):await provider.SaveAvatarAsync(record);Assert.False(saved.IsError,saved.Exception?.ToString());Assert.NotEqual(Guid.Empty,record.Id);
            var read=synchronous?provider.LoadAvatar(record.Id):await provider.LoadAvatarAsync(record.Id);Assert.False(read.IsError,read.Message);
            Assert.Equal(record.Password,read.Result.Password);Assert.Equal(record.Email,read.Result.Email);
        }else if(kind==1){
            IAvatarDetail record=new AvatarDetail{Email="private-canary@example.invalid",Username="private-canary",Description="private-canary"};
            var saved=synchronous?provider.SaveAvatarDetail(record):await provider.SaveAvatarDetailAsync(record);Assert.False(saved.IsError,saved.Exception?.ToString());Assert.NotEqual(Guid.Empty,record.Id);
            var read=synchronous?provider.LoadAvatarDetail(record.Id):await provider.LoadAvatarDetailAsync(record.Id);Assert.False(read.IsError,read.Message);
            Assert.Equal(record.Email,read.Result.Email);Assert.Equal(record.Description,read.Result.Description);
        }else{
            IHolon record=new Holon{Name="private-canary",MetaData=new(){["private"]="private-canary"}};
            var saved=synchronous?provider.SaveHolon(record,saveChildren:false):await provider.SaveHolonAsync(record,saveChildren:false);Assert.False(saved.IsError,saved.Exception?.ToString());Assert.NotEqual(Guid.Empty,record.Id);
            var read=synchronous?provider.LoadHolon(record.Id,loadChildren:false):await provider.LoadHolonAsync(record.Id,loadChildren:false);Assert.False(read.IsError,read.Message);
            Assert.Equal(record.Name,read.Result.Name);Assert.Equal("private-canary",read.Result.MetaData["private"].ToString());
        }
        Assert.Equal(1,transport.Writes);
    }

    [Theory][InlineData("reject",false)][InlineData("transport",true)][InlineData("500",true)][InlineData("read",true)][InlineData("different",true)]
    public async Task FailureDoesNotPublishNewIdOrPermitBlindReplay(string failure,bool uncertain)
    {
        var transport=new Transport{Failure=failure};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);IHolon record=new Holon{Name="private-canary"};
        var result=await provider.SaveHolonAsync(record,saveChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(Guid.Empty,record.Id);Assert.Equal(1,transport.Writes);
        Assert.Equal(uncertain,result.Exception is SolidRecordReconciliationException);
        if(result.Exception is SolidRecordReconciliationException error){Assert.True(error.Resource.IsAbsoluteUri);Assert.Equal(failure is "read" or "different",error.WriteAcknowledged);}
    }
    [Fact]
    public async Task MissingStorageKeyCannotWritePlaintext()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",null!,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.SaveAvatarAsync(new Avatar{Username="private-canary"});
        Assert.True(result.IsError);Assert.Equal(0,transport.Writes);
    }
}
