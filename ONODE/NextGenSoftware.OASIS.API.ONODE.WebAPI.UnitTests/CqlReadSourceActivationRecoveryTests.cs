using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlReadSourceActivationRecoveryTests
{
    public interface ReadSource : IOASISStorageProvider, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation { }
    public class Source : DispatchProxy
    {
        public bool Active, AlwaysFail;
        public int Attempts;
        public Exception Error = new HttpRequestException("never-log-native-secret", null, HttpStatusCode.InternalServerError);
        public TaskCompletionSource FirstAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override object? Invoke(MethodInfo? method, object?[]? arguments)
        {
            switch (method!.Name)
            {
                case "get_ProviderType": return new EnumValue<ProviderType>(ProviderType.BlockscoutOASIS);
                case "get_QueryProviderType": return ProviderType.BlockscoutOASIS;
                case "get_QueryCapabilities": return CosmicQueryCapabilities.ForStringMetadataKeyLookup("BlockscoutAddress",42,8);
                case "get_IsProviderActivated": return Active;
                case "ActivateQuerySourceAsync":
                    var token = (CancellationToken)arguments![0]!;
                    token.ThrowIfCancellationRequested(); Assert.True(token.CanBeCanceled);
                    Attempts++; FirstAttempt.TrySetResult();
                    Active = !AlwaysFail && Attempts > 1;
                    return Task.FromResult(new OASISResult<bool>(Active) { IsError = !Active, Exception = Active ? null : Error });
                default: throw new InvalidOperationException("No query, write, signing or canonical registration is allowed.");
            }
        }
    }

    private sealed class CaptureLogger : ILogger<CqlHostedSourceRegistry>
    {
        public List<string> Messages = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState,Exception?,string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private static (ReadSource Adapter, Source State) Create()
    {
        var adapter = DispatchProxy.Create<ReadSource, Source>();
        return (adapter, (Source)(object)adapter);
    }

    [Fact]
    public async Task ExplicitRecoveryPublishesAvailabilityOnlyAfterSecondAwaitedHealthCheck()
    {
        var (adapter,state) = Create(); var log = new CaptureLogger();
        using var profile = new CqlReadSourceProfile(adapter, CqlReadSourceActivationPolicy.TransientServerErrorOnce);
        using var services = new ServiceCollection()
            .AddSingleton(profile)
            .AddSingleton<ILogger<CqlHostedSourceRegistry>>(log)
            .AddSingleton<ICqlHostedSourceRegistry, CqlHostedSourceRegistry>()
            .BuildServiceProvider();
        var registry = services.GetRequiredService<ICqlHostedSourceRegistry>();
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            Array.Empty<ProviderType>(), new[] { new StorageQueryTariff(ProviderType.BlockscoutOASIS,0,"test-v1") },
            null, new[] { ProviderType.BlockscoutOASIS });
        using var hosted = new CqlHostedSources(admission, registry,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CqlHostedSources>.Instance);
        await hosted.StartAsync(default);
        await state.FirstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(Assert.Single(hosted.Describe()).Available);
        Assert.Equal(1,state.Attempts);
        await hosted.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(Assert.Single(hosted.Describe()).Available);
        Assert.Equal(2,state.Attempts);
        Assert.Contains(log.Messages, text => text.Contains("completedAttempt=1") && text.Contains("maximumAttempts=2") && text.Contains("httpStatus=500"));
        Assert.DoesNotContain(log.Messages, text => text.Contains("never-log-native-secret"));
        Assert.Same(adapter,registry.Find(ProviderType.BlockscoutOASIS));
    }

    [Fact]
    public async Task ExhaustedServerErrorsStayUnavailableAndNeverStartAThirdAttempt()
    {
        var (adapter,state) = Create(); state.AlwaysFail=true;
        state.Error=new HttpRequestException("secret",null,HttpStatusCode.ServiceUnavailable);
        using var profile=new CqlReadSourceProfile(adapter,CqlReadSourceActivationPolicy.TransientServerErrorOnce);
        var registry=new CqlHostedSourceRegistry(new[] {profile});
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>registry.ActivateReadSourceAsync(ProviderType.BlockscoutOASIS,default));
        Assert.Equal("CQL_SOURCE_ACTIVATION_FAILED",error.Message);
        Assert.Equal(2,state.Attempts); Assert.False(state.Active);
    }

    [Theory]
    [InlineData(401)] [InlineData(403)] [InlineData(404)] [InlineData(429)] [InlineData(501)]
    public async Task AuthenticationRateLimitAndUnsupportedStatusesAreNotRetried(int status)
    {
        var (adapter,state)=Create(); state.Error=new HttpRequestException("secret",null,(HttpStatusCode)status);
        using var profile=new CqlReadSourceProfile(adapter,CqlReadSourceActivationPolicy.TransientServerErrorOnce);
        var registry=new CqlHostedSourceRegistry(new[] {profile});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>registry.ActivateReadSourceAsync(ProviderType.BlockscoutOASIS,default));
        Assert.Equal(1,state.Attempts);
    }

    [Theory]
    [InlineData("invalid-data")] [InlineData("timeout")] [InlineData("missing-status")]
    public async Task MalformedDataTimeoutsAndUntypedTransportFailuresAreNotRetried(string kind)
    {
        var (adapter,state)=Create(); state.Error=kind switch {
            "invalid-data"=>new InvalidDataException("secret"),
            "timeout"=>new TaskCanceledException("secret",new TimeoutException()),
            _=>new HttpRequestException("secret") };
        using var profile=new CqlReadSourceProfile(adapter,CqlReadSourceActivationPolicy.TransientServerErrorOnce);
        var registry=new CqlHostedSourceRegistry(new[] {profile});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>registry.ActivateReadSourceAsync(ProviderType.BlockscoutOASIS,default));
        Assert.Equal(1,state.Attempts);
    }

    [Fact]
    public async Task DefaultProfilesPreserveOneAttemptEvenForServerErrors()
    {
        var (adapter,state)=Create(); using var profile=new CqlReadSourceProfile(adapter);
        var registry=new CqlHostedSourceRegistry(new[] {profile});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>registry.ActivateReadSourceAsync(ProviderType.BlockscoutOASIS,default));
        Assert.Equal(1,state.Attempts);
    }

    [Theory]
    [InlineData(500)] [InlineData(502)] [InlineData(503)] [InlineData(504)]
    public async Task CancellationDuringBackoffStopsWithoutAnotherNativeOperation(int status)
    {
        var (adapter,state)=Create(); state.Error = new HttpRequestException("secret",null,(HttpStatusCode)status);
        using var profile=new CqlReadSourceProfile(adapter,CqlReadSourceActivationPolicy.TransientServerErrorOnce);
        var registry=new CqlHostedSourceRegistry(new[] {profile}); using var stopping=new CancellationTokenSource();
        var activation=registry.ActivateReadSourceAsync(ProviderType.BlockscoutOASIS,stopping.Token);
        await state.FirstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(2)); stopping.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>activation);
        Assert.Equal(1,state.Attempts); Assert.False(state.Active);
    }
}
