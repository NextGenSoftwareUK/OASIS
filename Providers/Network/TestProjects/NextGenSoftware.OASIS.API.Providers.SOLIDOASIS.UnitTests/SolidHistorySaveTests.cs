using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidHistorySaveTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CredentialRotationKeepsHistoricalDataButOnlyCurrentAuthentication(bool synchronous)
    {
        var transport=new Resource();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);IOASISStorageProvider contract=provider;
        var source=new Avatar{Id=Guid.NewGuid(),IsActive=true};
        void Set(string prefix){source.VerificationToken=prefix+"-verify";source.ResetToken=prefix+"-reset";source.RefreshTokens=new(){new NextGenSoftware.OASIS.API.Core.Objects.RefreshToken{Token=prefix+"-refresh"}};source.ProviderWallets=new(){[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS]=new(){new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet{Id=Guid.NewGuid(),PublicKey=prefix+"-public",PrivateKey=prefix+"-private"}}};}
        Set("old");Assert.False((await provider.SaveAvatarAsync(source)).IsError);Set("new");Assert.False((await provider.SaveAvatarAsync(source)).IsError);
        foreach(var prefix in new[]{"old","new"}){
            var selected=new[]{
                synchronous?contract.LoadAvatarByVerificationToken(prefix+"-verify"):await contract.LoadAvatarByVerificationTokenAsync(prefix+"-verify"),
                synchronous?contract.LoadAvatarByResetToken(prefix+"-reset"):await contract.LoadAvatarByResetTokenAsync(prefix+"-reset"),
                synchronous?contract.LoadAvatarByRefreshToken(prefix+"-refresh"):await contract.LoadAvatarByRefreshTokenAsync(prefix+"-refresh"),
                synchronous?contract.LoadAvatarByPublicKey(prefix+"-public"):await contract.LoadAvatarByPublicKeyAsync(prefix+"-public"),
                synchronous?contract.LoadAvatarByPrivateKey(prefix+"-private"):await contract.LoadAvatarByPrivateKeyAsync(prefix+"-private")};
            foreach(var result in selected){if(prefix=="old"){Assert.True(result.IsError);Assert.Null(result.Result);}else{Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(source.Id,result.Result.Id);}}
        }
        var archived=await provider.LoadAvatarAsync(source.Id,1);Assert.False(archived.IsError,archived.Exception?.ToString());Assert.Equal("old-verify",archived.Result.VerificationToken);
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(value=>(byte)value).ToArray();
    private sealed class Resource:HttpMessageHandler
    {
        public string? Turtle;public int Revision,Requests,PutCalls,FailureMode,Deletes;public bool DetailOnly;private Uri? resource;private int identityReads;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Requests++;
            if(request.RequestUri==Root)return new(HttpStatusCode.OK){Content=new StringContent("")};
            if(DetailOnly)Assert.Contains("/avatar-details/",request.RequestUri!.AbsolutePath);
            if(request.Method==HttpMethod.Delete){Assert.Equal("\""+Revision+"\"",Assert.Single(request.Headers.IfMatch).Tag);Deletes++;Turtle=null;return new(HttpStatusCode.NoContent);}
            if(request.Method==HttpMethod.Get&&request.RequestUri!.AbsolutePath.EndsWith('/'))return new(HttpStatusCode.OK){Content=new StringContent("@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains <"+resource+"> .",System.Text.Encoding.UTF8,"text/turtle")};
            if(request.Method==HttpMethod.Put){
                PutCalls++;
                if(Turtle==null)Assert.Contains(request.Headers.IfNoneMatch,tag=>tag.Tag=="*");
                else Assert.Equal("\""+Revision+"\"",Assert.Single(request.Headers.IfMatch).Tag);
                if(FailureMode==1)return new(HttpStatusCode.PreconditionFailed);
                resource=request.RequestUri;Turtle=await request.Content!.ReadAsStringAsync(cancellationToken);Revision++;
                if(FailureMode==3)throw new HttpRequestException("Controlled lost return after committed resource PUT.");
                return new(HttpStatusCode.NoContent);
            }
            Assert.Equal(HttpMethod.Get,request.Method);
            if(Turtle==null)return new(HttpStatusCode.NotFound);
            if(request.RequestUri!=resource)return new(HttpStatusCode.NotFound);
            if(FailureMode==4&&++identityReads==2){
                using var codec=new SolidPrivateRecordCodec(Root,Key());var id=Guid.Parse(System.IO.Path.GetFileNameWithoutExtension(resource!.AbsolutePath));
                if(resource.AbsolutePath.Contains("/avatars/",StringComparison.Ordinal)){
                    var changed=codec.Decode<IAvatar>(Turtle,id);changed.Username="changed-owner";changed.Email="changed@example.invalid";
                    Turtle=(await codec.StageVersionAsync(changed,Turtle)).Turtle;
                }else{
                    var changed=codec.Decode<IAvatarDetail>(Turtle,id);changed.Username="changed-owner";changed.Email="changed@example.invalid";
                    Turtle=(await codec.StageVersionAsync(changed,Turtle)).Turtle;
                }
                Revision++;
            }
            var returned=Turtle;
            if(FailureMode==2&&Revision>=2){using var codec=new SolidPrivateRecordCodec(Root,Key());var id=Guid.Parse(System.IO.Path.GetFileNameWithoutExtension(resource!.AbsolutePath));returned=codec.Encode(codec.Decode<IHolon>(Turtle,id));}
            var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(returned,System.Text.Encoding.UTF8,"text/turtle")};
            response.Headers.ETag=new("\""+Revision+"\"");return response;
        }
    }

    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task AvatarIdentityReassignmentStopsBeforePairMutation(bool email,bool hard)
    {
        var transport=new Resource();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);var row=new Avatar{Id=Guid.NewGuid(),Username="owned-avatar",Email="owned@example.invalid",IsActive=true};
        Assert.False((await provider.SaveAvatarAsync(row)).IsError);transport.FailureMode=4;
        var failed=email?await provider.DeleteAvatarByEmailAsync(row.Email,!hard):await provider.DeleteAvatarByUsernameAsync(row.Username,!hard);
        Assert.True(failed.IsError);Assert.False(failed.Result);Assert.False(failed.IsDeleted);Assert.IsType<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(failed.Exception);
        Assert.Equal(1,transport.PutCalls);Assert.Equal(0,transport.Deletes);
        using var codec=new SolidPrivateRecordCodec(Root,Key());var actual=codec.Decode<IAvatar>(transport.Turtle!,row.Id);Assert.Equal("changed-owner",actual.Username);Assert.Equal(DateTime.MinValue,actual.DeletedDate);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailIdentityReassignmentStopsBeforeLifecycleMutation(bool email)
    {
        var transport=new Resource{DetailOnly=true};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);var row=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-detail",Email="owned@example.invalid",IsActive=true};
        Assert.False((await provider.SaveAvatarDetailAsync(row)).IsError);transport.FailureMode=4;IOASISStorageProvider contract=provider;
        var failed=email?await contract.DeleteAvatarDetailByEmailAsync(row.Email,false):await contract.DeleteAvatarDetailByUsernameAsync(row.Username,false);
        Assert.True(failed.IsError);Assert.False(failed.Result);Assert.False(failed.IsDeleted);Assert.IsType<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(failed.Exception);
        Assert.Equal(1,transport.PutCalls);Assert.Equal(0,transport.Deletes);
        using var codec=new SolidPrivateRecordCodec(Root,Key());var actual=codec.Decode<IAvatarDetail>(transport.Turtle!,row.Id);Assert.Equal("changed-owner",actual.Username);Assert.Equal(DateTime.MinValue,actual.DeletedDate);
    }

    [Theory][InlineData(1)][InlineData(3)]
    public async Task StandaloneDetailSoftFailureNeverReportsDeleted(int mode)
    {
        var transport=new Resource{DetailOnly=true};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);var row=new AvatarDetail{Id=Guid.NewGuid(),IsActive=true,XP=456};
        Assert.False((await provider.SaveAvatarDetailAsync(row)).IsError);var original=transport.Turtle;transport.FailureMode=mode;IOASISStorageProvider contract=provider;
        var failed=await contract.DeleteAvatarDetailAsync(row.Id);
        Assert.True(failed.IsError);Assert.False(failed.Result);Assert.False(failed.IsDeleted);Assert.Equal(2,transport.PutCalls);Assert.Equal(0,transport.Deletes);Assert.Equal(1,row.Version);
        if(mode==1)Assert.Equal(original,transport.Turtle);
        else{Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));using var codec=new SolidPrivateRecordCodec(Root,Key());Assert.NotEqual(DateTime.MinValue,codec.Decode<IAvatarDetail>(transport.Turtle!,row.Id).DeletedDate);Assert.Equal(DateTime.MinValue,(await codec.DecodeVersionAsync<IAvatarDetail>(transport.Turtle!,row.Id,1)).DeletedDate);}
    }

    [Theory]
    [InlineData(0,false,false)][InlineData(0,false,true)][InlineData(0,true,false)][InlineData(0,true,true)]
    [InlineData(1,false,false)][InlineData(1,false,true)][InlineData(1,true,false)][InlineData(1,true,true)]
    [InlineData(2,false,false)][InlineData(2,false,true)][InlineData(2,true,false)][InlineData(2,true,true)]
    public async Task StandaloneDetailDeletionPreservesHistoryAndNeverTouchesAvatar(int selector,bool hard,bool synchronous)
    {
        var transport=new Resource{DetailOnly=true};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-detail",Email="owned@example.invalid",XP=321,Karma=123,IsActive=true};
        Assert.False((await provider.SaveAvatarDetailAsync(detail)).IsError);IOASISStorageProvider contract=provider;
        var deleted=selector switch{
            0=>synchronous?contract.DeleteAvatarDetail(detail.Id,!hard):await contract.DeleteAvatarDetailAsync(detail.Id,!hard),
            1=>synchronous?contract.DeleteAvatarDetailByUsername(detail.Username,!hard):await contract.DeleteAvatarDetailByUsernameAsync(detail.Username,!hard),
            _=>synchronous?contract.DeleteAvatarDetailByEmail(detail.Email,!hard):await contract.DeleteAvatarDetailByEmailAsync(detail.Email,!hard)};
        Assert.False(deleted.IsError,deleted.Exception?.ToString());Assert.True(deleted.Result);Assert.True(deleted.IsDeleted);
        if(hard){Assert.Null(transport.Turtle);Assert.Equal(1,transport.Deletes);}
        else{
            using var codec=new SolidPrivateRecordCodec(Root,Key());var current=codec.Decode<IAvatarDetail>(transport.Turtle!,detail.Id);
            Assert.Equal(2,current.Version);Assert.Equal(321,current.XP);Assert.Equal(123L,current.Karma);Assert.NotEqual(DateTime.MinValue,current.DeletedDate);
            Assert.Equal(DateTime.MinValue,(await codec.DecodeVersionAsync<IAvatarDetail>(transport.Turtle!,detail.Id,1)).DeletedDate);
            var original=transport.Turtle;var repeated=await contract.DeleteAvatarDetailAsync(detail.Id);Assert.False(repeated.IsError,repeated.Exception?.ToString());Assert.Equal(original,transport.Turtle);Assert.Equal(2,transport.PutCalls);
        }
    }

    [Theory][InlineData(1)][InlineData(2)][InlineData(3)]
    public async Task HistoryFailurePreservesCallerStampsAndNeverReplays(int mode)
    {
        var transport=new Resource();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);var source=new Holon{Id=Guid.NewGuid(),Description="original-private"};
        Assert.False((await provider.SaveHolonAsync(source,saveChildren:false)).IsError);var first=source.VersionId;var committed=transport.Turtle;
        transport.FailureMode=mode;source.Description="new-private-intent";
        var failed=await provider.SaveHolonAsync(source,saveChildren:false);
        Assert.True(failed.IsError);Assert.False(failed.IsSaved);Assert.Null(failed.Result);Assert.Equal(1,source.Version);Assert.Equal(first,source.VersionId);Assert.Equal("new-private-intent",source.Description);Assert.Equal(2,transport.PutCalls);
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        if(mode==1){Assert.Equal(committed,transport.Turtle);Assert.Equal(1,transport.Revision);Assert.False(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));}
        else{
            Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));Assert.Equal(2,transport.Revision);
            Assert.Equal(2,codec.Decode<IHolon>(transport.Turtle!,source.Id).Version);Assert.Equal("original-private",(await codec.DecodeVersionAsync<IHolon>(transport.Turtle!,source.Id,1)).Description);
            var evidence=Assert.IsType<SolidRecordReconciliationException>(failed.Exception);Assert.Equal(mode==2,evidence.WriteAcknowledged);
        }
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task OrdinarySavePublishesVerifiedVersionsAndPreservesArchiveWhenDisabled(int kind)
    {
        var transport=new Resource();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolonBase source=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        async Task Save(){
            if(source is IAvatar avatar){var result=await provider.SaveAvatarAsync(avatar);Assert.False(result.IsError,result.Exception?.ToString());}
            else if(source is IAvatarDetail detail){var result=await provider.SaveAvatarDetailAsync(detail);Assert.False(result.IsError,result.Exception?.ToString());}
            else{var result=await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.False(result.IsError,result.Exception?.ToString());}
        }
        source.Description="private-first";await Save();Assert.Equal(1,source.Version);var first=source.VersionId;
        source.Description="private-second";await Save();Assert.Equal(2,source.Version);Assert.Equal(first,source.PreviousVersionId);
        provider.IsVersionControlEnabled=false;source.Description="private-third";await Save();Assert.Equal(3,source.Version);
        if(kind==0){
            var historical=await provider.LoadAvatarAsync(source.Id,1);Assert.False(historical.IsError,historical.Exception?.ToString());Assert.Equal("private-first",historical.Result.Description);
            Assert.Equal("private-first",provider.LoadAvatarByProviderKey(source.Id.ToString(),1).Result.Description);
            Assert.Equal(1,Assert.Single((await provider.LoadAllAvatarsAsync(1)).Result).Version);
            var requests=transport.Requests;var denied=await provider.LoadAvatarByVerificationTokenAsync("old-token",1);Assert.True(denied.IsError);Assert.Equal(requests,transport.Requests);
        }else if(kind==1){
            Assert.Equal("private-first",(await provider.LoadAvatarDetailAsync(source.Id,1)).Result.Description);
            Assert.Equal(1,Assert.Single(provider.LoadAllAvatarDetails(1).Result).Version);
        }else{
            Assert.Equal("private-first",(await provider.LoadHolonAsync(source.Id,loadChildren:false,version:1)).Result.Description);
            Assert.Equal("private-first",provider.LoadHolon(source.Id.ToString(),loadChildren:false,version:1).Result.Description);
            Assert.Equal(1,Assert.Single((await provider.LoadAllHolonsAsync(loadChildren:false,version:1)).Result).Version);
            var missing=await provider.LoadHolonAsync(source.Id,loadChildren:false,version:99);Assert.True(missing.IsError);Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
            Assert.Empty((await provider.LoadAllHolonsAsync(loadChildren:false,version:99)).Result);
        }
        using var codec=new SolidPrivateRecordCodec(Root,Key());
        async Task Check<T>()where T:class,IHolonBase{Assert.Equal("private-first",(await codec.DecodeVersionAsync<T>(transport.Turtle!,source.Id,1)).Description);Assert.Equal("private-second",(await codec.DecodeVersionAsync<T>(transport.Turtle!,source.Id,2)).Description);}
        if(kind==0)await Check<IAvatar>();else if(kind==1)await Check<IAvatarDetail>();else await Check<IHolon>();
        Assert.DoesNotContain("private-first",transport.Turtle!);Assert.DoesNotContain("private-third",transport.Turtle!);Assert.Equal(3,transport.Revision);
    }
}
