# Episode Structure

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Narrative — orchestrates chapter/scene flow, serves all pillars by creating the episodic pacing structure)

## Overview

Episode Structure defines the higher-level narrative pacing: chapters, scenes within chapters, and the sequence in which scenes must be played. It reads narrative content from Chapter Content Data, instructs Scene Management to load scenes, monitors the Branching Dialogue System for scene completion, and drives the player through the episode's intended path.

It is not a linear list — it manages optional scenes (分支场景), chapter branching (chapters can diverge based on NSM state), and the overall episode conclusion. It is the conductor that ensures every scene loads at the right moment in the right order.

**Owned data:** Current episode/chapter/scene index, scene sequence for current chapter. **Not owned:** the content of individual scenes (Chapter Content Data), the transitions themselves (Scene Management).

## Player Fantasy

**Indirect.** Players experience the episode structure as narrative momentum — the sense that the story is building toward something, that each scene earns the next, and that the chapter conclusions feel like meaningful arrivals. They should never think about "scene loading" — they think about "what happens next".

The emotional target is pacing that creates both anticipation and satisfaction: the player should want to continue to the next scene (curiosity at the cliffhanger) and feel rewarded when a chapter concludes (narrative payoff).

## Detailed Design

### Core Rules

**Rule 1: Episode and Chapter Hierarchy**
```
Episode = 1 game session (30-60 min)
Chapter = major narrative act within an episode (3-6 scenes)
Scene = single dialogue location (1 node graph, 1 background)
```
Episode Content Data defines: ordered list of chapters. Each chapter defines: ordered list of scenes. Scene transitions follow this order unless branching paths redirect.

**Rule 2: Scene Sequence**
Within a chapter, scenes play in the order defined in Chapter Content Data, unless a branching condition redirects to an alternate scene. After each scene completes (`DialogueSceneComplete`), Episode Structure queries the chapter's scene list for the next sceneId and calls `SceneManagement.LoadScene(nextSceneId, transitionType)`.

**Rule 3: Chapter Completion**
When the last scene in a chapter's scene list completes:
1. Emit `CHAPTER_COMPLETE` to NSM
2. Update NSM `chapter.current` to next chapter index
3. Trigger autosave (per Save/Load System Rule 2)
4. Display chapter completion UI (if chapter is not the last chapter)

**Rule 4: Episode Completion**
When the last scene of the last chapter completes:
1. Emit `EPISODE_COMPLETE` to NSM
2. Update NSM `episode.complete` flag
3. Display episode credits or "To Be Continued" based on content plan
4. Return to main menu

**Rule 5: Branching Path Selection**

### States and Transitions

| State | Description | Valid Transitions |
|-------|-------------|-----------------|
| `EPISODE_IDLE` | No episode loaded, at main menu | → `EPISODE_LOADING` (on `StartEpisode(episodeId)`) |
| `EPISODE_LOADING` | Loading episode data from Chapter Content Data | → `CHAPTER_ACTIVE` (on episode data loaded) |
| `CHAPTER_ACTIVE` | Playing through a chapter's scenes | → `CHAPTER_COMPLETE` (on last scene done), → `SCENE_TRANSITIONING` (on scene change) |
| `SCENE_TRANSITIONING` | Waiting for Scene Management to complete transition | → `CHAPTER_ACTIVE` (on `SceneReady`) |
| `CHAPTER_COMPLETE` | Chapter ended, showing completion UI | → `EPISODE_LOADING` (on next chapter), → `EPISODE_COMPLETE` (on last chapter done) |
| `EPISODE_COMPLETE` | Episode finished, showing credits/end card | → `EPISODE_IDLE` (on return to menu) |

### Interactions with Other Systems

**→ Scene Management:**
- Calls `LoadScene(sceneId, transitionType)` for each scene transition
- Receives `SceneReady(sceneId)` event after each scene loads

**← Branching Dialogue System:**
- Receives `DialogueSceneComplete(sceneId)` after each scene ends
- Uses this signal to trigger next scene load

**→ Narrative State Machine:**
- Reads `chapter.current` from NSM to determine starting chapter on resume
- Writes `chapter.current` on chapter completion
- Writes `episode.complete` on episode completion

**→ Save/Load System:**
- Triggers autosave via `SaveSystem.RequestAutosave("CHAPTER_COMPLETE")` on chapter completion

**→ Dialogue UI:**
- Receives `ShowChapterComplete()` and `ShowEpisodeComplete()` events for end-of-chapter/episode cards

## Formulas

The Episode Structure has no complex formulas — it manages sequencing and state transitions. The key calculation is the next-scene lookup:

**Formula 1: Next Scene Resolution**
```
nextSceneId = chapter.sceneList[chapter.sceneList.currentIndex + 1]
if (nextSceneId == null):
    if (chapter.isLastChapter): emit EPISODE_COMPLETE
    else: emit CHAPTER_COMPLETE
```

**Formula 2: Transition Type Selection**
```
transitionType = chapter.forceTransitionOverride
    ?? (scene.isMemoirOrFlashback ? FADE_BLACK : FADE_GREY)
```
Default transition is FADE_GREY. Memoir/flashback scenes use FADE_BLACK per Scene Management Rule 3.

## Edge Cases

- **If chapter has only one scene**: The scene IS the chapter. `DialogueSceneComplete` immediately triggers `CHAPTER_COMPLETE`.
- **If branch path has no next scene (dead end)**: Treat as `EPISODE_COMPLETE` to prevent infinite loop. Log warning.
- **If player loads save from a mid-chapter state**: Episode Structure reads NSM `chapter.current` and `dialogue.cursor` to resume at the correct chapter and scene.
- **If `EPISODE_COMPLETE` fires but content is missing credits**: Display minimal end card ("To Be Continued") and return to menu. Do not crash.
- **If a chapter's scene list is empty**: This is a data error. Skip the chapter, log error, attempt next chapter.

## Dependencies

- Upstream: **Chapter Content Data** (provides episode/chapter/scene definitions)
- Upstream: **Narrative State Machine** (provides/resumes chapter and dialogue state)
- Upstream: **Branching Dialogue System** (emits `DialogueSceneComplete`)
- Downstream: **Scene Management** (receives `LoadScene` calls)
- Downstream: **Save/Load System** (receives autosave triggers)
- Downstream: **Dialogue UI** (receives chapter/episode completion events)

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `CHAPTER_COMPLETE_UI_DURATION` | 3000ms | 1000–5000ms | How long chapter complete card is shown |
| `AUTO_ADVANCE_TO_NEXT_CHAPTER` | true | bool | If true, next chapter loads automatically after duration; if false, requires player tap |

## Acceptance Criteria

**AC1: Chapter Completes When Last Scene Ends**
- **GIVEN** player completes the last scene of chapter 2
- **WHEN** `DialogueSceneComplete("ch2_scene3")` is received
- **THEN** NSM `chapter.current` increments to 3; autosave is triggered; chapter 3 begins loading

**AC2: Episode Ends After Final Chapter**
- **GIVEN** player completes the last scene of the final chapter
- **WHEN** `DialogueSceneComplete` is received for the last scene
- **THEN** `EPISODE_COMPLETE` state is entered; episode credits display; player is returned to menu

**AC3: Resume From Mid-Episode Save**
- **GIVEN** player saved mid-chapter 2 with `chapter.current=2, dialogue.cursor="ch2_scene2_node5"`
- **WHEN** player loads that save
- **THEN** episode resumes at chapter 2, scene 2, dialogue node 5

**AC4: Flashback Scene Uses FADE_BLACK**
- **GIVEN** next scene has `isMemoirOrFlashback = true`
- **WHEN** Episode Structure calls `LoadScene`
- **THEN** `transitionType = FADE_BLACK` is passed

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should chapters be skippable in a "chapter select" replay mode, or is the path always locked? | Game Designer |
| OQ2 | Do we show a "previously on..." recap before resuming a saved episode? | UX Designer |
