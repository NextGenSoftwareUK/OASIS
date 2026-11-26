# NextGen Software WEB4 OASIS API Filecoin OASIS Provider

WEB4 OASIS API Filecoin OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integrate into the OASIS.

Filecoin is the world's largest decentralised storage network, combining verifiable storage proofs with a native blockchain. This provider stores OASIS Avatars, Avatar Details and Holons on the Filecoin EVM (FEVM): each record is written by a signed Ethereum-format transaction to the shared Web3CoreOASIS storage contract and read back through the Lotus Ethereum JSON-RPC (`/rpc/v1`; mainnet chain id 314, Glif public node by default). It shares its storage implementation with the other Web3Core EVM providers and estimates gas per transaction because FEVM meters execution in Filecoin gas units. Fully integrated with the OASIS HOT-Swappable Provider Architecture — switch to or from Filecoin at runtime with zero code changes.

Native storage conformance runs against a disposable local Lotus 2k devnet (chain id 31415926, local test FIL only): `TestProjects/NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.IntegrationTests/Fixture/start-lotus-devnet.sh`.

Initial Release targeting OASIS API v2.0.0: \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
