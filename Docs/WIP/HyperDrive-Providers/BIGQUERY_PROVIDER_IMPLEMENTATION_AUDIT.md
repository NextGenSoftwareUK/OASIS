# BigQuery provider — implementation audit,24 October 2026

## Acknowledged point saves and truthful bulk outcomes — 24 October

Current full **111controlledPASS,zero skips,590ms**. Three public point-save families now reuse existing Shared.StoredRecordPublicationSnapshot rather than changing caller IDs before SDK dispatch. Generated record ID/provider key remain detached until one-row DML acknowledgement matches the requested project AND exact job ID; post-ack validation preserves newer caller edits. Complete saves publish original caller identity/key with IsSaved/SavedCount1. Null sources reject before SDK access. Existing other-provider dictionary identity/entries remain intact.

The SDK receives an explicit native JobId BEFORE dispatch; actual SDK compressed REST test verifies it is serialized in jobReference. Reconciliation retains requested project/job/record and known location plus original cause. This is a request identity,NOT proof of submission,execution or success. Null/missing/non-one-row/unbound acknowledgements are fenced; mismatched response job/project remain diagnostic cause data,never overwrite the original request identity. Lost responses conservatively require reconciliation,no automatic retry/failover. Post-ack caller publication failure carries WriteAcknowledged=true/SavedCount1 while IsSaved=false. Native job/location durability across crash is still unproven.

Bulk roots now propagate errors/counts instead of discarding member failures and returning a seemingly successful subset. Uncertainty stops later dispatch even when continueOnError=true; earlier acknowledged keys reuse EXISTING KeyValuePartialMutationException. Null-root preflight occurs before any write; valid/empty bulk counts are verified. This is root-level acknowledgement behavior,NOT full captured multi-root intent/child graph support.

Evidence chronology: original42case baseline failed,but Avatar/Detail Name is computed,so those28cases had an invalid caller-edit premise. Only the14Holon baseline cases are claimed as clean before-fix evidence. Replaced fixture edit with actual Description; preserved intermediate45pass/28fixturefail. Corrected54point outcomes plus6null controls pass,then8genuine bulk failures reproduced separately and repaired. Final adds healthy/empty/preflight and wrong-job binding cases. All original reports retained. No hosted job/dataset/table/cloud request occurred; actual SDK transport tests use synthetic credentials/in-memory rejection. Source/PDB Core742/Shared56/provider4/unit6/copies verified. Offline assets refresh reused existing caches and shared helper project,no download/new environment.

## Current scope

This separate Google.Cloud.BigQuery.V2 provider is NOT GoogleCloudOASIS's canonical Firestore backend. GoogleCloudOASIS constructs its optional BigQuery/Cloud Storage clients during activation but those clients do not currently implement its record CRUD. Reusing a client object is not storage parity. No BigQuery registration/configuration references were found in the inspected BootLoader/DNA C#/project files. Do not infer hosted exposure from the provider enum/source existing.

## Query data separated from SQL

The original Esc helper replaced apostrophe with the same apostrophe at runtime; JSON,username,email,GUID and parent values were embedded directly in SQL. Sixteen actual SDK-method-boundary cases genuinely failed. Existing helpers now use official named BigQueryParameter string values for point/identity/parent reads,MERGE and DELETE; GoogleSQL is selected explicitly. The SDK receives exact quote/backslash/Unicode data separately from SQL. Indexed columns are a closed internal mapping; configured quoted project/dataset identifiers reject quote-delimiter,escape and control characters before I/O. Parameters cannot bind identifiers,as explained by [Google's parameterized-query documentation](https://cloud.google.com/bigquery/docs/parameterized-queries).

Full **31controlledPASS,zero skips,327ms**:16public sync/async route cases,8identifier admission cases,2actual managed SDK HTTP serialization cases and5existing metadata tests. The real Google.Cloud.BigQuery.V2 3.9.0 client serializes compressed REST jobs with named STRING parameters,GoogleSQL and synthetic OAuth credentials; the in-memory transport validates the actual gzip header/decoded request and returns controlled400without DNS/cloud dispatch. This proves request construction,NOT BigQuery execution,SQL semantics,IAM,fees or durability. No actual dataset/table/job created.

Original first build failed because the existing provider tests referenced an absent GetProviderVersion method. Added truthful assembly-version reporting and missing provider name/description before query baseline. Two subsequent failures were fixture expectations: parent filters use named value rather than id; real SDK compresses request JSON and emits lowercase named. Original reports retained; correcting those assumptions did not change production SQL behavior or hide a native failure. Core742/provider3/unit5 PDB/source and consumer hashes verified; SDK consumer exactly matches restored pinned package. See inventory for all paths/hashes/resource ownership.

## Open implementation/release gates

- Live native BigQuery queries,DML/job acknowledgement and actual one-row/job-binding acceptance. Controlled fences do not prove server behavior or durable recovery.
- Point caller ownership is repaired/controlled-verified above; prepared-operation integration and restart-safe attempt journaling/admission remain open.
- Complete canonical serialization/encryption/private index fidelity,history/version selection and IsVersionControlEnabled semantics.
- Complete graph/children/option behavior,captured multi-root intent,paired avatar/detail lifecycle and concurrency/identity uniqueness. Root bulk error/partial acknowledgement reporting is now controlled-verified,not native graph acceptance.
- Collection helpers still have LIMIT1000 and cannot claim exhaustive collections/search/exports. Duplicate identities still select latest first row rather than reject ambiguity.
- Activation currently provisions missing dataset/tables; explicit provisioning authority,cost admission,least-privilege credentials and hosted billing evidence must precede live use.
- Production DNA/BootLoader registration,public endpoint acceptance and full HyperDrive V2 remain open. No deployment/merge or full-provider certification from these31tests.

Pinned3.9.0was absent from inspected package/HTTP caches,so recovered once into existing shared cache; GoogleCloud's cached3.7.0was retained unchanged. Same checkout/runtime/test directories,no clone/new environment. Do not create another BigQuery environment or publish credentials in this document.
