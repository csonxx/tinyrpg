# Story: S1-4 — Touch Input System

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: must-have
> **Status**: ready-for-dev
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Type**: Logic
> **ADR**: No ADR applies — standard Unity input handling patterns
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Implement the single gateway for all touch input: gesture recognition (tap, swipe, long-press) with context-based routing to the correct receiver based on current scene state.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#)
- **GDD Ref**: `design/gdd/touch-input-system.md`

## Gesture Thresholds

| Gesture | Duration | Movement |
|---------|----------|----------|
| TAP | ≤ 300ms | ≤ 20px |
| SWIPE_LEFT/RIGHT | ≤ 500ms | ≥ 50px horizontal |
| LONG_PRESS | ≥ 600ms | ≤ 10px drift |

## Context Routing Table

| Scene Context | TAP | SWIPE_LEFT/RIGHT | LONG_PRESS |
|---------------|-----|------------------|------------|
| `DIALOGUE_ACTIVE` | Advance dialogue | Show history | Character info |
| `CHOICE_ACTIVE` | Select focused choice | Navigate choices | — |
| `CUTSCENE` | Blocked | Blocked | Blocked |
| `MENU_OPEN` | Activate menu item | Navigate menu | — |

## Input States

| State | Behavior |
|-------|---------|
| `ENABLED` | Normal operation |
| `DISABLED` | Menu open — all touches ignored |
| `BLOCKED` | Cutscene — touches consumed silently |

## Acceptance Criteria

- [ ] Tap during DIALOGUE_ACTIVE → fires `AdvanceDialogue()`
- [ ] Tap during CHOICE_ACTIVE → fires `ChoiceSelected(focusedIndex)`
- [ ] Swipe during CHOICE_ACTIVE → fires `NavigateChoices(direction)`
- [ ] Tap during CUTSCENE → no effect (blocked)
- [ ] Tap during MENU_OPEN → no effect (disabled)
- [ ] Long-press during DIALOGUE_ACTIVE → fires `ShowCharacterInfo(characterId)`
- [ ] First tap during text animation → cancel animation
- [ ] Second tap within 300ms → advance
- [ ] Haptic feedback fires on tap (when enabled in settings)

## Open Questions

| OQ1 | Should swipe-up gesture be used for anything? | UX Designer |
|-----|---------------------------------------------|-------------|
| OQ2 | Support double-tap to zoom on portraits? | UX Designer |

## Dependencies

- S1-3 (Branching Dialogue) — receives advance/choice signals
- S1-5 (Dialogue UI) — receives show-history and character-info signals

## Test Evidence

Location: `tests/unit/touch-input/` — unit tests covering GestureRecognition, ContextRouting, DoubleTapCancel, HapticFeedback
