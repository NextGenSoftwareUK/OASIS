using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests
{
    public interface IWeb4WalletTransactionRequest : IWeb3WalletTransactionRequest
    {
        //Need at least one of these to identify the sender.
        public Guid FromAvatarId { get; set; }
        public string FromAvatarUsername { get; set; }
        public string FromAvatarEmail { get; set; }

        //Need at least one of these to identify the receiver.
        public Guid ToAvatarId { get; set; }
        public string ToAvatarUsername { get; set; }
        public string ToAvatarEmail { get; set; }
        public bool WaitTillTokenSent { get; set; }
        public int WaitForTokenToSendInSeconds { get; set; }
        public int AttemptToSendTokenEveryXSeconds { get; set; }
        public bool WaitTillTokenBurnt { get; set; }
        public int WaitForTokenToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}