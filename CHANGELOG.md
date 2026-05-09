# Changelog

All notable project delivery changes are recorded here.

## 1.1.1 - 2026-05-09

- Added explicit setup and secret-handling documentation for OpenAI voice transcription, microphone testing, Wit/Meta settings, and GitHub secrets.
- Updated `VoiceCommandRecognizer` to resolve `OPENAI_API_KEY` from the local environment before requiring Inspector-only configuration.
- Cleared committed Wit/Meta `serverToken` values from `ProjectSettings/wit.config`.
- Extended static governance checks to catch committed OpenAI-like keys, non-empty Wit server tokens, and serialized scene API keys.

## 1.1.0 - 2026-05-09

- Hardened Bob interaction scheduling so runtime targets can interrupt or queue consistently.
- Fixed Map and Mapfull debug transitions so repeated 7/8 operations do not lose Bob state.
- Restored dock-level toggle behavior so panels such as Volume can close on repeated activation.
- Split large orchestration scripts into focused partial classes for motion, scheduling, diagnostics, navigation, onboarding, controller, and map responsibilities.
- Added project governance docs for delivery regression, media asset policy, industrialization audit, runtime architecture, navigation, page authoring, engineering standards, and asset naming.
- Added editor validation and asset governance tools for project structure, import policy, naming, line-count, and scene-binding review.
- Re-encoded `VID_BobCompressed.mp4` to H.264 Constrained Baseline settings for Unity playback compatibility.
- Added GitHub repository governance files, issue templates, pull request template, CODEOWNERS, and static delivery checks.

## 1.0.0 - 2026-05-09

- Initial public prototype baseline for the Bob Shared Mobility in-cabin assistant experience.
