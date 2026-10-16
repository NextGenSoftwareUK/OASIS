using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeEscrowBindsExplicitDepositAndRejectsNoOpOrUnauthorizedReplay(bool noOp)
    {
        Assert.AreEqual(new BigInteger(31337), ChainId, "Only retained local chain permitted.");
        var web3 = new Web3(new Account(PrivateKey, ChainId), Host);
        var owner = web3.TransactionManager.Account.Address;
        var token = await CreateTokenAsync(6, noOp);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!, "token-escrow.json")));
        var abi = json.RootElement.GetProperty("abi").GetRawText();
        var deployed = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi,
            json.RootElement.GetProperty("bytecode").GetString(), owner, new HexBigInteger(4000000));
        Assert.AreEqual(BigInteger.One, deployed.Status.Value);
        Assert.AreEqual(json.RootElement.GetProperty("runtimeBytecode").GetString(), await web3.Eth.GetCode.SendRequestAsync(deployed.ContractAddress));
        var escrow = web3.Eth.GetContract(abi, deployed.ContractAddress);
        Assert.AreEqual(BigInteger.One, await escrow.GetFunction("escrowProtocolVersion").CallAsync<BigInteger>());
        var id = System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray());
        var units = new BigInteger(1250000);
        var initial = await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
        await RejectEscrowSimulationAsync(() => escrow.GetFunction("lock").EstimateGasAsync(owner, null, null, new byte[32], token.Address, units), "Empty deposit id");
        await RejectEscrowSimulationAsync(() => escrow.GetFunction("lock").EstimateGasAsync(owner, null, null, id, token.Address, BigInteger.Zero), "Empty deposit amount");
        Assert.AreEqual(BigInteger.One, (await token.GetFunction("approve").SendTransactionAndWaitForReceiptAsync(owner,
            new HexBigInteger(200000), null, null, escrow.Address, units)).Status.Value);
        if (noOp)
        {
            Assert.AreEqual(BigInteger.Zero, (await MineEscrowRejectionAsync(escrow.GetFunction("lock"), owner, id, token.Address, units)).Status.Value);
            var missing = await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EscrowDepositOutput>(id);
            Assert.AreEqual("0x" + new string('0', 40), missing.Owner);
            Assert.AreEqual(BigInteger.Zero, missing.Amount);
            Assert.AreEqual(initial, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
            Assert.AreEqual(BigInteger.Zero, await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
            return;
        }
        var locked = await escrow.GetFunction("lock").SendTransactionAndWaitForReceiptAsync(owner,
            new HexBigInteger(600000), null, null, id, token.Address, units);
        Assert.AreEqual(BigInteger.One, locked.Status.Value);
        var deposit = await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EscrowDepositOutput>(id);
        Assert.AreEqual(owner.ToLowerInvariant(), deposit.Owner.ToLowerInvariant());
        Assert.AreEqual(token.Address.ToLowerInvariant(), deposit.Token.ToLowerInvariant());
        Assert.AreEqual(units, deposit.Amount); Assert.IsFalse(deposit.Released);
        Assert.AreEqual(initial - units, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
        Assert.AreEqual(units, await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
        await RejectEscrowSimulationAsync(() => escrow.GetFunction("lock").EstimateGasAsync(owner, null, null, id, token.Address, units), "Deposit id already used");
        var outsider = (await web3.Eth.Accounts.SendRequestAsync())[1];
        var unlockedRpc = new Web3(Host).Eth.GetContract(abi, escrow.Address);
        Assert.AreEqual(BigInteger.Zero, (await MineEscrowRejectionAsync(unlockedRpc.GetFunction("unlock"), outsider, id)).Status.Value);
        Assert.IsFalse((await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EscrowDepositOutput>(id)).Released);
        Assert.AreEqual(units, await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
        var released = await escrow.GetFunction("unlock").SendTransactionAndWaitForReceiptAsync(owner,
            new HexBigInteger(600000), null, null, id);
        Assert.AreEqual(BigInteger.One, released.Status.Value);
        Assert.IsTrue((await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EscrowDepositOutput>(id)).Released);
        Assert.AreEqual(initial, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
        Assert.AreEqual(BigInteger.Zero, await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
        Assert.AreEqual(BigInteger.Zero, (await MineEscrowRejectionAsync(escrow.GetFunction("unlock"), owner, id)).Status.Value);
        await RejectEscrowSimulationAsync(() => escrow.GetFunction("lock").EstimateGasAsync(owner, null, null, id, token.Address, units), "Deposit id already used");
        Assert.AreEqual(initial, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
    }

    private static async Task RejectEscrowSimulationAsync(Func<Task> operation, string reason)
    {
        Exception? failure = null;
        try { await operation(); } catch (Exception cause) { failure = cause; }
        Assert.IsNotNull(failure, "Expected native escrow rejection.");
        StringAssert.Contains(failure.ToString(), reason);
    }

    private static async Task<TransactionReceipt> MineEscrowRejectionAsync(Function function, string sender, params object[] arguments)
    {
        var web3 = new Web3(Host);
        var disabled = false;
        try
        {
            await web3.Client.SendRequestAsync<bool>(new Nethereum.JsonRpc.Client.RpcRequest(1, "evm_setAutomine", false));
            disabled = true;
            var hash = await function.SendTransactionAsync(sender, new HexBigInteger(600000), null, arguments);
            await web3.Client.SendRequestAsync<string>(new Nethereum.JsonRpc.Client.RpcRequest(2, "evm_mine"));
            return await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(hash);
        }
        finally
        {
            if (disabled) await web3.Client.SendRequestAsync<bool>(new Nethereum.JsonRpc.Client.RpcRequest(3, "evm_setAutomine", true));
        }
    }

    [FunctionOutput]
    public sealed class EscrowDepositOutput : IFunctionOutputDTO
    {
        [Parameter("address", "owner", 1)] public string Owner { get; set; } = "";
        [Parameter("address", "token", 2)] public string Token { get; set; } = "";
        [Parameter("uint256", "amount", 3)] public BigInteger Amount { get; set; }
        [Parameter("bool", "released", 4)] public bool Released { get; set; }
    }
}
