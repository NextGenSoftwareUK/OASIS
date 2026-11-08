# Galadriel public source status

4 November 2026. The provider's devnet.galadriel.com endpoint failed
TLS hostname validation. The RPC listed by thirdweb reported chain ID
696969, but its block calls for genesis, finalized and heights 1 through 3
returned upstream errors. A chain ID response alone does not verify the
underlying network or qualify source records. Keep this provider outside
the CQL demo until a bounded read and network identity both pass.
No installs or generated files were needed.

[thirdweb Galadriel endpoint and chain ID](https://thirdweb.com/galadriel-devnet)

## 8 November lifecycle review

The 4 November failed TLS/chain-identity evidence above remains authoritative; no failed endpoint was retried. No native RPC, inference/history request, account operation or SDK install was made during this review.

[Official L1 contracts README](https://github.com/galadriel-ai/contracts/blob/0270dbc82b58e92075463cc052e26b0d2f3ec40a/README.md) explicitly marks the project deprecated as of 09.10.24. The repository is not archived; these are separate states. Pinned README2611bytes SHA2566a17ef47fcbb6d7f217a6233145697074557d70514d92ff2ed82c9f3c48adf0c.

[Current overview](https://docs.galadriel.com/overview) and [quickstart](https://docs.galadriel.com/for-agents-developers/quickstart) describe the separate Sentience verified-inference API at https://api.galadriel.com/v1/verified with account/API-key authentication and history/get-by-hash access. [Official Sentience source](https://github.com/galadriel-ai/Sentience/tree/7e850258ccaca6f1662cd9f1a1a73c2f086420a4) README7345bytes SHA25610385194f14117ac9f08ad25877bab56399648e80c5e47ccdd402838f1b404d7, repository not archived; no README deprecation notice observed. This does not independently prove live service availability. The current marketing homepage describes GPU clusters, so do not conflate marketing, Sentience API documentation and old L1 support or silently replace one native contract with another.

Existing GaladrielOASIS real HttpClient JSON-RPC adapter at devnet.galadriel.com remains unchanged and unqualified. Next engineering gate is a supported capability contract/migration based on primary API/SDK sources, authenticated bounded exact-key read behavior using authorized access, then native/WEB4 federation/privacy/admission/replication/browser evidence. Historical L1 support requires separate chain evidence. No account action is requested while contract review/implementation remains agent-owned. Full SDK/API and ORM parity remains open.

Compact source receipt: LocalState/COSMIC/RecoveryLaptopResume20261107/galadriel-primary-source-reviewed.json. Original attempted new-document creation stopped at its existence guard; existing evidence was read and this review appended, preserving its original outcome. No clone/archive/runtime/model/cluster/container/database or image downloaded. No coverage increment.
