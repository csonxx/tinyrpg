# S2-2: Scene Management

> **Type**: Logic/Integration
> **Status**: Ready for Dev
> **Sprint**: 2
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Dependencies**: S1-1 (NSM), S1-2 (Save/Load)

## Overview

Implement Scene Management: Unity SceneManager wrapper with async loading, three transition animation types (FADE_GREY, FADE_BLACK, CROSSFADE), scene stack for overlay cutscenes, and Addressables-based background preloading.

## GDD Reference

- `design/gdd/scene-management.md` — full design spec

## Acceptance Criteria

- [ ] AC1: LoadScene emits SceneReady after async load completes
- [ ] AC2: FADE_GREY transition (400ms fade out, 100ms hold, 400ms fade in) plays correctly
- [ ] AC3: FADE_BLACK and CROSSFADE transitions work as specified
- [ ] AC4: PushOverlay/PopOverlay works; max stack depth 3 enforced
- [ ] AC5: Preload triggers when choicesRemaining <= 3; eliminates visible loading

## Files to Create

- `src/core/scene/SceneManagement.cs` — main MonoBehaviour
- `src/core/scene/TransitionType.cs` — enum
- `tests/unit/core/SceneManagementTests.cs` — unit tests

## Notes

- Use simple `AsyncOperation` + `LoadAssetAsync`; defer advanced pooling
- Scene backgrounds are not yet available from art team — mock/placeholder for now
