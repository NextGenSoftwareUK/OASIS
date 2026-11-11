using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

internal static class EvmStorageFixture
{
    private const string CountAbi = """
        [
          {"type":"function","name":"getAvatarsCount","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
          {"type":"function","name":"getAvatarDetailsCount","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
          {"type":"function","name":"getHolonsCount","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"}
        ]
        """;

    internal static async Task RequireEmptyAsync(Web3 reader, string contractAddress)
    {
        var contract = reader.Eth.GetContract(CountAbi, contractAddress);
        var avatars = await contract.GetFunction("getAvatarsCount").CallAsync<BigInteger>();
        var details = await contract.GetFunction("getAvatarDetailsCount").CallAsync<BigInteger>();
        var holons = await contract.GetFunction("getHolonsCount").CallAsync<BigInteger>();
        if (avatars != 0 || details != 0 || holons != 0)
            throw new InvalidOperationException(
                $"The dedicated storage contract must be empty. Found {avatars} avatars, {details} details, and {holons} holons.");
    }
}
