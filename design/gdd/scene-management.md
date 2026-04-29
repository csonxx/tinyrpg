# Scene Management

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Infrastructure — serves all pillars by enabling the narrative structure)

## Overview

Scene Management handles loading, unloading, and transitioning between Unity scenes and sub-scene states for 雾中誓言. Since this is a narrative visual novel game (not an open world), scene transitions are discrete — moving from one chapter/scene to the next, or entering/exiting a dialogue context.

The system wraps Unity's `SceneManager` with a higher-level API that understands the game's narrative structure (chapters, scenes, dialogue contexts, cutscenes) rather than raw scene names.

## Player Fantasy

**Indirect.** Players do not interact with scene management directly. They experience smooth transitions that maintain immersion — no visible loading screens, no jarring cuts between narrative moments.

## Detailed Design

### Core Rules

**Rule 1: Scene Types**
The game has three scene types, each mapping to a Unity scene:
- `WorldScene`: Static background art (no gameplay objects). Used for atmospheric establishing shots.
- `DialogueScene`: Background + character portrait layer + UI overlay. The main gameplay screen.
- `CutsceneScene`: Full-motion sequence with no player input. Used for narrative transitions.

Each narrative scene in Chapter Content Data references one of these scene types and a scene asset name.

**Rule 2: Scene Loading**
Scene loading is asynchronous via Unity's `AsyncOperation`. During the async load, a loading overlay is shown (not a progress bar — a thematic fade matching the art style). The overlay duration is minimum 400ms (art style requirement) regardless of actual load time, to prevent jarring flashes on fast devices.

**Rule 3: Transition Animations**
Transitions between scenes use one of three animation types:
- `FADE_GREY`: Scene fades to grey-lavender wash (400ms), holds (100ms), new scene fades in (400ms). Default for chapter/scene transitions.
- `FADE_BLACK`: Full black fade. Used for dream sequences and memory flashbacks.
- `CROSSFADE`: No fade — scenes are visually distinct enough that a hard cut is intentional. Used for rapid scene changes during tension montages.

**Rule 4: Scene Stack**
Scene Management maintains a `sceneStack` (last-in-first-out) for nested scenes. Dialogue scenes can push cutscene overlays. When a pushed scene ends, the previous scene is revealed. Maximum stack depth: 3.

**Rule 5: Asset Streaming**
Background art is loaded via Unity Addressables. The system preloads the next scene's background art while the current scene is still visible (during dialogue choices). This eliminates perceived load time.

## Interactions with Other Systems

**→ Episode Structure:**
- Episode Structure calls `LoadScene(sceneId, transitionType)` when advancing
- Episode Structure calls `PushOverlay(cutsceneId)` for cutscene overlays
- Scene Management emits `SceneReady` when the new scene's first frame has rendered

**→ Narrative State Machine:**
- `SceneReady` → NSM updates current scene state
- NSM `StateFullyLoaded` → Scene Management knows NSM is ready after a save load

**→ HUD (Trust Meters):**
- HUD is a persistent overlay scene that is never unloaded
- Scene Management does not manage HUD — HUD is always present

## Formulas

**Formula 1: Preload Trigger**
`nextSceneId` is preloaded when the current scene's Dialogue System enters the last choice of the scene (typically 2-3 choices from the end):

```
if (currentScene.choicesRemaining <= 3 AND nextScene != null):
    Addressables.LoadAssetAsync<Texture>(nextScene.backgroundAsset)
```

## Acceptance Criteria

**AC1: Scene Load Completes**
- **GIVEN** `LoadScene("ch1_scene3", FADE_GREY)` is called
- **WHEN** the new scene's first frame renders
- **THEN** `SceneReady("ch1_scene3")` event is emitted, and the scene's character portraits and background are visible

**AC2: Transition Animation Plays**
- **GIVEN** player is on `ch1_scene2`, `FADE_GREY` transition is requested
- **WHEN** the transition begins
- **THEN** the scene fades to grey-lavender over 400ms, holds 100ms, then the new scene fades in

**AC3: Push Overlay**
- **GIVEN** player is in `DialogueScene`
- **WHEN** `PushOverlay("cutscene_intro")` is called
- **THEN** the cutscene scene appears on top, dialogue scene remains beneath it
- **WHEN** the cutscene emits `CutsceneComplete`
- **THEN** the overlay is popped, dialogue scene is revealed

**AC4: Preload Reduces Load Feel**
- **GIVEN** `ch1_scene3` background was preloaded during `ch1_scene2`
- **WHEN** the player reaches the end of `ch1_scene2`
- **THEN** the transition to `ch1_scene3` shows no loading indicator

## Dependencies

- Upstream: **Episode Structure** (calls LoadScene/PushOverlay)
- Downstream: **HUD** (persistent overlay, not managed by Scene Management)
- Engine: Unity SceneManager, Addressables

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should scene transitions be skippable by the player (tap to skip), or always play through? | Game Designer |

## Tuning Knobs

| Knob | Default | Range |
|------|---------|-------|
| `MIN_OVERLAY_DURATION` | 400ms | 200–800ms |
| `PRELOAD_LOOKAHEAD_CHOICES` | 3 | 1–5 |
| `MAX_SCENE_STACK_DEPTH` | 3 | 2–5 |
