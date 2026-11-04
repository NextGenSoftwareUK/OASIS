# Ritual Chain public block source

4 November 2026. The official Ritual Chain RPC returned chain ID 784
(0x310), matching the current developer documentation. The older backend
example lists 1979 for the same hostname. Use the current chain ID and the
observed genesis hash when qualifying this source.

| Height | Block hash |
| --- | --- |
| 0 | 0x220f7b8da21377ee959d6bf3dc6397ffb5ec0737c37b08974c08247d0822ba93 |
| 1 | 0x03b6c4f4649f7fd4b367d70da103106023c04f28e6ee35f751dc987cf4358326 |
| 2 | 0x1e79dae540fc4062296df60238d2102be7d1f13281b8d2b40d153922ee357edb |
| 3 | 0x37bdf0258871d5ba6f8ceddae184447c77f00ab459dc26b763b0b5a08d8dd550 |

Anonymous eth_getBlockByNumber responses matched all requested heights.
This qualifies a bounded public chain read for further adapter work.
Infernet jobs and Ritual Chain precompile execution are separate contracts.
CQL integration and WEB4 federation and hosted acceptance remain open.
No runtime or generated output was needed.

[Current Ritual Chain docs](https://docs.ritualfoundation.org/) ·
[Older backend example](https://github.com/ritual-foundation/ritual-dapp-skills/blob/main/skills/ritual-dapp-backend/SKILL.md)

The guarded .NET reader built with zero errors and verified chain ID and
genesis before reproducing the three block hashes live. It retains exact
RPC timestamp quantities. The smoke used the existing build outputs.

The read-only CQL source built and activated blocks 1, 2, and 3 against
the live Ritual RPC. It projects stable public holon IDs and exact block
hashes and timestamp quantities. WEB4 admission, query execution and
federation remain open.

The guarded hosted publisher passed three configuration tests. It requires
the exact source digest and local federation/cleanup receipt before
accessing Railway. Applying requires the matching reviewed live release.

Ritual admission and local WEB4/Mongo federation passed. Native counts
were 3/2 and federated counts 5/4. The foreign actor saw three public rows
and zero private rows. Two SDK saves and nine receipts were verified.
The test dropped its fixture database, stopped the replica, and the
publisher accepted the source-bound receipt. Hosted acceptance is open.
