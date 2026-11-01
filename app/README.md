# App implementation notes

The canonical user/build guide is [the root README](../README.md); Persian readers can use [README-fa.md](../README-fa.md).

## Source and generated output

`index.html`, `style.css`, `core.js`, `rhythm.js`, `app.js` and `story-missions.html` are editable source. Artwork is in `assets/story/`. Run `node build.mjs` before opening the source app: it embeds artwork and generates `story.js` plus `../mowj-offline.html`. Do not commit either generated file.

## Implemented in 0.4.1

Onboarding, local profiles/history, signal tree, exercises, telegraph desk, on-air drills, 14 story contacts and rhythm analysis are implemented. Story progress and mission records have their own storage keys. Navigating away pauses story operation. A straight key records actual held duration; two-key mode measures spacing only. Rhythm data is session-only.

## Remaining work

Persian mapping verification, adaptive drills, audio export, microphone decoding and hardware/peer connectivity remain future work. Windows packaging exists; final acceptance is still limited as described in [VALIDATION.md](../docs/VALIDATION.md). Older roadmap text describing story missions or the installer as entirely unimplemented has been superseded.

## References

- International Morse timing: https://www.itu.int/rec/R-REC-M.1677-1-200910-I/
- CWops beginner practice: https://cwops.org/wp-content/uploads/2024/01/Practice-Instructions-and-Homework-Assignments-Beginner-CW-Rev-4.2.7.2.pdf
- ARRL W1AW: https://www.arrl.org/w1aw

These are optional references, not dependencies. They are not evidence that the app has been independently certified. The experimental Persian mapping has not been replaced with an unverified online table.
