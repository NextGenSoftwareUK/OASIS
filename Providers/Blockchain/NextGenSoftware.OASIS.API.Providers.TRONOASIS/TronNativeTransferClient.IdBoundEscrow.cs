using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TronNet.Protocol;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public sealed partial class TronNativeTransferClient
{
    public sealed record IdBoundEscrowResult(Guid LockId, string TransactionId, string ApprovalTransactionId);
    private sealed record Deposit(ByteString Owner, ByteString Token, BigInteger Amount, bool Released);

    /// <summary>Explicit per-deposit ABI only. Never substitutes aggregate lock-all semantics.</summary>
    public async Task<IdBoundEscrowResult> ExecuteIdBoundEscrowAsync(string ownerAddress, string escrowAddress,
        string tokenAddress, Guid lockId, decimal amount, string privateKey, string runtimeSha256,
        bool unlock, CancellationToken cancellationToken = default)
    {
        if (lockId == Guid.Empty || (!unlock && amount <= 0) || (unlock && amount != 0))
            throw new ArgumentException("A nonempty deposit identity, positive lock amount and zero amount for full-deposit release are required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(runtimeSha256 ?? "", @"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("An explicitly reviewed escrow runtime SHA256 is required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(privateKey ?? "", @"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var owner = _wallet.ParseAddress(ownerAddress);
        var escrow = _wallet.ParseAddress(escrowAddress);
        var token = _wallet.ParseAddress(tokenAddress);
        EncodeAddressCall("validate(address,address,address)", owner, escrow, token);
        if (escrow.Equals(token) || escrow.Equals(owner))
            throw new ArgumentException("A separate token escrow contract is required.");
        await VerifyOwnerSigningAuthorityAsync(owner, privateKey, Transaction.Types.Contract.Types.ContractType.TriggerSmartContract, cancellationToken);
        await VerifyNetworkAsync(cancellationToken);
        var info = await _wallet.GetProtocol().GetContractInfoAsync(new BytesMessage { Value = escrow },
            _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (info.SmartContract == null || !info.SmartContract.ContractAddress.Equals(escrow) || info.Runtimecode.Length == 0
            || !string.Equals(Convert.ToHexString(SHA256.HashData(info.Runtimecode.ToByteArray())), runtimeSha256, StringComparison.OrdinalIgnoreCase))
            throw new System.IO.InvalidDataException("Configured per-deposit escrow runtime identity does not match reviewed code.");
        if (await ReadContractUintAsync(owner, escrow, Selector("escrowProtocolVersion()"), cancellationToken) != 1)
            throw new System.IO.InvalidDataException("Unsupported per-deposit escrow protocol.");

        // Same canonical identity as the existing EVM provider, not platform-endian Guid.ToByteArray().
        var nativeId = SHA256.HashData(Encoding.UTF8.GetBytes(lockId.ToString("N")));
        var query = Selector("deposits(bytes32)").Concat(nativeId).ToArray();
        async Task<Deposit> ReadDeposit() => DecodeDeposit(await QueryContractAsync(ownerAddress, escrowAddress, query, cancellationToken));
        Task<BigInteger> Balance(ByteString address) => ReadContractUintAsync(owner, token, EncodeAddressCall("balanceOf(address)", address), cancellationToken);
        Task<BigInteger> Supply() => ReadContractUintAsync(owner, token, Selector("totalSupply()"), cancellationToken);
        var before = await ReadDeposit();
        var empty = before.Owner.ToByteArray().Skip(1).All(value => value == 0);
        if ((!unlock && (!empty || before.Amount != 0 || before.Released || before.Token.ToByteArray().Skip(1).Any(value => value != 0)))
            || (unlock && (empty || before.Released || !before.Owner.Equals(owner) || !before.Token.Equals(token) || before.Amount <= 0)))
            throw new Core.Helpers.StorageLifecycleConflictException("Deposit identity is used, missing, released or not owned by the requested signer/token.");
        BigInteger units;
        if (unlock) units = before.Amount;
        else
        {
            var decimals = await ReadContractUintAsync(owner, token, Selector("decimals()"), cancellationToken);
            if (decimals > 255) throw new System.IO.InvalidDataException("Token decimals exceed uint8.");
            units = TokenUnits(amount, (int)decimals);
        }
        var ownerBefore = await Balance(owner);
        var escrowBefore = await Balance(escrow);
        var supplyBefore = await Supply();
        if ((!unlock && ownerBefore < units) || (unlock && escrowBefore < units))
            throw new InvalidOperationException("Token custody does not satisfy the explicit deposit operation.");
        string approval = null;
        try
        {
            if (!unlock)
            {
                var allowanceCall = EncodeAddressCall("allowance(address,address)", owner, escrow);
                var allowance = await ReadContractUintAsync(owner, token, allowanceCall, cancellationToken);
                if (allowance != 0 && allowance != units)
                    throw new InvalidOperationException("Reconcile the existing escrow allowance before approving a different amount.");
                if (allowance == 0)
                {
                    approval = await ApproveTokenAsync(ownerAddress, tokenAddress, escrowAddress, units, privateKey, cancellationToken);
                    if (await ReadContractUintAsync(owner, token, allowanceCall, cancellationToken) != units)
                        throw new System.IO.InvalidDataException("Confirmed approval did not establish the exact allowance.");
                }
            }
            if (await ReadDeposit() != before || await Balance(owner) != ownerBefore || await Balance(escrow) != escrowBefore || await Supply() != supplyBefore)
                throw new System.IO.InvalidDataException("Deposit or token custody changed before escrow submission.");
            var call = unlock ? Selector("unlock(bytes32)").Concat(nativeId).ToArray()
                : Selector("lock(bytes32,address,uint256)").Concat(nativeId)
                    .Concat(EncodeAddressCall("address(address)", token).Skip(4)).Concat(UnsignedWord(units)).ToArray();
            var hash = await ExecuteSignedContractAsync(ownerAddress, escrowAddress, call, privateKey, cancellationToken);
            try
            {
                var after = await ReadDeposit();
                if (after != new Deposit(owner, token, units, unlock)
                    || await Balance(owner) != ownerBefore + (unlock ? units : -units)
                    || await Balance(escrow) != escrowBefore + (unlock ? -units : units)
                    || await Supply() != supplyBefore)
                    throw new System.IO.InvalidDataException("Confirmed escrow receipt did not establish exact deposit/custody effects.");
            }
            catch (Exception error) { throw new TronCommittedReadbackException(hash, _genesisBlockId, error); }
            return new(lockId, hash, approval);
        }
        catch (Exception error) when (approval != null)
        {
            throw new TronEscrowReconciliationException(approval, _genesisBlockId, error);
        }
    }

    private static byte[] Selector(string signature) => Convert.FromHexString(new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8]);
    private static byte[] UnsignedWord(BigInteger value)
    {
        if (value < 0 || value >= (BigInteger.One << 256)) throw new ArgumentOutOfRangeException(nameof(value));
        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        bytes.CopyTo(result, result.Length - bytes.Length);
        return result;
    }
    private static Deposit DecodeDeposit(byte[] data)
    {
        if (data.Length != 128 || data.Take(12).Any(value => value != 0) || data.Skip(32).Take(12).Any(value => value != 0)
            || data.Skip(96).Take(31).Any(value => value != 0) || data[127] > 1)
            throw new System.IO.InvalidDataException("Deposit query did not return a canonical owner/token/uint256/bool tuple.");
        ByteString Address(int offset) => ByteString.CopyFrom(new byte[] { 0x41 }.Concat(data.Skip(offset + 12).Take(20)).ToArray());
        return new(Address(0), Address(32), new BigInteger(data.AsSpan(64, 32), isUnsigned: true, isBigEndian: true), data[127] == 1);
    }
}
