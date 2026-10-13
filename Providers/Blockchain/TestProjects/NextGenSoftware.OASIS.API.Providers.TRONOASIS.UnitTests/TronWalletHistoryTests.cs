using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronWalletHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyStakeHistoryPreservesLockedAmountAndResourceRecipient(bool delegated)
    {
        var bytes = new byte[21]; bytes[0] = 0x41; bytes[20] = 7;
        var owner = ByteString.CopyFrom(bytes); bytes[20] = 8;
        var recipient = ByteString.CopyFrom(bytes);
        var freeze = new FreezeBalanceContract { OwnerAddress = owner, FrozenBalance = 9007199254740993L,
            FrozenDuration = 3, ReceiverAddress = delegated ? recipient : ByteString.Empty };
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract {
            Type = Transaction.Types.Contract.Types.ContractType.FreezeBalanceContract, Parameter = Any.Pack(freeze) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(new byte[32]) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal("FreezeBalanceContract", mapped.NativeOperation);
        Assert.Equal("9007199254740993", mapped.RawAmount);
        Assert.StartsWith("SUN-STAKED/", mapped.AmountUnit);
        Assert.Equal(!delegated, mapped.FromWalletAddress == mapped.ToWalletAddress);
        freeze.FrozenBalance = -1;
        transaction.RawData.Contract[0].Parameter = Any.Pack(freeze);
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { transaction, receipt, owner }));
        Assert.Contains("Negative", error.InnerException!.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WitnessVotesAreNotTokenTransfersAndRetainExecutionOutcome(bool failed)
    {
        var bytes = new byte[21]; bytes[0] = 0x41; bytes[20] = 7;
        var owner = ByteString.CopyFrom(bytes);
        var vote = new VoteWitnessContract { OwnerAddress = owner };
        vote.Votes.Add(new VoteWitnessContract.Types.Vote { VoteAddress = owner, VoteCount = 9007199254740993L });
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract {
            Type = Transaction.Types.Contract.Types.ContractType.VoteWitnessContract, Parameter = Any.Pack(vote) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(new byte[32]), Result = (TransactionInfo.Types.code)(failed ? 1 : 0) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal("VoteWitnessContract", mapped.NativeOperation);
        Assert.Equal("0", mapped.RawAmount); Assert.Equal(0d, mapped.Amount);
        Assert.Equal(mapped.FromWalletAddress, mapped.ToWalletAddress);
        Assert.Equal(!failed, mapped.NativeExecutionResult == "Success");
        bytes[20] = 8;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { transaction, receipt, ByteString.CopyFrom(bytes) }));
        Assert.Contains("does not involve", error.InnerException!.Message);
    }

    [Theory]
    [InlineData("missing-asset")] [InlineData("negative-quantity")] [InlineData("foreign-wallet")]
    public void NativeAssetHistoryRejectsMissingIdentityInvalidQuantityOrUnrelatedWallet(string mode)
    {
        var bytes=new byte[21];bytes[0]=0x41;bytes[20]=7;var owner=ByteString.CopyFrom(bytes);
        var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=1700000000000}};
        transaction.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferAssetContract,
            Parameter=Any.Pack(new TransferAssetContract{OwnerAddress=owner,ToAddress=owner,
                AssetName=mode=="missing-asset"?ByteString.Empty:ByteString.CopyFromUtf8("1000001"),Amount=mode=="negative-quantity"?-1:1})});
        bytes[20]=8;var wallet=mode=="foreign-wallet"?ByteString.CopyFrom(bytes):owner;
        var method=typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction",BindingFlags.NonPublic|BindingFlags.Static)!;
        var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{transaction,new TransactionInfo{Id=ByteString.CopyFrom(new byte[32])},wallet}));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeAssetHistoryRetainsRawIdentityQuantityAndOutcomeWithoutSunScaling(bool failed)
    {
        var bytes=new byte[21];bytes[0]=0x41;bytes[20]=7;var owner=ByteString.CopyFrom(bytes);
        bytes[20]=8;var recipient=ByteString.CopyFrom(bytes);var assetId=ByteString.CopyFromUtf8("1000001");
        var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=1700000000000}};
        var contract=new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferAssetContract,
            Parameter=Any.Pack(new TransferAssetContract{OwnerAddress=owner,ToAddress=recipient,AssetName=assetId,Amount=9007199254740993})};
        transaction.RawData.Contract.Add(contract);
        var receipt=new TransactionInfo{Id=ByteString.CopyFrom(new byte[32]),Result=(TransactionInfo.Types.code)(failed?1:0)};
        var method=typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction",BindingFlags.NonPublic|BindingFlags.Static)!;
        var mapped=(NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null,new object[]{transaction,receipt,owner})!;
        Assert.Equal("9007199254740993",mapped.RawAmount);Assert.Equal("TRC10-RAW/31303030303031",mapped.AmountUnit);
        Assert.Equal("TransferAssetContract",mapped.NativeOperation);Assert.Equal(failed?"Failed":"Success",mapped.NativeExecutionResult);
        Assert.Equal((double)9007199254740993m,mapped.Amount);
        Assert.Equal(owner,typeof(TronNativeTransferClient).GetMethod("OriginalOperationOwner",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{contract}));
    }
    [Fact]
    public void PermissionUpdateMapsOwnerWithoutInventingTransferredFunds()
    {
        var bytes = new byte[21]; bytes[0] = 0x41; bytes[20] = 7;
        var owner = ByteString.CopyFrom(bytes);
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract {
            Type = Transaction.Types.Contract.Types.ContractType.AccountPermissionUpdateContract,
            Parameter = Any.Pack(new AccountPermissionUpdateContract { OwnerAddress = owner }) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(new byte[32]) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal("AccountPermissionUpdateContract", mapped.NativeOperation);
        Assert.Equal(mapped.FromWalletAddress, mapped.ToWalletAddress);
        Assert.Equal("0", mapped.RawAmount); Assert.Equal(0d, mapped.Amount);
        Assert.Equal("Success", mapped.NativeExecutionResult);
        bytes[20] = 8;
        Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { transaction, receipt, ByteString.CopyFrom(bytes) }));
    }

    [Fact]
    public void AccountUpdateMapsOwnerWithoutInventingTransferredFunds()
    {
        var bytes = new byte[21]; bytes[0] = 0x41; bytes[20] = 7;
        var owner = ByteString.CopyFrom(bytes);
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract {
            Type = Transaction.Types.Contract.Types.ContractType.AccountUpdateContract,
            Parameter = Any.Pack(new AccountUpdateContract { OwnerAddress = owner, AccountName = ByteString.CopyFromUtf8("fixture") }) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(new byte[32]) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal("AccountUpdateContract", mapped.NativeOperation);
        Assert.Equal(mapped.FromWalletAddress, mapped.ToWalletAddress);
        Assert.Equal("0", mapped.RawAmount); Assert.Equal(0d, mapped.Amount);
        bytes[20] = 8;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { transaction, receipt, ByteString.CopyFrom(bytes) }));
        Assert.Contains("does not involve", error.InnerException!.Message);
    }
    [Fact]
    public void AccountCreationMapsActualParticipantsWithoutInventingTransferAmount()
    {
        var creatorBytes=new byte[21]; creatorBytes[0]=0x41; creatorBytes[20]=1;
        var createdBytes=(byte[])creatorBytes.Clone(); createdBytes[20]=2;
        var creator=ByteString.CopyFrom(creatorBytes); var created=ByteString.CopyFrom(createdBytes);
        var transaction=new Transaction { RawData=new Transaction.Types.raw { Timestamp=1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract { Type=Transaction.Types.Contract.Types.ContractType.AccountCreateContract,
            Parameter=Any.Pack(new AccountCreateContract { OwnerAddress=creator,AccountAddress=created }) });
        var receipt=new TransactionInfo { Id=ByteString.CopyFrom(new byte[32]) };
        var method=typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction",BindingFlags.NonPublic|BindingFlags.Static)!;
        foreach(var wallet in new[] {creator,created})
        {
            var mapped=(NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null,new object[] {transaction,receipt,wallet})!;
            Assert.Equal("AccountCreateContract",mapped.NativeOperation); Assert.Equal("0",mapped.RawAmount);
            Assert.Equal("SUN",mapped.AmountUnit); Assert.Equal(0d,mapped.Amount);
            Assert.NotEqual(mapped.FromWalletAddress,mapped.ToWalletAddress);
            Assert.Equal(wallet.Equals(creator)?NextGenSoftware.OASIS.API.Core.Enums.TransactionType.Debit:NextGenSoftware.OASIS.API.Core.Enums.TransactionType.Credit,mapped.TransactionType);
        }
    }

    [Fact]
    public void NativeMappingPreservesHashAndExactSunWithoutFabricatingGuid()
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 7;
        var owner = ByteString.CopyFrom(address); address[20] = 8;
        var transfer = new TransferContract { OwnerAddress = owner, ToAddress = ByteString.CopyFrom(address), Amount = 9007199254740993L };
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract
            { Type = Transaction.Types.Contract.Types.ContractType.TransferContract, Parameter = Any.Pack(transfer) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(Enumerable.Repeat((byte)255, 32).ToArray()) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal(Guid.Empty, mapped.TransactionId);
        Assert.Equal(new string('f', 64), mapped.NativeTransactionId);
        Assert.Equal("9007199254740993", mapped.RawAmount);
        Assert.Equal("SUN", mapped.AmountUnit);
        Assert.Equal("TransferContract", mapped.NativeOperation);
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Enums.TransactionType.Debit, mapped.TransactionType);
        Assert.Equal(DateTimeKind.Utc, mapped.CreatedDate.Kind);
        using var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(mapped));
        Assert.Equal(new string('f', 64), json.RootElement.GetProperty("NativeTransactionId").GetString());
        Assert.Equal("9007199254740993", json.RootElement.GetProperty("RawAmount").GetString());
        Assert.Equal("SUN", json.RootElement.GetProperty("AmountUnit").GetString());
        Assert.Equal("Success", mapped.NativeExecutionResult);
        receipt.Result = (TransactionInfo.Types.code)1;
        receipt.ResMessage = ByteString.CopyFromUtf8("rejected fixture");
        var failed = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.NotEqual("Success", failed.NativeExecutionResult);
        Assert.Equal("rejected fixture", failed.NativeError);
        Assert.Equal("9007199254740993", failed.RawAmount);
        address[20] = 9;
        var unrelated = Assert.Throws<TargetInvocationException>(() => method.Invoke(null,
            new object[] { transaction, receipt, ByteString.CopyFrom(address) }));
        Assert.IsType<InvalidOperationException>(unrelated.InnerException);
        Assert.Contains("does not involve", unrelated.InnerException!.Message);
        transaction.RawData.Contract.Clear();
        transaction.RawData.Contract.Add(new Transaction.Types.Contract
        {
            Type = Transaction.Types.Contract.Types.ContractType.CreateSmartContract,
            Parameter = Any.Pack(new CreateSmartContract { OwnerAddress = owner, NewContract = new SmartContract { CallValue = 12 } })
        });
        var failedDeployment = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Null(failedDeployment.ToWalletAddress);
        Assert.Equal("12", failedDeployment.RawAmount);
        Assert.NotEqual("Success", failedDeployment.NativeExecutionResult);
    }
}
