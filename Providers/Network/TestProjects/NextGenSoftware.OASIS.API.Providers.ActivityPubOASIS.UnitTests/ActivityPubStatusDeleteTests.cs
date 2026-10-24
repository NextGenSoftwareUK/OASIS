using System.Net;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubStatusDeleteTests
{
    private sealed class Transport:HttpMessageHandler
    {
        public int Deletes,Calls;public int Status=200;public bool Lost,WrongId,ActivationFailure;
        public readonly IOException LostCause=new("controlled lost DELETE acknowledgement");
        public readonly IOException ActivationCause=new("controlled instance activation failure");
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            if(request.Method==HttpMethod.Get){if(ActivationFailure)throw ActivationCause;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});}
            Assert.Equal(HttpMethod.Delete,request.Method);Deletes++;
            Assert.Equal("/api/v1/statuses/123",request.RequestUri!.AbsolutePath);Assert.Equal("",request.RequestUri.Query);
            Assert.Equal("Bearer",request.Headers.Authorization!.Scheme);Assert.Equal("synthetic-token",request.Headers.Authorization.Parameter);
            if(Lost)throw LostCause;
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)Status){Content=new StringContent(
                "{\"id\":\""+(WrongId?"124":"123")+"\",\"uri\":\"https://mastodon.invalid/users/owner/statuses/123\",\"content\":\"<p>owned</p>\",\"created_at\":\"2026-01-01T00:00:00Z\",\"account\":{\"id\":\"42\",\"acct\":\"owner\"}}")});
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeDeleteAcknowledgesExactReturnedStatus(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","synthetic-token",transport);
        var result=sync?provider.DeleteHolon("123"):await provider.DeleteHolonAsync("123");
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsDeleted);Assert.Equal(1,result.DeletedCount);
        Assert.NotNull(result.Result);Assert.True(result.Result.IsDeleted);Assert.Equal("123",result.Result.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.ActivityPubOASIS]);Assert.Equal(1,transport.Deletes);
    }
    [Theory][InlineData(401)][InlineData(403)][InlineData(404)][InlineData(429)]
    public async Task DefinitiveRejectionIsNotDeletionOrUnknownMutation(int status)
    {
        var transport=new Transport{Status=status};using var provider=new ActivityPubOASIS("https://mastodon.invalid","synthetic-token",transport);
        var result=await provider.DeleteHolonAsync("123");Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsDeleted);Assert.Equal(0,result.DeletedCount);
        Assert.IsType<HttpRequestException>(result.Exception);Assert.False(NativeMutationReconciliation.IsRequired(result.Exception));Assert.Equal(1,transport.Deletes);
    }
    [Theory][InlineData(500,false,false)][InlineData(408,false,false)][InlineData(204,false,false)][InlineData(200,true,false)][InlineData(200,false,true)]
    public async Task LostOrInvalidAcknowledgementRequiresReconciliationWithoutReplay(int status,bool lost,bool wrong)
    {
        var transport=new Transport{Status=status,Lost=lost,WrongId=wrong};using var provider=new ActivityPubOASIS("https://mastodon.invalid","synthetic-token",transport);
        var result=await provider.DeleteHolonAsync("123");Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsDeleted);Assert.Equal(0,result.DeletedCount);
        Assert.True(NativeMutationReconciliation.IsRequired(result.Exception));Assert.Equal(1,transport.Deletes);
        var evidence=Assert.IsType<MastodonStatusMutationReconciliationException>(result.Exception);
        Assert.Equal("https://mastodon.invalid",evidence.InstanceOrigin);Assert.Equal("123",evidence.NativeStatusId);Assert.Equal("",evidence.OriginalTransactionId);
        if(lost)Assert.Same(transport.LostCause,evidence.InnerException);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ActivationFailureRetainsOriginalCauseWithoutDelete(bool sync)
    {
        var transport=new Transport{ActivationFailure=true};using var provider=new ActivityPubOASIS("https://mastodon.invalid","synthetic-token",transport);
        var result=sync?provider.DeleteHolon("123"):await provider.DeleteHolonAsync("123");
        Assert.True(result.IsError);Assert.Same(transport.ActivationCause,result.Exception);Assert.Null(result.Result);
        Assert.False(result.IsDeleted);Assert.Equal(0,result.DeletedCount);Assert.Equal(0,transport.Deletes);Assert.Equal(1,transport.Calls);
        Assert.False(NativeMutationReconciliation.IsRequired(result.Exception));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)]
    public async Task InvalidIdentityOrMissingTokenRejectsBeforeHttp(int scenario)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid",scenario==0?"":"synthetic-token",transport);
        var result=scenario==1?await provider.DeleteHolonAsync(Guid.NewGuid()):await provider.DeleteHolonAsync(scenario==2?" ":scenario==3?"..":"123");
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsDeleted);Assert.Equal(0,result.DeletedCount);Assert.Equal(0,transport.Calls);
    }
}
