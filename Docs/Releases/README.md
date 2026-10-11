# Release-note review contract

ODOOM and OQUAKE use versioned reviewed notes in Component/version.md. The shared
generator rejects missing notes or missing common sections; never replace these
with raw commit subjects or a last-minute one-paragraph summary.

For each release, review the exact tag/candidate source, the previous published
baseline, commits, relevant code and packaged assets. Use the same sections:
Features; Fixes and hardening; Build and packaging; Downloads and requirements;
Source and full changelog. Link immutable released source and the full history
or prior-tag comparison. Deduplicate intermediate fixes and exclude roadmap
claims. Describe validation accurately; builds are not physical gameplay tests.

Every binary product release must include verified Windows, Linux and macOS
assets for its supported architectures. Record platform/architecture explicitly;
NuGet, npm and Unity source packages are platform-neutral distributions, not
proof of native platform execution. Publication must wait for the complete
required asset set, and must never label an old single-platform asset as portable.
