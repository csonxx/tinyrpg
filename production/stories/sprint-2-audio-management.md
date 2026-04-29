# S2-4: Audio Management

> **Type**: Integration
> **Status**: Ready for Dev
> **Sprint**: 2
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Dependencies**: S1-1 (NSM)

## Overview

Implement Audio Management: event-driven BGM/SFX/Voice system. Responds to SceneReady for scene-linked BGM, NSM state changes for pause/resume, and dialogue events for voice lines.

## GDD Reference

- `design/gdd/audio-management.md` — full design spec

## Acceptance Criteria

- [ ] AC1: SceneReady with sceneMusic triggers correct BGM playback
- [ ] AC2: NSM MENU_OPEN pauses BGM; resume on MENU_CLOSE
- [ ] AC3: Volume multipliers from Settings System apply to all audio output
- [ ] AC4: BGM_STOP fades out over 500ms

## Files to Create

- `src/core/audio/AudioManagement.cs` — main MonoBehaviour
- `src/core/audio/AudioEvent.cs` — event types
- `tests/integration/audio/AudioManagementTests.cs` — integration tests

## Notes

- No actual audio assets needed for MVP — mock audio keys return no-op
- Settings System dependency is future Sprint 2/3; use hardcoded volume multipliers for now
