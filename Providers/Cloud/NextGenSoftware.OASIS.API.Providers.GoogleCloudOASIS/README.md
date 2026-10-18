# NextGen Software WEB4 OASIS API Google Cloud OASIS Provider

GoogleCloudOASIS uses the Google Cloud SDKs. Canonical OASIS avatar/detail/holon persistence uses Firestore; optional Cloud Storage and BigQuery clients are separate capabilities, not covered by Firestore-only evidence.

## Current direct verification

Use the retained local runner `Scripts/TestHosts/Run-ReusableGoogleTests.ps1`. It requires the already-installed Firestore emulator, reuses cached dependencies/fixed outputs, takes an exclusive environment lease and stops its owned Java process afterward. No implicit installation or production credentials. Test results are scoped to native Firestore SDK calls, not hosted cloud authorization, charges or complete provider/interface certification.

Trusted application subclasses are projected into canonical Core DTOs before saving, including selected recursive graph nodes through the shared planner. Stored JSON continues to use the restricted type binder; consumer-only CLR type names are not a persistence contract. Avatar-detail provider keys are stored and published only after acknowledgement. Arbitrary explicit-interface/proxy implementations and extra application-specific fields are not certified by these subclass tests.

Historical reads currently reject positive versions. General concurrency, acknowledgement/recovery, all fields/options/interfaces, Cloud Storage/BigQuery and hosted/API/release gates remain open. Activation can perform service-side health/setup operations; Firestore tests disable Cloud Storage/BigQuery and use the loopback emulator. Current evidence and limitations: `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisplatform.world

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/OASIS

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
