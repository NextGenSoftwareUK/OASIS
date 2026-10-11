using System.Reflection;
using System.Text;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronStorageBackendTests
{
    [Fact]
    public void StorageAbiPreservesLongUnicodeKeysAndValuesWithoutTruncation()
    {
        var key = "private/🌳/" + new string('k', 100);
        var value = "秘密🌱" + new string('v', 1000);
        var encode = typeof(TronStorageBackend).GetMethod("Encode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var data = (byte[])encode.Invoke(null, new object[] { "put(string,string)", new[] { "string", "string" }, new object[] { key, value } })!;
        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("put(string,string)")[..8].ToUpperInvariant(), Convert.ToHexString(data.AsSpan(0, 4)));
        var decoded = new FunctionCallDecoder().DecodeOutput("0x" + Convert.ToHexString(data.AsSpan(4)),
            new[] { new ParameterOutput { Parameter = new Parameter("string", "key", 1) { DecodedType = typeof(string) } },
                new ParameterOutput { Parameter = new Parameter("string", "value", 2) { DecodedType = typeof(string) } } });
        Assert.Equal(key, decoded[0].Result);
        Assert.Equal(value, decoded[1].Result);
        var error = Assert.Throws<TargetInvocationException>(() => encode.Invoke(null,
            new object[] { "put(string,string)", new[] { "string", "string" }, new object[] { key, "\uD800" } }));
        Assert.IsType<EncoderFallbackException>(error.InnerException);
    }

    [Fact]
    public async Task CancellationRejectsStorageIoBeforeResolvingSigningCredentials()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var resolutions = 0;
        var backend = new TronStorageBackend(runtime.NativeTransfers, "unused", "unused", () => { resolutions++; return "unused"; });
        var token = new CancellationToken(true);
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.PutAsync("key", "value", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.DeleteAsync("key", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.GetAsync("key", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.ListKeysAsync("", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.VerifyAsync(token));
        Assert.Equal(0, resolutions);
    }
}
