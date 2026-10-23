# NextGen Software WEB4 OASIS API Oracle Database OASIS Provider

WEB4 OASIS API Oracle Database OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integrate into the OASIS.

This provider uses the real Oracle.ManagedDataAccess.Core client with indexed lookup columns and canonical JSON records in CLOBs. It targets Oracle Database, including applicable OCI-hosted database services; it does not implement OCI Object Storage. Hosted service, wallet/IAM and full interface acceptance must be verified separately.

## Configuration and verification

### Identity column compatibility

`USERNAME` and `EMAIL` in `OASIS_AVATARS` and `OASIS_AVATAR_DETAILS` require explicit character length semantics with capacity of at least 256 characters. New tables use `VARCHAR2(256 CHAR)`. Existing byte-semantic identity columns fail activation with a provisioning-review error; activation does not silently alter them or change the database character set.

Before upgrading an existing schema, have its DBA inspect data lengths, character representability, indexes and maintenance requirements. Do not blindly narrow wider columns. Oracle DDL commits independently, so a schema change is not an ordinary rollback-safe data transaction. The guarded local empty-test-schema procedure is not a general production migration. Unicode acceptance was tested on AL32UTF8; other database character sets and hosted configurations require their own verification.

### Write outcomes

An original ORA-12899 returned by the canonical DML statement before its row acknowledgement is an explicit capacity rejection, retaining the original Oracle exception. A matching error number after DML acknowledgement is not treated as proof of rejection: commit/transport uncertainty still requires reconciliation before replay. Rejected work can consume database resources even when it commits no record.

### Connection settings

Configure `OASIS.StorageProviders.OracleDBOASIS.ConnectionString` through the host's secure configuration. `IsVersionControlEnabled` defaults to false; enabling it creates version archives. Existing archives are preserved when creation is disabled. Historical profile reads do not authorize historical credentials: verification/reset/refresh token selectors require version zero.

Activation currently initializes/verifies SQL tables and indexes. Use only a database/schema where that provisioning is explicitly authorized. Real SQL tests reuse one retained local Oracle Free instance; they do not certify hosted Autonomous Database or every provider/HyperDrive interface. Current scoped evidence and remaining gates are tracked in [the working verification report](../../../Docs/WIP/HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md).

Initial Release targeting OASIS API v2.0.0: \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
