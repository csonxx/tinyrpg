# Save/Load System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29

## Overview

Save/Load serializes and persists the full game state to JSON files on the device's local storage. It provides both manual saves (player-initiated) and autosaves (triggered at checkpoint events).

**Owned data:** Save file structure (JSON schema). **Not owned:** the serialized NSM data itself — that is owned by NSM.

## Core Rules

**Rule 1: Save Format**
```json
{
  "version": "1.0",
  "timestamp": "ISO8601",
  "chapterIndex": 2,
  "sceneId": "ch2_scene4",
  "nsmState": { /* full NSM JSON */ },
  "nsmHash": "SHA256_hex",
  "playTimeSeconds": 3847,
  "choiceCount": 47
}
```
Save file is stored as JSON in `Application.persistentDataPath/saves/save_{slot}.json`.

**Rule 2: Autosave Triggers**
- After each chapter completes (`CHAPTER_COMPLETE` state)
- After each scene's first dialogue node (`SCENE_ACTIVE` after dialogue node 0)
- After `TrustBoundaryReached` events (danger zone entry)

**Rule 3: Manual Save Slots**
- 3 manual save slots (slot 0, 1, 2)
- Slot display shows: chapter name, scene name, timestamp, play time
- Overwriting a slot requires confirmation dialog

**Rule 4: Hash Integrity**
On save: compute `SHA256(nsmState JSON)` and store as `nsmHash`. On load: recompute hash, reject if mismatch.

## Acceptance Criteria

**AC1: Manual Save/Load Round-Trip**
- **GIVEN** player is mid-chapter with `trust.imperial=60, chapter.current=2`
- **WHEN** player saves to slot 1, then loads slot 1
- **THEN** state is restored to exactly `trust.imperial=60, chapter.current=2`

**AC2: Corrupt Save Rejected**
- **GIVEN** a save file with tampered JSON (hash mismatch)
- **WHEN** player attempts to load
- **THEN** error dialog shown, player returned to menu, autosave offered

**AC3: Autosave at Chapter End**
- **GIVEN** player completes chapter 1
- **WHEN** `CHAPTER_COMPLETE` state is entered
- **THEN** autosave slot is updated with current state

## Dependencies
- Upstream: **Narrative State Machine** (provides serializable state)
- Downstream: **Menu System** (displays save/load UI)

## Open Questions
| OQ1 | Cloud save sync for mobile? | Producer |
