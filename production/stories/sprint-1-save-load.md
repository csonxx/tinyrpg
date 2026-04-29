# Story: S1-2 — Save/Load System (MVP)

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: must-have
> **Status**: ready-for-dev
> **Estimate**: 3 days
> **Owner**: gameplay-programmer

---

## Overview

Implement save/load system that serializes full NSM state to JSON files in device storage. Provides 3 manual save slots + 1 autosave slot. Autosave triggers on chapter complete, scene node 0, and trust boundary reached.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#)
- **Storage**: `Application.persistentDataPath/saves/save_{slot}.json`
- **GDD Ref**: `design/gdd/save-load-system.md`

## Requirements

### Save File Format

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

### Slots

- Slot 0, 1, 2: manual saves (player's 3 slots)
- Slot "autosave": automatic saves (overwritten on each trigger)
- Overwriting a manual slot requires confirmation (handled by Menu System)

### Integrity Check

- On save: compute `SHA256(nsmState JSON)` and store as `nsmHash`
- On load: recompute hash, reject if mismatch
- Rejected load: show error dialog, return to menu, offer autosave

### Trigger Points (Autosave)

- `CHAPTER_COMPLETE` state entered
- `SCENE_ACTIVE` after dialogue node 0
- `TrustBoundaryReached` event

## Acceptance Criteria

- [ ] Save to slot 1 produces valid JSON file at `saves/save_1.json`
- [ ] Load from slot 1 restores exact NSM state
- [ ] Play time and choice count recorded correctly
- [ ] Hash mismatch on tampered save shows error dialog
- [ ] Autosave fires on chapter complete
- [ ] Autosave fires on first dialogue node of each scene
- [ ] All 4 slots accessible (3 manual + 1 autosave)
- [ ] Save files survive app restart

## Open Questions

| OQ1 | Cloud save sync for mobile? | Deferred to post-MVP |
|-----|----------------------------|---------------------|

## Dependencies

- S1-1 (NSM) — NSM must exist before save/load can serialize it
