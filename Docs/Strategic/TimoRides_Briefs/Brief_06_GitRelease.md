# Brief 06 – Git Workflow & Release Ops

## Objective
Enable seamless collaboration inside the OASIS monorepo while publishing updates to `timo-org/ride-scheduler-be` and `timo-org/Timo-Android-App`, plus define a Monday-ready release checklist.

## Current State
- Work happens in `/Volumes/Storage/OASIS_CLEAN` monorepo; `push-to-timo.sh` handles subtree splits but SSH passphrase blocks pushes.
- No CI enforcing tests/builds before publish.
- Release steps (seeding DB, configuring env vars) live in memory, not documented.

## Deliverables
1. **Credential workflow**
   - Document how to manage the user’s SSH key passphrase or propose alternative (SSH agent caching, new deploy key, HTTPS + PAT).
   - Update `push-to-timo.sh` to accept repo choice + branch and to stop on errors.
2. **CI pipeline**
   - Add GitHub Actions workflows inside subtrees (or monorepo but scoped) to run:
     - Backend: `npm ci`, `npm test`, `npm run lint`.
     - Android: `./gradlew lintDebug testDebugUnitTest assembleDebug`.
     - Driver bot: `dotnet build`.
   - Ensure workflows trigger on push to `main` and PRs.
3. **Release checklist**
   - Create `Docs/Operational/TimoRides_Release_Checklist.md` covering:
     - Pre-flight (tests, lint, seeds).
     - Environment prep (Mongo connection, `.env`, Twilio stub settings, Telegram bot token).
     - Deployment steps for backend (Heroku/Render? specify), Android APK generation, Telegram bot restart.
     - Verification steps (smoke test, booking via Telegram, Android booking).
4. **Versioning & tagging**
   - Define semantic version scheme (e.g., `mvp-<date>`).
   - Add scripts to create tags when publishing to `timo-org` repos.
5. **Knowledge sharing**
   - Summarize workflow in `Docs/Strategic/TimoRides_GitOps.md` with diagrams showing monorepo ↔ subtree flow.
   - Include troubleshooting section for common git subtree issues (history divergence, forced pushes).

## Acceptance Criteria
- Engineer can run one documented command to push backend or Android subtree to `timo-org` once credentials are available.
- CI pipelines execute automatically on GitHub and show green builds on successful commits.
- Release checklist walked through once and validated (include notes/checkboxes).

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Script: `TimoRides/push-to-timo.sh`
- GitHub repos: `git@github.com:timo-org/ride-scheduler-be.git`, `git@github.com:timo-org/Timo-Android-App.git`.***

