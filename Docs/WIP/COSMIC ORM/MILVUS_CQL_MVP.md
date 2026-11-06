# Milvus exact-key CQL

The public teaching profile reads canonical Holons from the existing
`oasis_holons` collection. Activation reads a configured public probe.
Each admitted key goes to Milvus REST v2 entity Get with strong consistency
and only the primary key and stored document requested. An empty successful
response represents absence. A native error envelope, duplicate or incorrect
primary key, invalid stored document, or private live record fails the
whole query. The shared bounded query engine handles ID equality and
membership, paging, tombstones and removal of embedded children.
[Milvus REST v2 Get](https://milvus.io/api-reference/restful/v2.6.x/v2/Vector%20%28v2%29/Get.md)

Five controlled cases passed at Core
`10e464dcf58f4624a27e19f12738978ecf0fcb07`. The TRX SHA-256 is
`1470dc8b22db5ebb4b125ae9a92ea6b5efefc01e68dda6792e38552117444331`.
The native Milvus 2.6.26 test passed with fresh synthetic data on a
disposable Linux runner. It returned 3/2/0 rows, denied the private row
and projected out embedded children. The native TRX SHA-256 is
`2524f094e4b063ffb3d0fb23df08709a49251147bdb9a07bb2530f2544eaa85c`.
[Native qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42239734505/job/127966535849)

Opt-in WEB4 admission passed ten Milvus checks and nine GUN/OrbitDB
regressions at Core `67c54b4199187a44c369002b13b188808525cf98`.
The six controlled provider checks include a transport that rejects a
foreign origin, write route, wrong collection and invalid key. The admission
TRX SHA-256 is
`aede9f225427bb69b53a61de08e5a250315ea419140e3edf2abcf80dd58394d4`.
The guarded provider TRX SHA-256 is
`b54df99b824fff884ab1c37773ad98640f5b67501e37ed1461eea221efdee3da`.

The reviewed `CQL_MILVUS_QUERY_ORIGIN`, protected
`CQL_MILVUS_QUERY_READER_TOKEN` and public
`CQL_MILVUS_QUERY_PROBE_ID` enable the profile. Owned source qualification,
live Mongo federation and browser acceptance remain open. LocalFile remains
first for live rollout.

The `MilvusHostedCqlNative` verifier compiled. It takes the reviewed
descriptor through `COSMIC_MILVUS_HOSTED_CQL_SOURCE`, a protected
`CQL_MILVUS_QUERY_READER_TOKEN` and the synthetic private probe ID through
`COSMIC_MILVUS_HOSTED_PRIVATE_ID`. It verifies 3/2/0, private denial and
descriptor stability without writing the source. The build log SHA-256 is
`d2ff02ae9f8a1d39fecf61d75f577568c4091d803fcd75643660455b1de07cf6`.
Credentialed execution remains open.
