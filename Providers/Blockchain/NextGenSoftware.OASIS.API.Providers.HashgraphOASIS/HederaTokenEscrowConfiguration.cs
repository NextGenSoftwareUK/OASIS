using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public enum HederaTokenEscrowProtocol { AggregateV1 = 0, IdBoundV1 = 1 }

    public sealed class HederaTokenEscrowConfiguration
    {
        public string ContractId { get; set; }
        public string RuntimeCodeHash { get; set; }
        public long GasLimit { get; set; }
        public long MaxFeeTinybars { get; set; }
        public HederaTokenEscrowProtocol Protocol { get; set; }

        internal void Validate()
        {
            if (!Regex.IsMatch(ContractId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z")
                || !Regex.IsMatch(RuntimeCodeHash ?? "", @"\A0x[0-9a-fA-F]{64}\z")
                || GasLimit <= 0 || GasLimit > 9007199254740991L || MaxFeeTinybars <= 0
                || !Enum.IsDefined(typeof(HederaTokenEscrowProtocol), Protocol))
                throw new InvalidOperationException("An explicit Hedera fungible escrow contract, verified runtime code hash, positive gas limit and fee ceiling are required.");
        }
    }
}
