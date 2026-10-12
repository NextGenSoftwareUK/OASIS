# Release documentation audit

Audit date: 12 October 2026. This is a source/metadata audit, not a claim that revised documentation has already reached public package registries.

## Completed changes

- Edge Runtime and Edge ONET Runtime now have comprehensive source READMEs covering installation, features, API composition, configuration, security, lifecycle, troubleshooting and platform limitations.
- Both project files declare `PackageReadmeFile` and include README.md in NuGet archives. These changes await hosted packing and publication.
- ODOOM/OQUAKE 1.1.0 live release notes were reviewed against released source and made consistent. Versioned notes and required headings are enforced for future game releases.
- The release inventory records verified public versions separately from pending or partially published work.

## Remaining findings

| Component / surface | Finding | Required follow-up |
|---|---|---|
| Contracts, HyperDrive Synchronization, ONET | No project-local README found in this audit; no PackageReadmeFile declaration found | Write owning-component guides from actual public contracts; include them in NuGet archives |
| EdgeSQLite provider | No project-local README or NuGet README declaration found | Document database/native platform requirements, transactions, durable outbox and recovery |
| HoloOASIS Edge provider | No project-local README or NuGet README declaration found | Document required conductor/hApp integration, lifecycle, supported platform evidence and limitations |
| Edge Native Endpoint | No project-local README or NuGet README declaration found | Document endpoint composition and host authentication/runtime requirements |
| HoloOASIS Unity | NuGet README metadata exists | Review content and archive inclusion against current Unity/mobile integration; metadata alone is not completeness |
| Other first-party NuGet projects | Many project files lack PackageReadmeFile (initial broader scan found 115 of 236 PackageId-bearing projects without it, including non-packable hosts) | Audit only packable candidates for publication, distinguish reusable packages from host applications, verify each declared file and packed root path |
| OASIS / STAR / Native / OGEngine release notes | General generator classifies commit subjects; does not provide consistent installation, configuration or platform prerequisites | Add reviewed product guides and version-specific verified asset/source references; summarize actual user-facing changes, not just commit subjects |
| Runtime release-note files under Docs/Releases | Existing static files still describe older versions (for example OASIS 5.0.1) | Preserve historical notes; add reviewed versioned current notes instead of changing their version headings in place |
| OIDE / ONODE Manager / HyperDrive Client / OurWorld | Not yet fully audited against exact released source and live assets | Review owning repository README, release notes, installation, platform assets, requirements and verification limits |

## Acceptance checklist

For each package or product, verify:

1. Purpose, scope and relationship to other OASIS components.
2. Installation command or exact release download links, supported frameworks and required native dependencies.
3. Real usage examples checked against actual APIs; identify host-provided integrations explicitly.
4. Configuration defaults, authentication, secret handling, persistence and lifecycle.
5. Troubleshooting and meaningful error codes.
6. Windows/Linux/macOS supported architecture assets and mobile/Unity limits; never equate managed compatibility with physical-device validation.
7. Version-specific features/fixes, immutable source tag and full changelog.
8. NuGet README declaration and actual archive inclusion, plus live registry rendering after publication.

Existing NuGet packages are immutable. Documentation changes belong in an unpublished version and must pass normal checked promotion/publication. Do not overwrite an existing package, invent a test result or treat this audit as completion of the remaining publication work.

See [release inventory](OASIS_RELEASE_STATUS.md) for actual publication status.
