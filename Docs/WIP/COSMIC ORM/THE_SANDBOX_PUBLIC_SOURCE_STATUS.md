# The Sandbox public contract query qualification — 8 November 2026

Status: native readiness passed; new bounded read-only CQL source/control tests prepared, cloud compilation and native CQL qualification pending. No coverage increment or hosted claim.

Official source: [thesandboxgame/sandbox-smart-contracts](https://github.com/thesandboxgame/sandbox-smart-contracts/tree/cef77fc29d12593fa70fa3a9c6d7e329c666dcc4), pinned commit cef77fc29d12593fa70fa3a9c6d7e329c666dcc4. Recursive tree1109010bytes SHA2566a8f8019c84113e22f073d139893a6406290c68a6582a60a07d803d37a1ad003 reviewed in memory, not cloned. Current packages/deploy/deployments/polygon JSON ABIs provide name()/symbol() on all three contracts:

| Key | Address | Native name | Symbol | Deployment JSON SHA256 |
| --- | --- | --- | --- | --- |
| land | 0x9d305a42a3975ee4c1c57555bed5919889dce63f | Sandbox's LANDs | LAND | c67c35091be563f6a7fec6e571fbc866c8487627cf9bb97e67c40e16d9a53c49 |
| sand | 0xbbba073c31bf03b8acf7c28ef0738decf3695683 | SAND | SAND | 08492fb6bbe7ce73cfb66a33ccf3cdf4d91183c3f53912c9f764592148ebe6cb |
| asset | 0xdbc52cd5b8eda1a7bcbabb838ca927d23e3673e5 | The Sandbox's ASSETs | ASSET | 21aec992991035f010dc38e83aa8b30a7a43a0d3c3f2df41205000f8b963de11 |

One original readiness schedule uses https://polygon-bor-rpc.publicnode.com/, chain0x89 and genesis0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b. Thirteen read-only calls verify chain/genesis, finalized header and canonical-number agreement, then bytecode/name/symbol for each contract using EIP1898 blockHash/requireCanonical. Block0x5c74496/hash0x48b44b6dcf4c9fd26ef358baeea9c9266f65c93836efe834c96977e75e28a118. Raw responses/status/hash retained before parsing in LocalState/COSMIC/RecoveryLaptopResume20261107/TheSandboxPolygonReadiness; acknowledged.json records all code hashes. No retry, transaction, wallet, owner/holdings query or private source write. Bytecode hashes describe observed contract/proxy code, not independent implementation audit or proof of token ownership.

SandboxContractQuerySource reuses the strict bounded shared EVM reader and bounded public key-value CQL compiler. Stable identities bind source owner, TheSandboxOASIS, sandbox/polygon/contracts and one of land/sand/asset. Each record independently verifies chain/genesis and its current finalized/canonical block, deployed code and exact reviewed name/symbol. All seven mandatory calls must pass; unknown/mixed IDs preflight before native transport; selected IDs immutable, maximum3concurrent reads. Metadata includes exact address, name, symbol, blocknumber/hash/genesis and observed code SHA256. Snapshots must retain their first observation as public blocks advance. Source is read-only; scans/writes unavailable. Existing TheSandboxOASIS REST facade unchanged and not certified by this scope.

New source-only test project declares26controlled cases and1explicitly opted-in native CQL3/2 case. XML/count/static checks pass; cloud workflow qualify-sandbox-contract.yml uses pinned dependencies, existing build lease and bounded workers. Cloud compilation/execution, WEB4 registration/configuration tests, real HTTP/Mongo federation/privacy, tariff/admission/presets, image/deployed package, original owned live cohort/HyperDrive replicas and browser acceptance remain required. No new local runtime, SDK, image, database or provider environment.
