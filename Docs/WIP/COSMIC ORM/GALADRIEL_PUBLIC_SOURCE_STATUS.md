# Galadriel public source status

4 November 2026. The provider's devnet.galadriel.com endpoint failed
TLS hostname validation. The RPC listed by thirdweb reported chain ID
696969, but its block calls for genesis, finalized and heights 1 through 3
returned upstream errors. A chain ID response alone does not verify the
underlying network or qualify source records. Keep this provider outside
the CQL demo until a bounded read and network identity both pass.
No installs or generated files were needed.

[thirdweb Galadriel endpoint and chain ID](https://thirdweb.com/galadriel-devnet)
