using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Moq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlNativeFailureDiagnosticsTests
{
    [Fact]
    public async Task RegistryPreservesNativeActivationCauseWithoutAnotherAttempt()
    {
        var cause = new HttpRequestException("sensitive native URL", null, HttpStatusCode.ServiceUnavailable);
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.NostrOASIS));
        var query = storage.As<ICosmicQueryStorageProvider>();
        query.SetupGet(value => value.QueryProviderType).Returns(ProviderType.NostrOASIS);
        query.SetupGet(value => value.QueryCapabilities).Returns(CosmicQueryCapabilities.ForStringMetadataKeyLookup("NostrEventId", 64));
        var activation = storage.As<ICosmicReadOnlyQueryActivation>();
        activation.Setup(value => value.ActivateQuerySourceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<bool> { IsError = true, Exception = cause });
        using var profile = new CqlReadSourceProfile(storage.Object);
        var registry = new CqlHostedSourceRegistry(new[] { profile });
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() => registry.ActivateReadSourceAsync(ProviderType.NostrOASIS, default));
        Assert.Same(cause, result.InnerException);
        Assert.Equal(503, CqlNativeFailureDiagnostics.Capture(result).HttpStatus);
        activation.Verify(value => value.ActivateQuerySourceAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CancellationWithAnInnerTimeoutRetainsBothCategories()
    {
        var error = new OperationCanceledException("request canceled", new TimeoutException("request timed out"));

        Assert.Equal("cancelled/timeout", CqlNativeFailureDiagnostics.Capture(error).Categories);
    }

    [Theory]
    [InlineData("response_headers")]
    [InlineData("response_body")]
    public void HederaCancellationStageSurvivesActivationWrapping(string stage)
    {
        var cancellation = new OperationCanceledException();
        cancellation.Data["COSMIC.NativeStage"] = stage;
        var activation = new InvalidOperationException("Activation failed.", cancellation);

        Assert.Equal(stage, CqlNativeFailureDiagnostics.Capture(activation).Stage);
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<(string Text, Exception? Exception)> Entries = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
            => Entries.Add((formatter(state, error), error));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void OperatorLogRetainsTypedCauseButNeverMessagesUrlsHeadersOrRawExceptions(bool activation)
    {
        const string secret = "Bearer token-value https://user:password@example.invalid/?api_key=private";
        var native = new HttpRequestException(secret, new WebSocketException(WebSocketError.NotAWebSocket, secret), HttpStatusCode.ServiceUnavailable);
        native.Data["COSMIC.NativeStage"] = "connect";
        native.Data["headers"] = secret;
        var error = new InvalidOperationException(secret, native);
        var logger = new CaptureLogger();
        CqlNativeFailureDiagnostics.Record(logger, ProviderType.NostrOASIS, activation, error,
            elapsedMilliseconds: activation ? 15001 : null);
        var entry = Assert.Single(logger.Entries);
        Assert.Null(entry.Exception);
        Assert.Contains("httpStatus=503", entry.Text);
        Assert.Contains("stage=connect", entry.Text);
        Assert.Contains("NotAWebSocket", entry.Text);
        Assert.Contains(activation ? "phase=activation" : "phase=query", entry.Text);
        if (activation)
            Assert.Contains("elapsedMilliseconds=15001", entry.Text);
        foreach (var value in new[] { "Bearer", "token-value", "https://", "password", "api_key", "private" })
            Assert.DoesNotContain(value, entry.Text);
    }

    [Fact]
    public void UntrustedDiagnosticValuesAndDeepExceptionChainsAreBounded()
    {
        Exception error = new Exception("not logged");
        error.Data["COSMIC.NativeStage"] = "https://private/?token=secret";
        for (int index = 0; index < 100; index++) error = new Exception("not logged", error);
        var summary = CqlNativeFailureDiagnostics.Capture(error);
        Assert.Equal("other", summary.Categories);
        Assert.Equal("unclassified", summary.Stage);
        Assert.Null(summary.HttpStatus);
        Assert.Equal("none", summary.WebSocketError);
    }
}
