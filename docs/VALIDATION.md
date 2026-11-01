# Validation — 2026-11-01

## Checks performed in this session

- `node app/tests.cjs`: 14 named assertions passed (international round trips, SOS, audio timeline, keyboard handling, profile validation, boundary handling, scoring guards and scoped history/reset behaviour).
- `node app/rhythm-tests.cjs`: passed duration/spacing, generated-tone exclusion, overlap, feedback and empty-recording checks.
- `node app/story-tests.cjs`: passed production-story structure, no debug skip, profile/audio bridge and navigation pause checks.
- `node app/build.mjs`: generated the standalone HTML and story bundle successfully.
- `scripts/test-layout.ps1`: installer and uninstaller assertions passed at requested 100%, 125%, 150%, 200% scales. On this display the 200% form was clamped to the available screen. These are layout tests, not installation tests.
- Supplied `E:\MOWJ-Setup-0.4.1-win-x64\Setup.exe` SHA-256 matched its supplied checksum: `0c1b6083e85788a38ca95b3483c7338b3cc0b3df1363874136619b195d2cf445`.
- Extracted the embedded payload without running the installer or creating installation shortcuts/registry entries. Launched its MOWJ executable. The QA copy's `main.cjs` was changed only to select an isolated marked profile in this workspace; source and user profile were untouched.
- Visibly reached all three onboarding stages, the overview, story introduction and Contact 01/14 receiving screen with the embedded coastal scene. This resolves the previously incomplete basic-launch smoke check, not full acceptance.

## Scope and remaining work

No installer/uninstaller functional cycle was repeated in this session. No fresh Windows installation or second computer was available. The runtime was extracted from the supplied installer; a fresh Windows package build/download was not performed. The web build and C# layout test compilation did run from the supplied source copy.

Audible quality/latency, complete keyboard interaction, end-to-end completion of all 14 contacts, actual saved PDF output, cold-restart progress recovery, update behaviour and clean-machine install/uninstall remain acceptance work. Synthetic test mocks are not proof of physical timing accuracy. Persian mapping remains experimental.

The unsigned installer is a preview candidate, not a fully validated production release. No GitHub repository or public release was created during this task.

## Final packaging pass — 2026-11-01

After adding MIT licensing and banners, all three JavaScript suites passed again. The standalone HTML includes the project MIT notice. The Windows installer was rebuilt from this source using the official pinned Electron 45.2.1 archive; SHA-256 matched desktop/runtime.json. The package includes LICENSE-MOWJ.txt, Electron LICENSE and LICENSES.chromium.html. No installation was performed during this packaging pass. These packaging checks do not close the acceptance gaps above.

The owner selected a normal 0.4.1 public release rather than a prerelease. Known coverage gaps remain disclosed; no public upload has been performed.
