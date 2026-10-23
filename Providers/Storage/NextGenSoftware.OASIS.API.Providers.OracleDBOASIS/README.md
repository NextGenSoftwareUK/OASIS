# NextGen Software WEB4 OASIS API Oracle Database OASIS Provider

WEB4 OASIS API Oracle Database OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integrate into the OASIS.

This provider uses the real Oracle.ManagedDataAccess.Core client with indexed lookup columns and canonical JSON records in CLOBs. It targets Oracle Database, including applicable OCI-hosted database services; it does not implement OCI Object Storage. Hosted service, wallet/IAM and full interface acceptance must be verified separately.

## Configuration and verification

Successful storage reads set `IsLoaded=true`. Point reads count one returned record; collection/export reads count returned roots, not recursively nested children. A successful empty collection has `LoadedCount=0` and remains loaded. Search counts its returned avatar/holon roots. Missing-record and failed reads do not claim loaded records. These response annotations do not change the query, persisted data, logical history or encryption policy.

Holon deletion sets `IsDeleted=true` and `DeletedCount=1` only after an acknowledged commit. Repeating a soft delete on an existing inactive tombstone acknowledges that state with `DeletedCount=0` and no new mutation. An uncertain commit does not claim deletion success or a count. The provider's boolean soft/hard-delete helpers preserve the native operation's result metadata through the existing result-copy helper. The `IOASISStorageProvider.DeleteHolon` GUID/provider-key, synchronous/asynchronous aliases perform **hard deletion**, matching HyperDrive's shared dispatch contract. HolonManager implements soft deletion through a tombstone Save; direct provider callers can explicitly select it with `DeleteHolonWithSoftDelete*(..., true)`.

Known point-read or delete-target absence uses `OASISResultErrorCodes.RecordNotFound` with no payload or native exception. Direct reads retain an unloaded error result; absent deletes do not acknowledge mutation, increment deletion counts or commit. V2's existing manager layer can distinguish that state from an outage and honor explicit `allowMissing` behavior, including first-use settings/karma ledger creation. Malformed keys, invalid versions, corrupt records and native failures are not relabeled as absence. Empty collection reads retain their normal successful-empty semantics.

### Identity column compatibility

`USERNAME` and `EMAIL` in `OASIS_AVATARS` and `OASIS_AVATAR_DETAILS` require explicit character length semantics with capacity of at least 256 characters. New tables use `VARCHAR2(256 CHAR)`. Existing byte-semantic identity columns fail activation with a provisioning-review error; activation does not silently alter them or change the database character set.

Before upgrading an existing schema, have its DBA inspect data lengths, character representability, indexes and maintenance requirements. Do not blindly narrow wider columns. Oracle DDL commits independently, so a schema change is not an ordinary rollback-safe data transaction. The guarded local empty-test-schema procedure is not a general production migration. Unicode acceptance was tested on AL32UTF8; other database character sets and hosted configurations require their own verification.

### Write outcomes

An original ORA-12899 returned by the canonical DML statement before its row acknowledgement is an explicit capacity rejection, retaining the original Oracle exception. A matching error number after DML acknowledgement is not treated as proof of rejection: commit/transport uncertainty still requires reconciliation before replay. Rejected work can consume database resources even when it commits no record.

### Connection settings

Configure `OASIS.StorageProviders.OracleDBOASIS.ConnectionString` through the host's secure configuration. `IsVersionControlEnabled` defaults to false; enabling it creates version archives. Existing archives are preserved when creation is disabled. Historical profile reads do not authorize historical credentials: verification/reset/refresh token selectors require version zero.

Activation first checks the six required table names in the current owner's `USER_TABLES`, using the same schema definitions as validation. If all exist, activation validates their column and primary-key invariants without entering provisioning or issuing DDL. Secondary tuning indexes are not silently recreated or certified on this path; a DBA should review their suitability. This adds one catalog query per activation, not per record operation.

Additional user-declared columns, including invisible columns, must permit the provider's explicit INSERT column lists: nullable, defaulted, identity-generated or virtual columns are accepted; a required stored extra column without a default is rejected before activation succeeds. This is structural admission, not proof that arbitrary default expressions, triggers or extra constraints will succeed for every input. Activation never repairs the schema. System-generated hidden columns are excluded from this user-schema check.

Every required table must be permanent (`TEMPORARY=N`, `DURATION` null). Validation reads these flags in the existing column-metadata query; it does not issue another query, convert temporary tables or alter their data. Session/transaction-scoped tables are rejected because they do not provide persistent storage across provider connections. See [Oracle's table metadata definitions](https://docs.oracle.com/en/database/oracle/oracle-database/19/refrn/ALL_TABLES.html).

If any table is absent, the existing initialization path still creates missing tables/indexes before validating. Use only a database/schema where that provisioning is explicitly authorized; Oracle DDL is not an ordinary rollback-safe data transaction. A validation-only activation is not proof that a particular least-privilege account can perform every storage operation. Real SQL tests reuse one retained local Oracle Free instance; they do not certify hosted Autonomous Database or every provider/HyperDrive interface. Current scoped evidence and remaining gates are tracked in [the working verification report](../../../Docs/WIP/HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md).

Separate schema provisioning from application access. A pre-provisioned schema can activate without `CREATE TABLE`: the local permission fixture verifies effective `CREATE SESSION` only, no roles, unchanged table identities, and avatar/detail/holon saves, indexed lookups and deletion. The schema owner has implicit access to its own tables. This is scoped local evidence, not a blanket privilege prescription for every operation or Autonomous/IAM configuration. The local fixture uses one passwordless schema-only identity through an administrator-authenticated proxy; that test setup is not a recommendation to give production applications administrator credentials. Have the DBA provision/review the intended application schema and cloud authentication separately.

Initial Release targeting OASIS API v2.0.0: \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
