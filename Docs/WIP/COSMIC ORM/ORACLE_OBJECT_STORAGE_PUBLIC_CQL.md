# Oracle Object Storage public metadata CQL

The teaching profile maps exact object keys from Oracle's published
`workshop-data` bucket into public logical Holons. It uses native
anonymous HEAD requests and returns the object size, ETag, last-modified
time and media type. A reviewed key catalogue bounds CQL equality and
membership. The scoped profile is independent of owned OCI SDK storage.
[Oracle workshop objects](https://docs.oracle.com/en-us/iaas/Content/bigdata/get-started-odh/get-started.htm) ·
[Oracle HeadObject](https://docs.oracle.com/en-us/iaas/Content/Object/Tasks/managingobjects_topic-To_get_object_details.htm)

6 November 2026 native qualification returned HTTP 200 for these three
published objects in the Phoenix region.

| Key | Bytes | ETag |
| --- | ---: | --- |
| `bds-livelabs/env.sh` | 534 | `ee15eb88-80e0-45a1-b8a1-a3121e1477fe` |
| `bds-livelabs/download-all-hdfs-data.sh` | 353 | `5aaf4866-3849-4d93-927a-ee2f9065fb27` |
| `bds-livelabs/download-all-objstore.sh` | 745 | `ff292829-0dff-4b3f-85fb-7a538e880ebf` |

The list request returned 404. The teaching profile uses the three exact
object keys through HEAD. Eight controlled cases and one real CQL 3/2
test passed at Core `e400afd53cef4997e8bfcabc4b34b3f84a854626`.
The TRX SHA-256 is
`ef955952294fb29ffa205baa95ae8c205fef7bab063ba011651e2f590c5475a5`.
WEB4 enrollment and live verification remain open. LocalFile remains
first for live rollout.
