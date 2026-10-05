using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;

namespace NextGenSoftware.OASIS.API.Core.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Core Test Harness v1.1 — Starknet RPC Smoke Test");

            const string defaultRpcUrl = "https://starknet-sepolia.g.alchemy.com/v2/1xjH03MIQE3xHN7qqkYKK";
            var rpcUrl = Environment.GetEnvironmentVariable("STARKNET_RPC_URL") ?? defaultRpcUrl;
            Console.WriteLine($"Connecting to Starknet RPC at {rpcUrl}");

            using var httpClient = new HttpClient();
            var rpcClient = new StarknetRpcClient(httpClient, rpcUrl);

            await RunRpcSmokeTest(rpcClient);
        }

        private static async Task RunRpcSmokeTest(IStarknetRpcClient rpcClient)
        {
            var blockResult = await rpcClient.GetBlockNumberAsync();
            if (!blockResult.IsError)
            {
                Console.WriteLine($"Current block number: {blockResult.Result}");
            }
            else
            {
                Console.WriteLine($"Block number query failed: {blockResult.Message}");
                return;
            }

            var accountAddress = Environment.GetEnvironmentVariable("STARKNET_SMOKE_ACCOUNT");
            if (!string.IsNullOrWhiteSpace(accountAddress))
            {
                var balanceResult = await rpcClient.GetBalanceAsync(accountAddress);
                if (!balanceResult.IsError)
                {
                    Console.WriteLine($"Balance for {accountAddress}: {balanceResult.Result} (wei)");
                }
                else
                {
                    Console.WriteLine($"Balance query failed: {balanceResult.Message}");
                }
            }
            else
            {
                Console.WriteLine("STARKNET_SMOKE_ACCOUNT not set; only block number will be fetched.");
            }

            var txHash = Environment.GetEnvironmentVariable("STARKNET_SMOKE_TX_HASH");
            if (!string.IsNullOrWhiteSpace(txHash))
            {
                var statusResult = await rpcClient.GetTransactionStatusAsync(txHash);
                if (!statusResult.IsError)
                {
                    Console.WriteLine($"Transaction {txHash} status: {statusResult.Result}");
                }
                else
                {
                    Console.WriteLine($"Transaction status query failed: {statusResult.Message}");
                }
            }
            else
            {
                Console.WriteLine("STARKNET_SMOKE_TX_HASH not set; skipping transaction status check.");
            }
        }
    }
}
