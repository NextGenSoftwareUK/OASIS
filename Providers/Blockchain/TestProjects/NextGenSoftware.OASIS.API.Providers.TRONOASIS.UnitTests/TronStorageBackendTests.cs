using System.Reflection;
using System.Text;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronStorageBackendTests
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(true, "秘密🌱")]
    public void StorageReadDecodingRetainsExistenceSeparatelyFromValue(bool exists, string value)
    {
        var encode = typeof(TronStorageBackend).GetMethod("Encode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var data = (byte[])encode.Invoke(null, new object[] { "result(bool,string)", new[] { "bool", "string" }, new object[] { exists, value } })!;
        var decode = typeof(TronStorageBackend).GetMethod("Decode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var result = (List<ParameterOutput>)decode.Invoke(null, new object[] { data.AsSpan(4).ToArray(), new[] { "bool", "string" } })!;
        Assert.Equal(exists, result[0].Result);
        Assert.Equal(value, result[1].Result);
        var error = Assert.Throws<TargetInvocationException>(() => decode.Invoke(null, new object[] { new byte[31], new[] { "bool", "string" } }));
        Assert.IsType<System.IO.InvalidDataException>(error.InnerException);
        // Complete but noncanonical trailing words must also remain terminal data corruption.
        var padded=data.AsSpan(4).ToArray().Concat(new byte[32]).ToArray();
        var noncanonical=Assert.Throws<TargetInvocationException>(()=>decode.Invoke(null,new object[] {padded,new[] {"bool","string"}}));
        Assert.IsType<System.IO.InvalidDataException>(noncanonical.InnerException);
    }

    [Fact]
    public void StoragePageDecodingPreservesUnicodeKeysAndExactLargeCounters()
    {
        var keys = new[] { "key/🌳", "key/秘密" };
        var next = System.Numerics.BigInteger.Parse("9007199254740993");
        var encode = typeof(TronStorageBackend).GetMethod("Encode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var types = new[] { "string[]", "uint256", "uint256" };
        var data = (byte[])encode.Invoke(null, new object[] { "result(string[],uint256,uint256)", types, new object[] { keys, next, next + 1 } })!;
        var decode = typeof(TronStorageBackend).GetMethod("Decode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var result = (List<ParameterOutput>)decode.Invoke(null, new object[] { data.AsSpan(4).ToArray(), types })!;
        Assert.Equal(keys, Assert.IsType<List<string>>(result[0].Result));
        Assert.Equal(next, result[1].Result);
        Assert.Equal(next + 1, result[2].Result);
    }

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
