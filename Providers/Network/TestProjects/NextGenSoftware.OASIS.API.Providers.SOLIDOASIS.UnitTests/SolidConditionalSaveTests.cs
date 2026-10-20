using System.Net;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidConditionalSaveTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(value=>(byte)value).ToArray();
    private sealed class Transport(string mode,string original):HttpMessageHandler
    {
        public int Reads;public int Writes;public Action? AfterPut;public Action? AfterRead;public string? Stored=mode=="create"?null:original;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri==Root)return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("")};
            if(request.Method==HttpMethod.Get){
                Reads++;if(Stored==null)return new HttpResponseMessage(HttpStatusCode.NotFound);
                var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Stored,System.Text.Encoding.UTF8,"text/turtle")};
                if(mode!="missing")response.Headers.ETag=new EntityTagHeaderValue("\"observed\"",mode=="weak");
                AfterRead?.Invoke();
                return response;
            }
            Assert.Equal(HttpMethod.Put,request.Method);Writes++;
            if(mode=="create"){Assert.Contains(EntityTagHeaderValue.Any,request.Headers.IfNoneMatch);Assert.Empty(request.Headers.IfMatch);}
            else{Assert.Equal("\"observed\"",Assert.Single(request.Headers.IfMatch).Tag);Assert.Empty(request.Headers.IfNoneMatch);}
            if(mode=="race")return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
            Stored=await request.Content!.ReadAsStringAsync(token);AfterPut?.Invoke();return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CallerEditDuringObservationStopsBeforeAnyPut(bool detail)
    {
        var id=Guid.NewGuid();IHolonBase record=detail?new AvatarDetail{Id=id,Username="owned"}:new Avatar{Id=id,Username="owned"};
        using var codec=new SolidPrivateRecordCodec(Root,Key());var original=detail?codec.Encode<IAvatarDetail>((IAvatarDetail)record):codec.Encode<IAvatar>((IAvatar)record);
        var transport=new Transport("replace",original){AfterRead=()=>record.Description="newer observation-time edit"};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);Assert.IsType<InvalidOperationException>(result.Exception);}
        if(detail)Check(await provider.SaveAvatarDetailAsync((IAvatarDetail)record));else Check(await provider.SaveAvatarAsync((IAvatar)record));
        Assert.Equal(0,transport.Writes);Assert.Equal(original,transport.Stored);Assert.Equal("newer observation-time edit",record.Description);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PostCommitCallerEditCannotBeOverwrittenBySavePublication(bool detail)
    {
        var id=Guid.NewGuid();IHolonBase record=detail?new AvatarDetail{Id=id,Username="owned"}:new Avatar{Id=id,Username="owned"};
        using var codec=new SolidPrivateRecordCodec(Root,Key());var original=detail?codec.Encode<IAvatarDetail>((IAvatarDetail)record):codec.Encode<IAvatar>((IAvatar)record);
        var transport=new Transport("replace",original){AfterPut=()=>record.Description="newer caller edit"};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);Assert.True(Assert.IsType<SolidRecordReconciliationException>(result.Exception).WriteAcknowledged);}
        if(detail)Check(await provider.SaveAvatarDetailAsync((IAvatarDetail)record));else Check(await provider.SaveAvatarAsync((IAvatar)record));
        Assert.Equal("newer caller edit",record.Description);Assert.False(record.ProviderUniqueStorageKey.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SOLIDOASIS));Assert.Equal(1,transport.Writes);
    }
    [Theory]
    [InlineData(0,"create")][InlineData(1,"create")][InlineData(2,"create")]
    [InlineData(0,"replace")][InlineData(1,"replace")][InlineData(2,"replace")]
    [InlineData(0,"race")][InlineData(1,"race")][InlineData(2,"race")]
    [InlineData(0,"weak")][InlineData(0,"missing")]
    public async Task ExplicitIdentitySavesUseObservedRevisionOrCreateOnly(int kind,string mode)
    {
        var id=Guid.NewGuid();IHolonBase record=kind==0?new Avatar{Id=id,Username="owned"}:kind==1?new AvatarDetail{Id=id,Username="owned"}:new Holon{Id=id,Name="owned"};
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        var original=kind==0?codec.Encode<IAvatar>((IAvatar)record):kind==1?codec.Encode<IAvatarDetail>((IAvatarDetail)record):codec.Encode<IHolon>((IHolon)record);
        var transport=new Transport(mode,original);using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);record.Description="replacement";
        var success=mode is "create" or "replace";
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.Equal(!success,result.IsError);if(success){Assert.Same(record,result.Result);Assert.True(result.IsSaved);Assert.Equal(id.ToString(),record.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SOLIDOASIS]);}else Assert.Null(result.Result);
            if(mode=="race")Assert.Equal(HttpStatusCode.PreconditionFailed,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
            if(mode is "weak" or "missing")Assert.IsType<InvalidDataException>(result.Exception);
        }
        if(kind==0)Check(await provider.SaveAvatarAsync((IAvatar)record));
        else if(kind==1)Check(await provider.SaveAvatarDetailAsync((IAvatarDetail)record));
        else Check(await provider.SaveHolonAsync((IHolon)record,saveChildren:false));
        Assert.Equal(success?2:1,transport.Reads);Assert.Equal(mode is "weak" or "missing"?0:1,transport.Writes);
        if(!success)Assert.Equal(original,transport.Stored);Assert.Equal(id,record.Id);
    }
}
