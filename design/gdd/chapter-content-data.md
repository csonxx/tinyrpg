# Chapter Content Data

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Foundation — all narrative content lives here; serves all pillars by providing the story substance)

## Overview

Chapter Content Data is the data architecture that holds all narrative content for 雾中誓言: episode definitions, chapter definitions, scene definitions, dialogue trees, character profiles, and location metadata. It is not a software system — it is the content database that other systems query.

The data is stored as ScriptableObject assets in Unity (one per episode), serialized as JSON for editor tooling and potential future export to a narrative authoring tool. Each asset contains the full content tree for one episode.

This document defines the schema — the structure of the data — not the content itself (that is authored separately).

## Player Fantasy

**Indirect.** Players never interact with Chapter Content Data directly. They experience it as the richness of the story: varied scenes, believable character voices, meaningful choices, and narrative paths that feel like they lead somewhere. Poor content data produces a hollow game regardless of how good the systems are.

## Detailed Design

### Core Rules

**Rule 1: Episode Asset**
One Unity `ScriptableObject` asset per episode (e.g., `Episode1.asset`). Contains:
- `episodeId`: string identifier (e.g., "episode_1")
- `episodeTitle`: display name (localized string key)
- `chapters`: ordered list of `ChapterDefinition` references

**Rule 2: Chapter Definition**
Each chapter contains:
- `chapterId`: string (e.g., "ch1")
- `chapterTitle`: display name key
- `sceneList`: ordered list of `SceneDefinition` references
- `isLastChapter`: bool

**Rule 3: Scene Definition**
Each scene contains:
- `sceneId`: string (e.g., "ch1_scene1")
- `sceneType`: `WORLD_SCENE` | `DIALOGUE_SCENE` | `CUTSCENE_SCENE`
- `backgroundAsset`: Addressables path for background texture
- `sceneMusic`: Addressables path for BGM (or null)
- `startNodeId`: string — the nodeId of the dialogue tree to begin with
- `isMemoirOrFlashback`: bool — if true, Scene Management uses FADE_BLACK
- `characterPresent`: list of `characterId` strings active in this scene
- `locationDescription`: lore text shown when examining location

**Rule 4: Dialogue Tree**
Each scene has one root dialogue tree (referenced by `startNodeId`). The tree nodes are:
```
DialogueNode {
  nodeId: string
  type: TEXT | CHOICE | CONDITION | END
  speakerId: characterId | null  (null = narration)
  content: string  (localization key)
  choices: Choice[]  (only for CHOICE)
  conditionExpr: string  (only for CONDITION; NSM key expression)
  trueNextNodeId: string  (CONDITION only)
  falseNextNodeId: string  (CONDITION only)
  nextNodeId: string  (TEXT and END)
  trustShift: { imperial: int, underground: int }  (CHOICE only)
}
```

**Rule 5: Character Profile**
Each character has a profile asset:
```
CharacterProfile {
  characterId: string
  displayNameKey: string
  portraitAsset: Addressables path (default portrait)
  portraitEmotions: { emotionName: texturePath }  (optional alternate portraits)
  role: PROTAGONIST | ALLY | ENEMY | NEUTRAL | UNDERGROUND | HANDLER
  bioKey: string  (lore description)
}
```

**Rule 6: Localization**
All human-readable text is stored as localization keys (not raw strings). Keys follow the pattern:
- Dialogue: `dialogue_{sceneId}_{nodeId}` (e.g., `dialogue_ch1_scene1_node5`)
- UI: `ui_{screenName}_{elementName}` (e.g., `ui_menu_save`)
- Character: `char_{characterId}_name`, `char_{characterId}_bio`

## Formulas

Chapter Content Data is a data store — it has no formulas. The content itself (dialogue, choices, trust shifts) is authored by writers, not calculated.

## Edge Cases

- **If a scene's `startNodeId` doesn't exist in the dialogue tree**: Treat as `END` node (graceful failure). Log error for data correction.
- **If a CHOICE node has no choices array or empty array**: Treat as `END` node. Log error.
- **If a CONDITION node has a conditionExpr that can't be parsed**: Treat as `false` path. Log error.
- **If a referenced Addressables asset is missing**: Scene Management logs error; use placeholder texture or skip scene.
- **If characterId in `characterPresent` has no CharacterProfile**: Log warning; skip portrait rendering for that character.

## Dependencies

- Upstream: **None** (this is a data layer — no systems feed into it)
- Downstream: **All narrative systems** query this data:
  - Episode Structure reads episode/chapter/scene definitions
  - Scene Management reads backgroundAsset and sceneType
  - Branching Dialogue System reads dialogue trees
  - Dialogue UI reads speakerId and portrait assets
  - Relationship Memory reads character profiles

## Tuning Knobs

Chapter Content Data is content, not code — it has no tuning knobs. The trust shift values in choices are authored by game designers and reviewed during content balancing.

## Acceptance Criteria

**AC1: Episode Asset Contains Complete Hierarchy**
- **GIVEN** a valid `Episode1.asset`
- **WHEN** it is queried for `episodeId`, `chapters[]`, and each chapter's `sceneList[]`
- **THEN** all required fields are populated with valid references; no null references in the chain

**AC2: Dialogue Tree Has Valid Node References**
- **GIVEN** a dialogue tree for `sceneId`
- **WHEN** Branching Dialogue System traverses nodes via `nextNodeId`, `trueNextNodeId`, `falseNextNodeId`
- **THEN** all referenced nodeIds exist in the tree; no dangling references

**AC3: All Text Uses Localization Keys**
- **GIVEN** any content string field (dialogue text, character name, UI label)
- **WHEN** the field is inspected
- **THEN** it contains a localization key (format `category_key`) not a raw string

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Do we support branching episodes (where completing episode 1 branches to episode 2A or 2B based on trust)? | Game Designer |
| OQ2 | What is the maximum reasonable dialogue tree depth for a single scene before performance becomes a concern? | Engine Programmer |
