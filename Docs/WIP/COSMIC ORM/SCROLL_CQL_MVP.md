# Scroll CQL MVP admission

Updated 29 October 2026. Controls verified; native/restart/paired demo pending.

## Root fixes and proof

The original three-string constructor now supplies declared chain534352 to the
shared encryption boundary, preserving its public signature. An explicit positive
chain-ID overload supports configured networks; zero/negative values reject before
native access. The shared CQL compiler admits Scroll's explicit authorized record
catalogue, preserving immutable ID capture, authority separation and bounded reads.
Unsupported business/metadata predicates, ordering, graph and over-bound catalogues
reject; inactive/cancellation paths behave correctly without opening RPC.

Initial actual reproduction3failed/5passed. Fixed Scroll11passed; affected EVM
family105passed/0failed/0skipped. Test `ScrollCqlTests.cs` in the Ethereum integration
project. Evidence `C:/Source/OASIS-cosmic/TestResults/ScrollCqlCurrent/scroll-family-controls.trx`.
SHA256: `4346fd2d848b8ed83883354cc134f2a29a413f973e9cb68dc28f6c3a605701a0`.

## Exact next step

Reuse existing EVM31338/runtime/protected key. Add journalled Scroll logical contract
fixture; verify native three-record query filtered to two, restart and reread original
hashes without replaying writes. Then verify Mongo-paired C#/HTTP/browser5/4 and
hub preset before inventory proof enrollment. No native/public Scroll network,
sequencer/finality/bridge, central registration or full parity claim yet.
