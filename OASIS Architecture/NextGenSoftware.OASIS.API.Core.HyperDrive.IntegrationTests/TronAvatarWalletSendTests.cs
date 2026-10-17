using System.Reflection;
using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using Rijndael256;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronAvatarWalletSendTests
{
    [Theory]
    [InlineData(false, false, "id")]
    [InlineData(true, false, "id")]
    [InlineData(false, true, "id")]
    [InlineData(true, true, "id")]
    [InlineData(false, false, "username")]
    [InlineData(true, false, "username")]
    [InlineData(false, true, "username")]
    [InlineData(true, true, "username")]
    [InlineData(false, false, "email")]
    [InlineData(true, false, "email")]
    [InlineData(false, true, "email")]
    [InlineData(true, true, "email")]
    [InlineData(false, true, "mint")]
    [InlineData(true, true, "mint")]
    [InlineData(false,true,"mint-await")] [InlineData(true,true,"mint-await")]
    [InlineData(false,false,"send-token-await")] [InlineData(false,true,"send-token-await")]
    [InlineData(false,true,"burn-token-await")]
    [InlineData(false, true, "mint-nft")]
    [InlineData(true, true, "mint-nft")]
    [InlineData(false, true, "escrow-release")]
    [InlineData(true, true, "escrow-release")]
    [InlineData(false, true, "escrow-manager-release")]
    [InlineData(true, true, "escrow-manager-release")]
    public async Task AvatarTransferUsesEncryptedDetachedWalletAndRealSdkSigning(bool synchronous, bool token, string selector)
    {
        var escrowRelease=selector is "escrow-release" or "escrow-manager-release";
        var mintToken=selector is "mint" or "mint-await";
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var root = Path.Combine(directory!.FullName, "TestResults", "HyperDriveTron");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "avatar-wallet.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var walletRoot = Path.Combine(root, "avatar-wallet-files");
        Directory.CreateDirectory(walletRoot);
        // LocalFile's wallet prefix is distinct from its avatar/holon storage directory.
        LocalFileOASIS local = selector=="mint-await"?new HederaTokenIntentTests.GatedWalletStore(walletRoot)
            :new LocalFileOASIS(filePath: walletRoot + Path.DirectorySeparatorChar, storageDirectory: walletRoot);
        var sender = Guid.Parse("cfa31df2-584d-47e4-aeb6-14745e5142f1");
        var recipient = Guid.Parse("cfa31df2-584d-47e4-aeb6-14745e5142f2");
        const string key = "0000000000000000000000000000000000000000000000000000000000000001";
        var owner = new TronNet.TronECKey(key, TronNet.TronNetwork.MainNet).GetPublicAddress();
        var destination = new TronNet.TronECKey(new string('2', 64), TronNet.TronNetwork.MainNet).GetPublicAddress();
        var tokenAddress = new TronNet.TronECKey(new string('3', 64), TronNet.TronNetwork.MainNet).GetPublicAddress();
        var escrowAddress = new TronNet.TronECKey(new string('4',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var escrowBytes=ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(escrowAddress));
        var depositId=Guid.Parse("5f31afde-1229-4e99-8088-a0034d33a557");
        var nativeDepositId=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(depositId.ToString("N")));
        var runtimeCode=ByteString.CopyFromUtf8("synthetic reviewed per-deposit runtime fixture");
        var runtimeHash=Convert.ToHexString(SHA256.HashData(runtimeCode.ToByteArray()));
        var released=false;
        var sourceBytes = ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(owner));
        var destinationBytes = ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(destination));
        var authority = new Account { Address = sourceBytes, OwnerPermission = new Permission { Id = 0, Threshold = 1 } };
        authority.OwnerPermission.Keys.Add(new TronNet.Protocol.Key { Address = sourceBytes, Weight = 1 });
        var encrypted = Rijndael256.Rijndael.Encrypt(key, new string('k', 32), KeySize.Aes256);
        var genesis = new string('0', 64);
        var broadcasts = 0; var constructions = 0; var receipts = 0;
        var transactionId = ByteString.Empty;
        Action? mutateMintRequest = null;
        Method<TRequest,TReply> Rpc<TRequest,TReply>(string service, string name, MessageParser<TRequest> request, MessageParser<TReply> reply)
            where TRequest : class, IMessage<TRequest> where TReply : class, IMessage<TReply>
            => new(MethodType.Unary, service, name, Marshallers.Create<TRequest>(x => x.ToByteArray(), x => request.ParseFrom(x)),
                Marshallers.Create<TReply>(x => x.ToByteArray(), x => reply.ParseFrom(x)));
        var service = ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetContractInfo",BytesMessage.Parser,SmartContractDataWrapper.Parser),
                (BytesMessage request,ServerCallContext _) => {
                    Assert.True(escrowRelease);Assert.Equal(escrowBytes,request.Value);
                    return Task.FromResult(new SmartContractDataWrapper{SmartContract=new SmartContract{ContractAddress=escrowBytes},Runtimecode=runtimeCode});
                })
            .AddMethod(Rpc(Wallet.Descriptor.FullName, "GetBlockByNum2", NumberMessage.Parser, BlockExtention.Parser),
                (NumberMessage _, ServerCallContext _) => Task.FromResult(new BlockExtention { Blockid = ByteString.CopyFrom(new byte[32]) }))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName, "GetBlockByNum2", NumberMessage.Parser, BlockExtention.Parser),
                (NumberMessage _, ServerCallContext _) => Task.FromResult(new BlockExtention { Blockid = ByteString.CopyFrom(new byte[32]) }))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName, "GetAccount", Account.Parser, Account.Parser),
                (Account request, ServerCallContext _) => { Assert.Equal(sourceBytes, request.Address); return Task.FromResult(authority); })
            .AddMethod(Rpc(Wallet.Descriptor.FullName, "CreateTransaction2", TransferContract.Parser, TransactionExtention.Parser),
                (TransferContract request, ServerCallContext _) => {
                    constructions++; Assert.Equal(sourceBytes, request.OwnerAddress); Assert.Equal(destinationBytes, request.ToAddress);
                    Assert.False(token); Assert.Equal(1230000, request.Amount);
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = now, Expiration = now + 60000 } };
                    transaction.RawData.Contract.Add(new Transaction.Types.Contract { Type = Transaction.Types.Contract.Types.ContractType.TransferContract,
                        Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(request) });
                    return Task.FromResult(new TransactionExtention { Transaction = transaction, Result = new Return { Result = true } });
                })
            .AddMethod(Rpc(Wallet.Descriptor.FullName, "TriggerConstantContract", TriggerSmartContract.Parser, TransactionExtention.Parser),
                (TriggerSmartContract request, ServerCallContext _) => {
                    Assert.True(token); Assert.Equal(sourceBytes, request.OwnerAddress);
                    if(escrowRelease){
                        byte[] Word(int value){var data=new byte[32];new System.Numerics.BigInteger(value).ToByteArray(true,true).CopyTo(data,32-new System.Numerics.BigInteger(value).ToByteArray(true,true).Length);return data;}
                        var call=Convert.ToHexString(request.Data.Span[..4]);byte[] data;
                        string Signature(string value)=>new Nethereum.Util.Sha3Keccack().CalculateHash(value)[..8].ToUpperInvariant();
                        if(call==Signature("deposits(bytes32)")){
                            Assert.Equal(escrowBytes,request.ContractAddress);Assert.Equal(nativeDepositId,request.Data.ToByteArray()[4..]);
                            data=new byte[128];sourceBytes.Span[1..].CopyTo(data.AsSpan(12,20));
                            NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(tokenAddress).AsSpan(1).CopyTo(data.AsSpan(44,20));
                            Word(123).CopyTo(data,64);data[127]=(byte)(released?1:0);
                        }else if(call==Signature("escrowProtocolVersion()")){Assert.Equal(escrowBytes,request.ContractAddress);data=Word(1);}
                        else{
                            Assert.Equal(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(tokenAddress),request.ContractAddress.ToByteArray());
                            if(call==Signature("totalSupply()"))data=Word(1000);
                            else {Assert.Equal(Signature("balanceOf(address)"),call);data=Word(request.Data.Span[16..36].SequenceEqual(sourceBytes.Span[1..])?(released?1000:877):(released?0:123));}
                        }
                        var output=new TransactionExtention{Result=new Return{Result=true}};output.ConstantResult.Add(ByteString.CopyFrom(data));return Task.FromResult(output);
                    }
                    Assert.Equal(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(tokenAddress), request.ContractAddress.ToByteArray());
                    Assert.Equal("313CE567", Convert.ToHexString(request.Data.ToByteArray()));
                    var decimals = new byte[32]; decimals[31] = 2;
                    var response = new TransactionExtention { Result = new Return { Result = true } };
                    response.ConstantResult.Add(ByteString.CopyFrom(decimals)); return Task.FromResult(response);
                })
            .AddMethod(Rpc(Wallet.Descriptor.FullName, "TriggerContract", TriggerSmartContract.Parser, TransactionExtention.Parser),
                (TriggerSmartContract request, ServerCallContext _) => {
                    Assert.True(token); constructions++; Assert.Equal(sourceBytes, request.OwnerAddress);
                    Assert.Equal(escrowRelease?escrowBytes.ToByteArray():NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(tokenAddress), request.ContractAddress.ToByteArray());
                    var data = request.Data.ToByteArray();
                    if(escrowRelease){
                        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("unlock(bytes32)")[..8].ToUpperInvariant(),Convert.ToHexString(data.AsSpan(0,4)));
                        Assert.Equal(nativeDepositId,data[4..]);
                    }else if (selector == "mint-nft") {
                        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("mint(address,uint256,string)")[..8].ToUpperInvariant(), Convert.ToHexString(data.AsSpan(0,4)));
                        Assert.Equal(new System.Numerics.BigInteger(1), new System.Numerics.BigInteger(data.AsSpan(36,32),true,true));
                        Assert.Equal(new System.Numerics.BigInteger(96), new System.Numerics.BigInteger(data.AsSpan(68,32),true,true));
                        var length = (int)new System.Numerics.BigInteger(data.AsSpan(100,32),true,true);
                        Assert.Equal("https://example.invalid/owned-nft/1", System.Text.Encoding.UTF8.GetString(data,132,length));
                    } else if(selector=="burn-token-await"){
                        Assert.Equal(36,data.Length);Assert.Equal("42966C68",Convert.ToHexString(data.AsSpan(0,4)));
                        Assert.Equal(new System.Numerics.BigInteger(123),new System.Numerics.BigInteger(data.AsSpan(4),true,true));
                    } else {
                    Assert.Equal(68, data.Length);
                    Assert.Equal(mintToken ? "40C10F19" : "A9059CBB", Convert.ToHexString(data.AsSpan(0, 4)));
                    Assert.Equal(new System.Numerics.BigInteger(123), new System.Numerics.BigInteger(data.AsSpan(36), isUnsigned: true, isBigEndian: true));
                    }
                    if(!escrowRelease&&selector!="burn-token-await")Assert.Equal((mintToken ? sourceBytes : destinationBytes).ToByteArray()[1..], data.AsSpan(16, 20).ToArray());
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = now, Expiration = now + 60000 } };
                    transaction.RawData.Contract.Add(new Transaction.Types.Contract { Type = Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
                        Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(request) });
                    return Task.FromResult(new TransactionExtention { Transaction = transaction, Result = new Return { Result = true } });
                })
            .AddMethod(Rpc(Wallet.Descriptor.FullName, "BroadcastTransaction", Transaction.Parser, Return.Parser),
                (Transaction request, ServerCallContext _) => {
                    broadcasts++; transactionId = ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));
                    if(selector is "mint-await" or "send-token-await")Assert.Equal("Original seed 🌱 memo",request.RawData.Data.ToStringUtf8());
                    TronPreparationVerifier.VerifyWithAccountAuthority(Convert.ToBase64String(request.ToByteArray()),
                        Convert.ToHexString(transactionId.ToByteArray()).ToLowerInvariant(), sourceBytes.ToByteArray(), 100000000, authority);
                    if(escrowRelease)released=true;
                    mutateMintRequest?.Invoke();
                    return Task.FromResult(new Return { Result = true });
                })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName, "GetTransactionInfoById", BytesMessage.Parser, TransactionInfo.Parser),
                (BytesMessage request, ServerCallContext _) => {
                    receipts++; Assert.Equal(transactionId, request.Value);
                    var receipt = new TransactionInfo { Id = transactionId, BlockNumber = 1,
                        Receipt = new ResourceReceipt { Result = Transaction.Types.Result.Types.contractResult.Success } };
                    if (token) { var word = new byte[32]; word[31] = 1; receipt.ContractResult.Add(ByteString.CopyFrom(word)); }
                    return Task.FromResult(receipt);
                }).Build();
        var server = new Server { Services = { service }, Ports = { new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure) } };
        server.Start();
        var completed = false;
        try
        {
            Assert.False((await local.ActivateProviderAsync()).IsError);
            foreach (var id in new[] { sender, recipient }) {
                var prior = await local.LoadProviderWalletsForAvatarByIdAsync(id);
                Assert.False(prior.IsError, prior.Message); Assert.True(prior.Result == null || prior.Result.Count == 0, "Preserve prior fixture; inspect before retry.");
                var avatar = await local.LoadAvatarAsync(id);
                Assert.Null(avatar.Result);
            }
            async Task Save(Guid id, string address, string secret) {
                var wallet = new ProviderWallet { Id = Guid.NewGuid(), AvatarId = id, ProviderType = ProviderType.TRONOASIS,
                    IsDefaultWallet = true, WalletAddress = address, PrivateKey = secret };
                var saved = await local.SaveProviderWalletsForAvatarByIdAsync(id, new() { [ProviderType.TRONOASIS] = new List<IProviderWallet> { wallet } });
                Assert.False(saved.IsError, saved.Message);
            }
            await Save(sender, owner, encrypted); await Save(recipient, destination, "");
            foreach (var pair in new[] { (sender, "tron-avatar-sender"), (recipient, "tron-avatar-recipient") }) {
                var saved = await local.SaveAvatarAsync(new Core.Holons.Avatar { Id = pair.Item1,
                    Username = pair.Item2, Email = pair.Item2 + "@example.invalid", IsActive = true });
                Assert.False(saved.IsError, saved.Message);
            }
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings {
                Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local);
            Assert.False(providers.SetAndActivateCurrentStorageProvider(local).IsError);
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 4);
            var wallets = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            var port = server.Ports.Single().BoundPort;
            await using var runtime = new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"), new Uri($"http://127.0.0.1:{port}"), genesis);
            var tron = new TRONOASIS(contractAddress: mintToken ? tokenAddress : "storage-contract-must-not-be-used-as-token", walletManager: wallets, nativeTransfers: runtime.NativeTransfers,
                idBoundEscrow:escrowRelease?new(escrowAddress,runtimeHash):null,
                nativeSigningKeyResolver: _ => throw new InvalidOperationException("Avatar send must not use raw-address key resolution."));
            // Official SDK signing against controlled RPC, without bypassing provider activation state.
            Assert.False(tron.IsProviderActivated);
            if(escrowRelease)Assert.True(providers.RegisterProvider(tron));
            async Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>> MintNft() {
                var request = new Core.Objects.NFT.Requests.MintWeb3NFTRequest { MintedByAvatarId = sender, Amount = 1, NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false, JSONMetaDataURL = "https://example.invalid/owned-nft/1", SendToAddressAfterMinting = destination,
                    MetaData = new() { ["MintFunctionSignature"] = "mint(address,uint256,string)", ["TokenAddress"] = tokenAddress, ["TokenId"] = "1" } };
                mutateMintRequest = () => { request.JSONMetaDataURL = "https://changed.invalid/not-submitted"; request.MintedByAvatarId = Guid.Empty; };
                var minted = synchronous ? tron.MintNFT(request) : await tron.MintNFTAsync(request);
                Assert.False(minted.IsError,minted.Message);
                Assert.Equal(tokenAddress + "/1",minted.Result.Web3NFT.NFTTokenAddress);
                Assert.Equal("https://example.invalid/owned-nft/1",minted.Result.Web3NFT.JSONMetaDataURL);
                Assert.Equal(sender,minted.Result.Web3NFT.MintedByAvatarId);
                Assert.Equal(destination,minted.Result.Web3NFT.SendToAddressAfterMinting);
                Assert.Same(minted.Result.Web3NFT,Assert.Single(minted.Result.Web3NFTs));
                return new() { Result = minted.Result };
            }
            async Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>> ReleaseEscrow(){
                var request=new Core.Objects.Wallet.Requests.UnlockWeb4TokenRequest{UnlockedByAvatarId=sender,LockId=depositId,TokenAddress=tokenAddress,EscrowContractAddress=escrowAddress,
                    ProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.TRONOASIS)};
                mutateMintRequest=()=>{request.UnlockedByAvatarId=recipient;request.LockId=Guid.Empty;request.TokenAddress=owner;};
                var response=selector=="escrow-manager-release"
                    ?synchronous?wallets.UnlockToken(request):await wallets.UnlockTokenAsync(request)
                    :synchronous?tron.UnlockToken(request):await tron.UnlockTokenAsync(request);
                Assert.False(response.IsError,response.Message);var native=Assert.IsType<TronIdBoundEscrowResponse>(response.Result);
                Assert.Equal(depositId,native.LockId);Assert.Equal(native.TransactionResult,Assert.Single(native.NativeTransactions));
                Assert.True(response.IsSaved);Assert.Equal(1,response.SavedCount);return response;
            }
            async Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>> MintToken(){
                var request=new Core.Objects.Wallet.Requests.MintWeb3TokenRequest {MintedByAvatarId=sender,Amount=1.23m,
                    MemoText=selector=="mint-await"?"Original seed 🌱 memo":null};
                if(local is not HederaTokenIntentTests.GatedWalletStore gate)return synchronous?tron.MintToken(request):await tron.MintTokenAsync(request);
                var pending=synchronous?Task.Run(()=>tron.MintToken(request)):tron.MintTokenAsync(request);
                try{
                    await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.False(pending.IsCompleted);
                    request.Amount=99;request.MemoText="changed";request.MintedByAvatarId=recipient;
                    gate.Resume.TrySetResult();return await pending;
                }finally{gate.Resume.TrySetResult();await pending;}
            }
            async Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>> TokenOperationAwait(){
                var request=new Core.Objects.Wallet.Requests.SendWeb3TokenRequest {FromTokenAddress=token?tokenAddress:"TRX",
                    FromWalletAddress=owner,ToWalletAddress=destination,Amount=1.23m,OwnerPrivateKey=key,OwnerSeedPhrase="",OwnerPublicKey="",
                    MemoText="Original seed 🌱 memo"};
                var burn=new Core.Objects.Wallet.Requests.BurnWeb3TokenRequest {TokenAddress=tokenAddress,Amount=1.23m,
                    OwnerPrivateKey=key,OwnerPublicKey="",OwnerSeedPhrase=""};
                var gate=(SemaphoreSlim)typeof(TRONOASIS).GetField("_runtimeLifecycle",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tron)!;
                await gate.WaitAsync();var released=false;
                Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>>? pending=null;
                try{
                    pending=selector=="burn-token-await"?tron.BurnTokenAsync(burn):tron.SendTokenAsync(request);Assert.False(pending.IsCompleted);
                    request.FromWalletAddress=destination;request.ToWalletAddress=owner;request.Amount=99;
                    request.MemoText="changed";request.FromTokenAddress=destination;
                    burn.Amount=99;burn.TokenAddress=destination;
                    gate.Release();released=true;return await pending;
                }finally{if(!released)gate.Release();if(pending!=null)await pending;}
            }
            var result = selector is "send-token-await" or "burn-token-await"?await TokenOperationAwait():escrowRelease?await ReleaseEscrow():selector == "mint-nft" ? await MintNft() : mintToken
                ? await MintToken()
                : selector == "username"
                ? token
                    ? synchronous ? tron.SendTransactionByUsername("tron-avatar-sender", "tron-avatar-recipient", 1.23m, tokenAddress)
                        : await tron.SendTransactionByUsernameAsync("tron-avatar-sender", "tron-avatar-recipient", 1.23m, tokenAddress)
                    : synchronous ? tron.SendTransactionByUsername("tron-avatar-sender", "tron-avatar-recipient", 1.23m)
                        : await tron.SendTransactionByUsernameAsync("tron-avatar-sender", "tron-avatar-recipient", 1.23m)
                : selector == "email"
                ? token
                    ? synchronous ? tron.SendTransactionByEmail("tron-avatar-sender@example.invalid", "tron-avatar-recipient@example.invalid", 1.23m, tokenAddress)
                        : await tron.SendTransactionByEmailAsync("tron-avatar-sender@example.invalid", "tron-avatar-recipient@example.invalid", 1.23m, tokenAddress)
                    : synchronous ? tron.SendTransactionByEmail("tron-avatar-sender@example.invalid", "tron-avatar-recipient@example.invalid", 1.23m)
                        : await tron.SendTransactionByEmailAsync("tron-avatar-sender@example.invalid", "tron-avatar-recipient@example.invalid", 1.23m)
                : token
                ? synchronous ? tron.SendTransactionById(sender, recipient, 1.23m, tokenAddress)
                    : await tron.SendTransactionByIdAsync(sender, recipient, 1.23m, tokenAddress)
                : synchronous ? tron.SendTransactionById(sender, recipient, 1.23m)
                    : await tron.SendTransactionByIdAsync(sender, recipient, 1.23m);
            Assert.False(result.IsError, result.Message);
            Assert.Equal(Convert.ToHexString(transactionId.ToByteArray()).ToLowerInvariant(), result.Result.TransactionResult);
            Assert.Equal(1, constructions); Assert.Equal(1, broadcasts); Assert.Equal(1, receipts);
            Assert.False(tron.IsProviderActivated);
            var retained = await local.LoadProviderWalletsForAvatarByIdAsync(sender);
            Assert.False(retained.IsError, retained.Message);
            Assert.Equal(encrypted, retained.Result[ProviderType.TRONOASIS].Single().PrivateKey);
            completed = true;
        }
        finally
        {
            try {
                if (completed) foreach (var id in new[] { sender, recipient }) {
                    Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(id, new())).IsError);
                    Assert.False((await local.DeleteAvatarAsync(id, softDelete: false)).IsError);
                }
            } finally { try { await local.DeActivateProviderAsync(); } finally { await server.ShutdownAsync(); } }
        }
    }
}
