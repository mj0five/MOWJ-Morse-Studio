# Release preparation — 0.4.1

Prepared on 2026-11-01 in a separate workspace copy; the original E:\MOWJ-GitHub is unchanged. No GitHub upload or website deployment has been performed.

## Publication decisions

- Version: 0.4.1, intended as the initial normal public release at the owner's request; do not mark it as a GitHub prerelease solely because follow-up validation exists.
- Source, documentation and included project artwork: MIT, Jey (mj0five), 2026. Third-party notices remain separate.
- README banner: signal-lab A, descriptive subtitle, green J with violet dot.
- Website/social artwork: coastal B, serif green J with violet dot. Artwork is prepared; no website has been deployed.
- Product identity: MOWJ — Morse Studio. Proposed repository slug: MOWJ-Morse-Studio.

## Public release text

MOWJ 0.4.1 is an offline Morse learning studio with an interactive signal tree, exercises, straight-key rhythm analysis and 14 coastal story contacts. Available as standalone HTML and an unsigned Windows x64 installer. Source and project artwork are MIT licensed. Practice progress stays local; desktop and browser histories are separate.

Source tests, standalone build, installer/uninstaller layout checks and a basic desktop launch smoke test passed. Persian mapping remains experimental. Clean-machine install/uninstall, full mission completion, audible quality and PDF acceptance remain follow-up validation; see KNOWN_ISSUES.md and VALIDATION.md. This release is not an emergency communications tool.

## Upload plan

Upload the source archive contents as repository files. Upload the Windows installer, standalone HTML, license/third-party notices and SHA256SUMS.txt as release downloads. Keep runtimes, .build/.cache, QA profiles, extracted payloads and personal histories out of the source repository.
