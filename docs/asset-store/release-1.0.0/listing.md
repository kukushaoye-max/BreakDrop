# BreakDrop 1.0.0 — current candy template submission

Prepared 2026-10-22. This listing replaces the earlier prototype copy only when submitting the matching current candy release. Review the final validation report and actual package before submission.

## Package title

BreakDrop — Candy Physics Puzzle Template

## Overview

A portrait Unity 2D puzzle template with tap-to-remove supports, 20 JSON levels, candy-themed UGUI, undo, hints, local progression and level-editing tools.

## Description

BreakDrop is a Unity 2D physics puzzle template. Tap supports to guide a hexagonal target safely onto a platform. Blocks fall and rotate through physics, with further input locked until the structure settles.

Includes 20 playable JSON levels, rectangular and L-shaped blocks, full C# source, a level workshop, validated reference routes, undo, hints, restart, countdown, star progress and win/fail screens. The candy-themed UGUI includes level selection, settings, local stamina, consumable items and daily supplies. Music and sound effects are generated procedurally.

**Requirements:** Unity 2022.3.13f1c1, Built-in Render Pipeline, Unity UI (`com.unity.ugui` 1.0.0) and 2D Sprite (`com.unity.2d.sprite` 1.0.0, for the included slicing editor tool). Install these registry packages through Package Manager. Keep built-in Physics 2D, UI, IMGUI, audio, image conversion and JSON modules enabled. Set Active Input Handling to Input Manager (Old) or Both. No separate Asset Store purchase or third-party gameplay SDK is required.

Keep supplied assets under `Assets/BreakDrop/Resources`. Outfit and Noto Sans SC fonts use SIL Open Font License 1.1; see the included Third-Party Notices and font licenses.

**Limitations:** The supplied game UI and some editor labels are Chinese; some labels and controls are baked into artwork. English documentation explains customization. Ads are unavailable placeholders; no ad provider, cloud saves or server reward validation is included. WeChat export tools are excluded. Local gameplay requires no account, API key, subscription or CDN.

Android and iOS physical-device acceptance was skipped. Device compatibility is not certified; other Unity versions, URP, HDRP and WebGL deployment are unverified. Device builds require platform build tools; iOS also requires macOS/Xcode. Signing and store publication are separate steps.

## AI Description

This package contains content generated in whole or in part with artificial intelligence (AI). OpenAI Codex assisted with portions of the C# gameplay code, Unity Editor tools and included documentation. OpenAI ChatGPT image generation was used for portions of the included 2D artwork and UI graphics. Subsequent work included code integration and debugging, sprite import configuration, UGUI integration, level authoring, and physics and gameplay verification. This disclosure covers functional package content, not only marketing images. Included third-party fonts retain their original licenses and attribution.

## Technical details

- Unity baseline: 2022.3.13f1c1; Built-in Render Pipeline.
- Registry dependencies: Unity UI 1.0.0 (runtime UI), 2D Sprite 1.0.0 (editor slicing tools).
- Input: legacy Input Manager, mouse click / single touch; portrait layout designed around 390 x 844.
- Demo: Assets/BreakDrop/Scenes/Prototype.unity.
- UGUI prefab: Assets/BreakDrop/Resources/UI/CandyCanvas.prefab.
- Runtime source: Assets/BreakDrop/Scripts/Runtime; editor tools: Assets/BreakDrop/Editor.
- Production catalog: 20 tower levels; Resources/Levels also retains 3 non-playable legacy fixtures.
- Five editor support samples; workshop includes fixed-support and hinge examples.
- 11 block artwork variants; rectangle, L2 and L3 collider shapes.
- Reference routes are validated solutions, not guaranteed global optima or cross-platform deterministic results.
- Local PlayerPrefs saves, stamina and consumable inventory; editable C# implementation.
- Procedural music and effects included; no external audio library required.
- Game UI and some editor labels: Chinese. Documentation: English.
- Third-party fonts: Outfit and Noto Sans SC, SIL OFL 1.1; full licenses supplied.
- Android/iOS device acceptance: intentionally skipped, not passed.
- No WeChat SDK, real ad integration, cloud backend, API keys, APK/IPA or executable build tools included.

## Submission steps (publisher only)

1. Use the package and checksum identified in the matching release report. Do not upload the original project's entire Assets folder or release engineering helpers.
2. Replace the previous description, technical details and AI Description with the sections above. Review and edit the listing for accuracy before submission.
3. Use candy-version screenshots, not early prototype screenshots. Chinese text must remain visible where it is part of the product.
4. Preserve registry dependencies in the submission project's Packages/manifest.json; the accompanying project includes this file. Do not classify Unity UI or 2D Sprite as purchased Asset Store dependencies.
5. Confirm upload, draft fields, publisher support contact and active website in Publisher Portal. The website task remains paused; release preparation does not change the publisher profile or submit the package.
6. This is an Asset Store template release, not an Android, iOS or WeChat consumer-game launch approval.
