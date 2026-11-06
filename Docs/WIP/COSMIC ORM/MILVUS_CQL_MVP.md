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
The qualification workflow runs a separate CQL test against Milvus 2.6.26
with fresh synthetic data on a disposable Linux runner.
[Native qualification workflow](../../../.github/workflows/qualify-milvus-native.yml)

WEB4 enrollment, owned source qualification, live Mongo federation and
browser acceptance remain open. LocalFile remains first for live rollout.
