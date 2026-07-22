namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Static facade over <see cref="IDIDChallengeStore"/>.
    /// Defaults to <see cref="InMemoryDIDChallengeStore"/> (suitable for single-node deployments).
    /// Multi-node deployments should call <see cref="SetProvider"/> at startup with a
    /// RedisDIDChallengeStore instance so all ONODE instances share nonce state.
    /// </summary>
    public static class DIDChallengeStore
    {
        /// <summary>Nonce lifetime in seconds (5 minutes). Shared by all store implementations.</summary>
        public const int NonceTtlSeconds = 300;

        private static IDIDChallengeStore _provider = new InMemoryDIDChallengeStore();

        /// <summary>
        /// Replace the active store implementation. Call once at startup before any requests are served.
        /// </summary>
        public static void SetProvider(IDIDChallengeStore provider)
            => _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));

        /// <inheritdoc cref="IDIDChallengeStore.Issue"/>
        public static string Issue(string did) => _provider.Issue(did);

        /// <inheritdoc cref="IDIDChallengeStore.ConsumeIfValid"/>
        public static bool ConsumeIfValid(string did, string nonce) => _provider.ConsumeIfValid(did, nonce);
    }
}
