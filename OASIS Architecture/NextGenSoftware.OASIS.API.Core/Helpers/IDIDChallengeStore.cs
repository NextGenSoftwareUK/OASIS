namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Abstraction over the DID challenge-nonce store.
    /// Swap implementations via <see cref="DIDChallengeStore.SetProvider"/> at startup:
    ///   - <see cref="InMemoryDIDChallengeStore"/> — default, single-node
    ///   - RedisDIDChallengeStore (in ONODE.WebAPI) — multi-node deployments
    /// </summary>
    public interface IDIDChallengeStore
    {
        /// <summary>Generates and stores a new nonce for <paramref name="did"/>. Replaces any existing pending nonce.</summary>
        string Issue(string did);

        /// <summary>
        /// Returns true and removes the nonce if it was issued for <paramref name="did"/> and has not expired.
        /// Returns false if the nonce is unknown, expired, or already consumed.
        /// </summary>
        bool ConsumeIfValid(string did, string nonce);
    }
}
