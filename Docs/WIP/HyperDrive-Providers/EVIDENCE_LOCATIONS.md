# Evidence locations and migration boundary

Updated 2026-10-27. New raw evidence belongs under `TestResults/HyperDrive-Providers/`, not docs. This checkpoint changes no code or tests.

Reference audit: 296 files / 8,316,334 bytes; 269 retained because source, registry, docs or preserved-history references exist; 27 unreferenced files / 633,925 bytes moved unchanged.

The scan covered repository text, archived history and nested wildcard mentions; dependency/build/cache folders and TestResults were excluded. Self-references did not count. Linked aggregates and source-review JSON consumed by the generator remain in the legacy NativeEvidence directory.

Do not generate new raw evidence until writers and their references are migrated in a separate code change. Preserve historical links; do not rewrite archives to make a move appear safe.

Reference metadata: `TestResults/HyperDrive-Providers/documentation-evidence-reference-scan.json`; SHA-256 `f8d631a8f0bd04c941a5256d15f61a62c70d141660a01772a1745d33a8b93901`.
Move manifest: `TestResults/HyperDrive-Providers/documentation-evidence-moves.json`; SHA-256 `cb4fa66524fc21b680dcb8b3ee7945a30ad233ec989e3c550d55695c378b2dbb`.

Files below moved from `Docs/WIP/HyperDrive-Providers/NativeEvidence/<relative name>` to `TestResults/HyperDrive-Providers/NativeEvidence/<relative name>`. Nested paths are preserved. TestResults is local/ignored; tracked originals remain recoverable from Git history. No history rewrite or evidence payload deletion.

| Relative name | Bytes | SHA-256 |
|---|---:|---|
| aws-dynamo-credential-read-probe.json | 231 | `a83843ba9c0b2d1d6d6df5bab353c7552c2dc337eebfb262ea6d56fa6fd3ed0a` |
| aws-dynamo-final-store-inspection.json | 957 | `73004f8164ab40c7a7ad68dc37e1ffa707f20d5f0b74fcd6126f8107d19df9b2` |
| aws-dynamo-initial-identities.json | 3003 | `97fc87bfcf15950655df962516c662dfe18ff21286a50d82376c6ab0805553b0` |
| conformance-activitypuboasis-initial-read-ack.json | 418 | `a4466f82661f948f4e1c2a2414a5b9514f27d8f4d46327bab22c6351b0af39f9` |
| conformance-awsoasis-initial-activation.json | 1845 | `db27cca29ceeb2cf2b99134096541ad780a332063ae07e241df3770a51f264b2` |
| conformance-awsoasis-initial-configuration.json | 1345 | `102631e13e2f7a10f8350841e2de265cc3c3e348eed7b8103690bbe3db819bf0` |
| conformance-dynamodboasis-initial-activation.json | 1855 | `b0bbdba00f3ac50b56e2dfb7d8ae652496d695988ff0e17a9f942855d9ce7d47` |
| conformance-dynamodboasis-initial-configuration.json | 1355 | `8aa673690a47e39eb9f3e321782e64cf9342106fcc6054f7124513870309985d` |
| conformance-postgresqloasis-initial-v1.json | 1653 | `53976a4b3d24ce48592c7192b1c21b7b9e7cbe174c6e3a245a3f202fc9fb0594` |
| conformance-postgresqloasis-initial-v2-visibility.json | 3043 | `5658605298db79cae5e8a1a3f8b5fc9f99467219d986bdf7d9a70f4b1206532e` |
| conformance-postgresqloasis-initial-v2.json | 2525 | `9f5969b50ade23e6ece0cf16d7db1511d2f4208cc4915132bf5f8830c5478570` |
| evm-family-current.trx.gz | 2586 | `745cef2724054718d40c2a7a3864d4fa7da41497dca10a4f5c7fd70158b5c887` |
| near-prepared-storage-preack-reconciliation.json | 622 | `267de7373e1af4ee91a88e252d82b78facf02789f4255116cd1d9f8ceadb1af0` |
| near-storage-mutation-current.trx | 537796 | `c13d5883a9585bfa4f41fc6e9adef42b0de7c550064430542976236bbb3c58ee` |
| near-storage-mutation-red.trx | 49933 | `93d22375a75250fc9f802378c91b133cb2f0cc9073a14698bf6dd69c866cb51e` |
| Polkadot/psp22-public-approval.completed.json | 5071 | `8fc996750f03b16a3527df5868d600aeaafef1a32ced1f11b4afcbf94ffb202a` |
| Polkadot/psp22-public-postfix-approval.completed.json | 5071 | `6dfe216591e9b8000d5b3be44ced15fd5ae1d4437f9e6c9ce4395d803cb32a5c` |
| Polkadot/psp22-public-postfix-native-current.trx | 2865 | `8ae45554510a28861c1205b36b1f469f04ae33dfdb8b4731763fd69c6dd17e28` |
| Polkadot/psp22-public-postfix-recovery-current.trx | 2865 | `0e125796c07588a79f9c16651959e58f7fde37b15672274766c0c4fafc35f788` |
| Polkadot/psp22-test-token-constructor-simulation-current.json | 957 | `61f536df2701c28d319e1372447a20b3cd411c63bfe3cd78d01a4561044754a3` |
| Polkadot/psp34-escrow-constructor-simulation-current.json | 1252 | `4c6ef137aee57d6a9a5c371793e05d1d0522e53658ebca9880c763c9bc08d06b` |
| Polkadot/psp34-escrow-public-native-current.trx.gz | 1211 | `b005fb7941a1b9f90e48d2ec3a7f2e20c8dbbdf2bed8b7e5d478ed7978881e44` |
| Polkadot/psp34-escrow-public-recovery-current.trx.gz | 1207 | `c6fb81f4210f92a0f0074cff0d83e3e0852e068c7306477525e99a75d8e6dfd1` |
| Polkadot/psp34-onode-producer-identities.json | 710 | `e1ef4eee841c0b5688528472f5afa80ee43ad671f87dddabefba201e38fc400f` |
| Polkadot/psp34-public-api-producer-identities.json | 1169 | `dd9cd374bb545a3e7edc3bdfff2ebd5a4b3ea0690aff4f433d46de682ff96f60` |
| postgres-v1-report-retirement.json | 644 | `76191cda70a6a147066a2659f3c70aa8f175f418cdca3e898bfe6141d56da650` |
| solid-core-native-current.trx.gz | 1736 | `82c997e71c98abf08827caf86e3cdaad689942e544507e65fd3ea25f8633e9d7` |

This index is metadata only. Use the move manifest for exact paths/tracked status and look up a specific artifact instead of loading raw reports during recovery.
