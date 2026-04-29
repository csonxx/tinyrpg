# Touch Input System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Foundation — enables player interaction with all presentation layer systems)

## Overview

Touch Input System translates raw mobile touch events (tap, swipe, long-press) into game-meaningful actions and routes them to the correct receiver based on the current scene context. It is the single gateway for all player touch input — no touch event reaches game logic without passing through this system.

The system operates as a context-aware router: the same tap gesture means different things depending on whether the player is in a dialogue scene (advance text), a choice moment (select option), or a menu (navigate items). This avoids hardcoding input logic into individual UI components and centralizes input policy in one place.

**Owned behavior:** Gesture recognition timing, touch routing rules, haptic feedback triggers, accessibility input accommodations. **Not owned:** the visual response to input (that's the UI's job), the game logic that processes a choice (that's the Branching Dialogue System).

## Player Fantasy

**Indirect.** Players do not think about the input system — they think about the dialogue, the choices, the story. The input system should feel like lifting a finger to turn a page in an intimate letter, not operating a device. Responsive feedback (haptic + visual) reassures the player that every tap registered, without drawing attention to the mechanism.

Touch interactions should feel precise and intentional: tapping a dialogue box advances cleanly, tapping a choice selects it cleanly, swiping reveals history smoothly. The player should never wonder "did my tap register?" or feel they missed a response because the wrong gesture fired.

## Detailed Design

### Core Rules

**Rule 1: Gesture Types**
The system recognizes four touch gestures, each with configurable timing thresholds:
- `TAP`: Touch down + touch up within `TAP_MAX_DURATION` (default: 300ms) and within `TAP_MAX_MOVEMENT` (default: 20px). Triggers on touch-up.
- `SWIPE_LEFT`: Touch down, horizontal movement > `SWIPE_THRESHOLD` (default: 50px) in left direction within `SWIPE_MAX_DURATION` (default: 500ms). Triggers on swipe completion.
- `SWIPE_RIGHT`: Same as SWIPE_LEFT but right direction. Triggers on swipe completion.
- `LONG_PRESS`: Touch down held for `LONG_PRESS_DURATION` (default: 600ms) without movement exceeding `LONG_PRESS_MOVEMENT_TOLERANCE` (default: 10px). Triggers once on threshold reached.

**Rule 2: Context-Based Routing**
Every frame, the system queries `SceneManagement.GetCurrentSceneContext()` to determine what input mode is active. The routing table:

| Scene Context | TAP | SWIPE_LEFT | SWIPE_RIGHT | LONG_PRESS |
|---------------|-----|------------|-------------|------------|
| `DIALOGUE_ACTIVE` | Advance dialogue / dismiss narration | Show dialogue history | Show dialogue history | Show character info tooltip |
| `CHOICE_ACTIVE` | Select focused choice | Navigate choices left | Navigate choices right | — |
| `CUTSCENE` | — | — | — | — (cutsceens are not skippable by default per Scene Management OQ1) |
| `MENU_OPEN` | Activate focused menu item | Navigate menu left | Navigate menu right | — |
| `HISTORY_OVERLAY` | Dismiss history panel | — | — | — |

**Rule 3: Haptic Feedback**
Haptic feedback is triggered by the input system (not the receiving UI):
- `TAP` on valid target → `HapticFeedback.LIGHT` (Unity's default impact feedback)
- `TAP` on choice → `HapticFeedback.MEDIUM`
- `LONG_PRESS` recognized → `HapticFeedback.HEAVY`
- `SWIPE` threshold crossed → `HapticFeedback.SELECTION`
Haptics can be disabled in Settings (signal: `Settings.HapticEnabled = false`).

**Rule 4: Touch Blocklist**
During `CUTSCENE` context, all touch input is blocked except system gestures (notification pull-down, OS-level back gesture). The input system holds a `TouchBlockToken` that is requested on cutscene start and released on cutscene end.

**Rule 5: Multi-Touch Policy**
Only the first touch per gesture is processed. Additional simultaneous touches are ignored until the current gesture completes. This prevents accidental two-finger taps from being interpreted as distinct inputs.

### States and Transitions

The Touch Input System has three operational states:

| State | Description | Valid Transitions |
|-------|-------------|-----------------|
| `ENABLED` | Normal operation, all gestures processed and routed | → `DISABLED` (on `MenuOpen`), → `BLOCKED` (on `CutsceneStart`) |
| `DISABLED` | All touch events ignored, but system still tracks Scene context | → `ENABLED` (on `MenuClosed`) |
| `BLOCKED` | Touch events are consumed but not routed — used during cutscenes | → `ENABLED` (on `CutsceneEnd`) |

Transitions are triggered by signals from other systems:
- `MenuOpen` (from NSM state `MENU_OPEN`) → `TouchInput.Disable()`
- `MenuClosed` → `TouchInput.Enable()`
- `CutsceneStart` → `TouchInput.Block()`
- `CutsceneEnd` → `TouchInput.Unblock()`

### Interactions with Other Systems

**← Scene Management:**
- Scene Management drives the `GetCurrentSceneContext()` query that determines routing
- Scene Management emits `MenuOpen`, `MenuClosed`, `CutsceneStart`, `CutsceneEnd` signals that drive Touch Input state transitions

**→ Branching Dialogue System:**
- Routes `TAP` during `CHOICE_ACTIVE` context as `ChoiceSelected(focusedChoiceIndex)`
- Routes `SWIPE_LEFT`/`SWIPE_RIGHT` during `CHOICE_ACTIVE` as `NavigateChoices(direction)`
- Routes `TAP` during `DIALOGUE_ACTIVE` as `AdvanceDialogue()`

**→ HUD / UI:**
- Routes `LONG_PRESS` during `DIALOGUE_ACTIVE` as `ShowCharacterInfo(characterId)`
- HapticFeedback signals go to Unity's haptic engine (not to UI)

**→ Settings System:**
- Subscribes to `Settings.HapticEnabled` changes to enable/disable haptic output

## Formulas

**Formula 1: Tap Detection**
A tap is registered when:
```
isTap = (touchDuration <= TAP_MAX_DURATION) AND (euclideanDistance(touchDownPos, touchUpPos) <= TAP_MAX_MOVEMENT)
```

**Formula 2: Swipe Direction Detection**
```
swipeDirection = (touchUpPos.x - touchDownPos.x) > 0 ? RIGHT : LEFT
isSwipe = (touchDuration <= SWIPE_MAX_DURATION) AND (abs(swipeDeltaX) >= SWIPE_THRESHOLD) AND (abs(swipeDeltaY) < SWIPE_THRESHOLD)
```
(Diagonal movement exceeding vertical threshold cancels the swipe — only near-horizontal swipes register.)

**Formula 3: Long Press Recognition**
```
isLongPress = (touchHeldDuration >= LONG_PRESS_DURATION) AND (movementSinceDown <= LONG_PRESS_MOVEMENT_TOLERANCE)
```
Triggers once at the threshold, not continuously.

## Edge Cases

- **If touch starts on a UI element but ends outside it**: The gesture routing uses the starting touch position, not the release position, for routing decisions. This prevents accidental navigation when dragging a finger off a button.
- **If a swipe and tap fire simultaneously (borderline gesture)**: Swipe takes priority if movement exceeds `TAP_MAX_MOVEMENT` during the touch. A gesture that moves 21px in 200ms is classified as a swipe, not a tap.
- **If touch occurs during scene transition**: All touches queued during `SceneManagement.IsTransitioning()` are ignored. The transition animation completes before input resumes.
- **If player rapidly taps during text animation**: During `DIALOGUE_ACTIVE`, taps during text animation (text is still printing) are queued as advance-to-end on first tap, dismiss on second tap (skips to end of current text block). After text is fully displayed, taps advance normally.
- **If system notification appears mid-game**: OS-level touches (notification dismissal, incoming call) are not consumed by the game input system. The game remains in its last state.
- **If device is rotated during touch**: Rotation invalidates all in-progress touches. Any touch active during rotation is cancelled without triggering a gesture.
- **If device has no haptic engine**: The haptic calls are wrapped in a try/catch that silently no-ops on platforms without haptic support. No crash, no error logged.

## Dependencies

- Upstream: **Scene Management** (provides current scene context via `GetCurrentSceneContext()`, emits state-change signals)
- Upstream: **Settings System** (provides `HapticEnabled` toggle)
- Downstream: **Branching Dialogue System** (receives `AdvanceDialogue()`, `ChoiceSelected()`, `NavigateChoices()`)
- Downstream: **HUD / UI** (receives `ShowCharacterInfo()`, `ShowDialogueHistory()`)
- Engine: Unity Touch input APIs (`Input.GetTouch()`, `TouchPhase`)

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `TAP_MAX_DURATION` | 300ms | 100–500ms | Longer = more forgiving for slow lifts |
| `TAP_MAX_MOVEMENT` | 20px | 10–50px | Larger = tolerates more finger wobble |
| `SWIPE_THRESHOLD` | 50px | 30–100px | Larger = requires more decisive swipe |
| `SWIPE_MAX_DURATION` | 500ms | 300–1000ms | Longer = more forgiving for slow swipes |
| `LONG_PRESS_DURATION` | 600ms | 400–1200ms | Longer = requires more deliberate hold |
| `LONG_PRESS_MOVEMENT_TOLERANCE` | 10px | 5–30px | Larger = tolerates more finger drift during hold |
| `TEXT_ADVANCE_TAP_THRESHOLD` | 2 | 2–4 | Number of rapid taps to skip text vs. single tap to advance |

## Acceptance Criteria

**AC1: Tap Advances Dialogue**
- **GIVEN** player is in `DIALOGUE_ACTIVE` context with text fully displayed
- **WHEN** player taps anywhere on the screen
- **THEN** `AdvanceDialogue()` is sent to Branching Dialogue System

**AC2: Tap Selects Choice**
- **GIVEN** player is in `CHOICE_ACTIVE` context with choice index 2 focused
- **WHEN** player taps the screen
- **THEN** `ChoiceSelected(2)` is sent to Branching Dialogue System

**AC3: Swipe Opens History**
- **GIVEN** player is in `DIALOGUE_ACTIVE` context
- **WHEN** player swipes left or right
- **THEN** `ShowDialogueHistory()` is sent to HUD

**AC4: Long Press Shows Character Info**
- **GIVEN** player is in `DIALOGUE_ACTIVE` context with character "ZHANG" speaking
- **WHEN** player long-presses (600ms+) without moving
- **THEN** `ShowCharacterInfo(ZHANG)` is sent to HUD

**AC5: Cutscene Blocks Input**
- **GIVEN** player is in `CUTSCENE` context
- **WHEN** player taps the screen
- **THEN** no gesture is routed; tap is consumed silently

**AC6: Haptic Fires on Tap**
- **GIVEN** player has haptics enabled in settings
- **WHEN** player taps a valid target
- **THEN** `HapticFeedback.LIGHT` fires on the device

**AC7: Disabled State Blocks All Input**
- **GIVEN** Touch Input is in `DISABLED` state (menu open)
- **WHEN** player taps the screen
- **THEN** no gesture is routed

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should swipe-up gesture be used for anything? (Could open a quick menu or journal) | UX Designer |
| OQ2 | Do we support double-tap to zoom on character portraits? | UX Designer |
| OQ3 | Should we support two-finger tap for a "mark as important" feature on dialogue? | Game Designer |
