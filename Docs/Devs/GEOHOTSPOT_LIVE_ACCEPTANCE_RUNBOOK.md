# GeoHotSpot live acceptance runbook

**Updated:** 2026-09-20

Use this runbook for signals a desktop test cannot observe: physical location, AR input, target-display rendering, media quality, and failures in a deployed multi-replica environment. Automated contract evidence remains in `GEOHOTSPOT_AUTOMATED_VERIFICATION.md`.

## Prepare

1. Deploy the exact WEB4/WEB5 build under test and record its commit/deployment IDs.
2. Build Our World from the tested commit and record the build ID, device, OS, display mode, and operator.
3. Copy `Scripts/geohotspot-acceptance-evidence.template.json` outside the repository.
4. Use disposable avatars, quests, rewards, and provider data. Do not point destructive fixtures at production identities.
5. Record the client log and API trace for the entire run. Capture the screen for presentation/media cases.

## Required cases

| Case ID | Procedure | Pass condition | Minimum evidence |
|---|---|---|---|
| `location-arrival-radius` | Approach a known hotspot from outside; sample outside, exact boundary, and inside. | Outside is rejected; boundary and inside are accepted once. | GPS trace plus API responses |
| `location-dwell-reset` | Enter, remain short of duration, leave, re-enter, then remain for the full duration. | First stay does not trigger; exit resets progress; second continuous stay triggers once. | Screen recording plus trigger trace |
| `ar-gaze-duration` | Look away before the threshold, then hold continuous gaze through it. | Interrupted gaze resets; continuous gaze triggers once at/after the authored duration. | AR recording plus trigger evidence |
| `ar-touch` | Touch outside the object, then touch the authored representation. | Miss does nothing; hit submits one touch trigger. | AR recording plus API response |
| `map-ar-vr-ir-representation` | Open every mode authored by the fixture. | Correct 2D/3D representation, location, scale, and visibility appear in each enabled mode. | One capture per mode |
| `audio-video-text-link` | Trigger the ordered content fixture. | Every authored item runs once, in server response order, with usable media/link output. | Recording plus ordered client log |
| `quest-objective-presentation` | Complete a partial objective and then the final objective. | Partial completion shows only objective presentation; final completion shows the unique quest presentation with correct title, collected artwork, and audio. | Recording plus quest responses |
| `two-avatar-deployed-replicas` | Release two avatars simultaneously through different WEB5 replicas against one remaining allocation. | Exactly one request succeeds and one durable grant/progress transition exists. | Both responses plus provider record |
| `deployed-provider-interruption` | Pause/terminate the selected disposable provider during a trigger, restore it, then replay the same idempotency key twice. | First replay resumes and commits once; second replay returns the same result without another grant. HyperDrive behavior follows the configured provider policy. | Process timeline, responses, journal and provider records |

## Validate and publish

Set every completed case to `PASS`, add factual notes, and attach at least one artifact path or URL. Then run:

```powershell
./Scripts/run_geohotspot_full_matrix.ps1 -PortableProviders `
  -ProviderProfilesPath C:\TestFixtures\providers.json `
  -ConcurrencyFixturePath C:\TestFixtures\replicas.json `
  -AcceptanceEvidencePath C:\TestEvidence\geohotspot-live-acceptance.json
```

Any missing prerequisite is a `SKIP`; any incomplete or malformed acceptance bundle is a `FAIL`. Preserve the generated JSON, Markdown, HTML, TRX, Unity XML, service logs, and referenced artifacts under the release evidence ID.
