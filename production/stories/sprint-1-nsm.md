# Story: S1-1 — Narrative State Machine

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: must-have
> **Status**: ready-for-dev
> **Estimate**: 5 days
> **Owner**: gameplay-programmer
> **Type**: Logic
> **ADR**: No ADR applies — foundational infrastructure layer
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Build the Narrative State Machine — the single source of truth for all game state. The NSM is a flat key-value store with event-driven mutations, event subscription, undo queue, and JSON serialization.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#)
- **Architecture**: Singleton service with event bus pattern
- **Data**: Flat Dictionary<string, object> for all state
- **Events**: Signal-style event bus for system-to-system communication
- **Serialization**: JSON via Unity's JsonUtility (or Newtonsoft if available)
- **GDD Ref**: `design/gdd/narrative-state-machine.md`

## Requirements

### Core Interface

```csharp
// Mutate: apply a named event with a delta to a key
void Mutate(string eventName, float delta);

// Set: direct value set (for non-numeric state)
void Set(string key, object value);

// Get: retrieve current value
T Get<T>(string key);

// Subscribe: register for state change events
void Subscribe(string keyPattern, Action<NSMEvent> callback);
```

### NSM States

| State | Description |
|-------|-------------|
| `TITLE` | At title screen |
| `CHAPTER_LOADING` | Loading chapter data |
| `SCENE_ACTIVE` | Scene running, no dialogue active |
| `DIALOGUE_ACTIVE` | Dialogue tree active |
| `CUTSCENE` | Cutscene playing |
| `MENU_OPEN` | Pause menu open |
| `CHAPTER_COMPLETE` | Chapter ended, showing completion |
| `ERROR` | Fatal error state |

### Events

- `NSM.StateChanged(oldState, newState)`
- `NSM.KeyChanged(key, oldValue, newValue)`
- `NSM.UndoPerformed(newKey, newValue)`
- `NSM.SchemaValidationFailed(errors[])`

### Undo Queue

- Maximum 20 undo steps (configurable via `MAX_UNDO`)
- Undo restores previous value of the mutated key
- Undo events are also emitted as `UndoPerformed`
- Undo across state transitions (chapter boundary) blocked per Edge Case 6

### Serialization

- `Serialize()` → JSON string of entire KV store
- `Deserialize(json)` → restores exact state
- SHA256 hash of serialized JSON stored alongside for integrity
- Schema validation on deserialize (keys match expected schema)

## Acceptance Criteria

- [ ] `Mutate("trust.imperial", +5f)` increases imperial trust by 5
- [ ] `Mutate("trust.imperial", -10f)` does not go below 0
- [ ] `Mutate("trust.imperial", +200f)` does not exceed 100
- [ ] `Subscribe("trust.*", callback)` fires when any trust key changes
- [ ] `Undo()` restores previous value of last mutated key
- [ ] `Undo()` called 21 times does not crash (queue caps at 20)
- [ ] `Serialize()` produces valid JSON
- [ ] `Deserialize(json)` restores exact state including nested arrays
- [ ] Hash mismatch on deserialize logs error and emits `SchemaValidationFailed`
- [ ] NSM transitions: `TITLE → CHAPTER_LOADING → SCENE_ACTIVE → DIALOGUE_ACTIVE → SCENE_ACTIVE → ... → CHAPTER_COMPLETE → TITLE`
- [ ] Unit tests: Mutate, Undo, Serialize, Deserialize, EventRouting

## Open Questions

None — all resolved in GDD.

## Test Evidence

Location: `tests/unit/nsm/` — unit tests covering Mutate, Undo, Serialize, Deserialize, EventRouting

## Dependencies

- No dependencies — this is the foundation
