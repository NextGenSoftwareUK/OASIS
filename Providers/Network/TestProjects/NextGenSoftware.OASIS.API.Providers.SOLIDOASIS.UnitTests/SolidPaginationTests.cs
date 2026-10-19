using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidPaginationTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly Uri Container=new(Root,"oasis/avatars/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport(string mode,int kind=0):HttpMessageHandler
    {
        private Uri Container=>new(Root,"oasis/"+(kind==0?"avatars":kind==1?"avatar-details":"holons")+"/");
        public readonly Guid A=Guid.NewGuid(),B=Guid.NewGuid();
        public int PageReads,RecordReads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var uri=request.RequestUri!;
            Assert.Equal(Root.Host,uri.Host);Assert.StartsWith(Root.AbsolutePath,uri.AbsolutePath);
            if(uri==Root)return Task.FromResult(Reply(""));
            if(uri.AbsolutePath==Container.AbsolutePath)
            {
                PageReads++;Assert.Contains("max-member-count",request.Headers.GetValues("Prefer").Single());
                var second=uri.Query=="?page=2";
                if(mode=="redirect"&&!second){var redirect=new HttpResponseMessage(HttpStatusCode.SeeOther);redirect.Headers.Location=new Uri("?page=2",UriKind.Relative);return Task.FromResult(redirect);}
                if(second&&mode=="failure")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone));
                var members=second?$"<{A}.ttl>, <{B}.ttl>":$"<{A}.ttl>";
                var response=Reply($"@base <{Container}> . @prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains {members} .");
                var tag=second&&mode=="changed"?"changed":"stable,tag;quoted";
                var canonical=$"<{(mode=="wrongCanonical"?Root:Container)}>; rel=\"canonical\"; etag=\"{tag}\"";
                if(mode!="missingCanonical")response.Headers.TryAddWithoutValidation("Link",canonical);
                response.Headers.TryAddWithoutValidation("Link","<http://www.w3.org/ns/ldp#Page>; rel=type");
                if(!second)
                {
                    var target=mode=="external"?"https://evil.invalid/page":mode=="outside"?"/other/page":mode=="cycle"?Container.AbsoluteUri:"?page=2";
                    response.Headers.TryAddWithoutValidation("Link",$"<{target}>; rel=\"next\"");
                    if(mode=="ambiguous")response.Headers.TryAddWithoutValidation("Link","<?page=3>; rel=next");
                    if(mode=="malformed")response.Headers.TryAddWithoutValidation("Link","<unterminated");
                }
                return Task.FromResult(response);
            }
            RecordReads++;
            var id=Guid.Parse(Path.GetFileNameWithoutExtension(uri.AbsolutePath));
            using var codec=new SolidPrivateRecordCodec(Root,Key());
            var rdf=kind==0?codec.Encode<IAvatar>(new Avatar{Id=id,Username=id.ToString()})
                :kind==1?codec.Encode<IAvatarDetail>(new AvatarDetail{Id=id,Username=id.ToString()})
                :codec.Encode<IHolon>(new Holon{Id=id,Name=id.ToString()});
            return Task.FromResult(Reply(rdf));
        }
        private static HttpResponseMessage Reply(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
    }

    [Theory][InlineData(false,0)][InlineData(true,0)][InlineData(false,1)][InlineData(true,1)][InlineData(false,2)][InlineData(true,2)]
    public async Task AllPagesMergeBeforeDistinctEncryptedMembersAreRead(bool synchronous,int kind)
    {
        var transport=new Transport("valid",kind);using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        IEnumerable<Guid> ids;
        if(kind==0){var result=synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(record=>record.Id);}
        else if(kind==1){var result=synchronous?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(record=>record.Id);}
        else {var result=synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(record=>record.Id);}
        Assert.Equal(2,transport.PageReads);Assert.Equal(2,transport.RecordReads);
        Assert.Equal(new[]{transport.A,transport.B}.OrderBy(id=>id),ids.OrderBy(id=>id));
    }

    [Theory][InlineData("external")][InlineData("outside")][InlineData("cycle")][InlineData("changed")][InlineData("failure")][InlineData("ambiguous")][InlineData("malformed")][InlineData("wrongCanonical")][InlineData("missingCanonical")]
    public async Task FailedOrUnsafeTraversalNeverPublishesPartialResultsOrFetchesMembers(string mode)
    {
        var transport=new Transport(mode);using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(0,transport.RecordReads);
        Assert.Equal(mode is "changed" or "failure"?2:1,transport.PageReads);
    }

    [Fact]
    public async Task Explicit303IsTraversedAsPageNotAsReplacementResource()
    {
        var transport=new Transport("redirect");using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(2,transport.PageReads);Assert.Equal(2,transport.RecordReads);
    }
}
