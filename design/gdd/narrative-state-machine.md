# Narrative State Machine

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Identity Is a Cage / No Choice Is Perfect / Trust Is the Most Fragile Currency

## Overview

The Narrative State Machine (NSM) is the central data repository for all game state in 雾中誓言. It is a flat key-value store with structured sub-objects — not a deeply nested object tree. Every narrative system in the game reads from and writes to the NSM; no other system stores narrative state independently.

**What the NSM tracks:**
- Current chapter index and scene ID within chapter
- Dual trust meters: Imperial Loyalty (0–100) and Underground Trust (0–100)
- Character relationship values: one float per named character (–100 to +100)
- Clue/intel flags: boolean per clue ID
- Dialogue history: which choice IDs were selected, which branches were visited
- Progress flags: which narrative events have triggered, which scenes are completed
- Game flags: miscellaneous boolean state (e.g., "has_met_contact_A", "knows_about_betrayal")

**Key design decisions:**
- Single source of truth: NSM is the only system that owns narrative state
- Event-driven mutations: all state changes go through NSM, which emits events to subscribing systems (Trust Economy listens for trust changes, Relationship Memory listens for choice events)
- Snapshot serialization: save/load serializes the entire NSM state to JSON — no delta compression at this layer
- No conditional logic in state: the NSM stores facts, not rules. Rules live in the systems that consume the facts.

## Player Fantasy

**Indirect.** Players do not interact with the NSM directly. They experience its effects:
- **Seamless continuity** — characters remember past interactions; choices made in Chapter 1 affect Chapter 5. The player feels the weight of their decisions because the game never forgets.
- **Trust consequences** — when a trust meter shifts, characters react appropriately. When the underground suspects the protagonist, dialogue options reflect that suspicion. The player feels the fragility of trust because the NSM makes every shift consequential.
- **Discovery rewarded** — clues collected in early scenes unlock new dialogue options later. The player feels clever when the NSM retrieves a flag they earned hours ago.

**Pillar connections:**
- Pillar 1 (Identity Is a Cage): The NSM tracks the double identity — trust in both factions simultaneously. When both meters are balanced, the player feels the structural impossibility of the protagonist's position.
- Pillar 3 (Trust Is Fragile): The NSM makes trust legible. When a contact is lost or a relationship degrades, the player sees it in the relationship values — a number they watched climb and then collapse.

## Detailed Design

### Core Rules

**Rule 1: Single Mutate Interface**
All narrative state changes go through one method: `NSM.Mutate(key, value)`. No other system writes to NSM state directly. `Mutate()` performs:
1. Validates the key exists in the schema
2. Validates the value is within the key's type and range constraints
3. Writes the new value
4. Emits a `StateChanged` event with (key, oldValue, newValue) to all subscribers
5. Queues the mutation in the undo history (last 20 mutations)

```csharp
// Pseudocode
void Mutate(string key, object value) {
    ValidateSchema(key, value);
    var old = _state[key];
    _state[key] = value;
    Emit("StateChanged", key, old, value);
    _undoQueue.Enqueue((key, old));
    if (_undoQueue.Count > 20) _undoQueue.Dequeue();
}
```

**Rule 2: Event Subscription Model**
Other systems subscribe to NSM events by declaring interest in specific keys or key patterns:
- `Subscribe("trust.*", callback)` — receives all trust-related mutations
- `Subscribe("relationship.*", callback)` — receives all relationship mutations
- `Subscribe("*", callback)` — receives all mutations (used by Save/Load for change tracking)

**Rule 3: Snapshot Serialization**
`NSM.Save()` serializes the entire `_state` dictionary to JSON. `NSM.Load(json)` replaces `_state` wholesale and emits a `StateFullyLoaded` event (with no old value).

**Rule 4: Undo System**
`NSM.Undo()` pops the last mutation from `_undoQueue` and calls `Mutate()` with the old value. Undo is available during dialogue (up to last 3 choices) but not during cutscenes or chapter transitions.

**Rule 5: State Schema Validation**
NSM enforces a schema at runtime. Schema is a dictionary of key → (type, min, max, default). Unknown keys are rejected; out-of-range values are clamped. Schema is defined in `ChapterContentData` and loaded at chapter start. NSM rejects mutations that would introduce unknown keys unless the schema is in "open" mode (development only).

### States and Transitions

The NSM does not own the game's active scene state (Scene Management owns that). NSM's own state machine tracks which **narrative phase** the game is in:

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `CHAPTER_LOADING` | Chapter data being loaded, NSM state being restored | → `SCENE_ACTIVE` (success) or `ERROR` (data corruption) |
| `SCENE_ACTIVE` | Player in a scene, no dialogue active | → `DIALOGUE_ACTIVE` (dialogue node triggered) or `MENU_OPEN` (player opens menu) or `CUTSCENE` (cutscene begins) or `CHAPTER_COMPLETE` (scene list exhausted) |
| `DIALOGUE_ACTIVE` | Player in dialogue, making choices | → `SCENE_ACTIVE` (dialogue node complete) or `CUTSCENE` (choice triggers cutscene) |
| `MENU_OPEN` | Pause/settings menu open | → `SCENE_ACTIVE` (resume) or `CHAPTER_LOADING` (load game) |
| `CUTSCENE` | Cinematic playing, player input blocked | → `SCENE_ACTIVE` (cutscene complete) or `DIALOGUE_ACTIVE` (cutscene triggers dialogue) |
| `CHAPTER_COMPLETE` | Chapter ending sequence | → `CHAPTER_LOADING` (next chapter) or `TITLE` (game complete) |
| `ERROR` | Data corruption detected | → `TITLE` (return to title) |
| `TITLE` | Title screen | → `CHAPTER_LOADING` (new game or continue) |

**Note:** The NSM state machine tracks narrative phase only. Scene Management owns actual Unity scene loading/unloading.

### Interactions with Other Systems

**→ Save/Load System:**
- Save/Load calls `NSM.Save()` to get JSON snapshot for writing to disk
- Save/Load calls `NSM.Load(json)` to restore state
- NSM emits `StateChanged` events that Save/Load can subscribe to (for autosave dirty tracking)
- Contract: Save/Load owns the file I/O; NSM owns the data serialization format

**→ Dual Trust Economy:**
- NSM stores `trust.imperial` (float, 0–100) and `trust.underground` (float, 0–100)
- Dual Trust Economy calls `NSM.Mutate("trust.imperial", newValue)` when trust changes
- Dual Trust Economy subscribes to `StateChanged("trust.*")` to trigger UI updates and narrative events
- Contract: NSM stores the raw values; Dual Trust Economy defines the rules for when and by how much trust changes

**→ Branching Dialogue System:**
- NSM stores `dialogue.history` (list of choice IDs in order)
- Branching Dialogue reads `NSM.Get("character.{id}.relationship")` to gate dialogue options
- Branching Dialogue reads `NSM.Get("flags.{flagId}")` to determine which dialogue nodes are accessible
- Branching Dialogue calls `NSM.Mutate("dialogue.history", list.Append(choiceId))` when a choice is made
- Branching Dialogue calls `NSM.Mutate("flags.{flagId}", true)` when a story flag is set
- Contract: NSM stores what happened; Branching Dialogue defines what options are available based on state

**→ Relationship Memory:**
- NSM stores `character.{name}.relationship` (float, –100 to +100) for each named character
- Relationship Memory calls `NSM.Mutate("character.{name}.relationship", delta)` to apply relationship changes
- Relationship Memory subscribes to `StateChanged("character.*")` to track relationship history
- Contract: NSM stores the current value; Relationship Memory defines relationship change formulas

**→ Episode Structure:**
- Episode Structure calls `NSM.Mutate("chapter.current", chapterIndex)` and `NSM.Mutate("scene.current", sceneId)` as chapters/scenes advance
- Episode Structure calls `NSM.Mutate("chapter.flags", flags)` to persist chapter-level completion flags
- Episode Structure subscribes to `StateChanged("chapter.current")` to trigger scene loading
- Contract: Episode Structure owns the sequencing logic; NSM stores the current position and progress

**→ Scene Management:**
- Scene Management subscribes to `StateFullyLoaded` to know when NSM state is ready after a load
- Scene Management and NSM do not share data directly; Scene Management calls Episode Structure, which calls NSM
- Contract: Clean separation — Scene Management handles engine scene objects; NSM handles data

## Formulas

**Formula 1: State Value Clamping**

All numeric values in NSM are clamped to their schema-defined range:

`clamp(value, min, max) = max(min, min(max, value))`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| Trust Imperial | `T_i` | float | [0.0, 100.0] | Imperial faction loyalty |
| Trust Underground | `T_u` | float | [0.0, 100.0] | Underground resistance trust |
| Character Relationship | `R_c` | float | [-100.0, 100.0] | Per-character relationship value |
| Clue Flag | `F_clue` | bool | {true, false} | Whether a clue has been found |
| Choice History Index | `H_len` | int | [0, ∞) | Number of choices made (capped at 200 in memory) |

**Example:** If a dialogue choice applies `T_u = T_u + 15` but T_u was already 90, the result is clamped to `T_u = 100`.

---

**Formula 2: Relationship Value Accumulation**

Relationship values accumulate via delta application, not absolute assignment (except at initialization):

`R_c(new) = clamp(R_c(old) + delta_R_c, -100.0, 100.0)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| delta_R_c | `ΔR` | float | [–50.0, +50.0] per choice | Relationship change from one dialogue choice |

**Output Range:** –100.0 (hostile) to +100.0 (deeply loyal). At –100.0, the character refuses all contact. At +100.0, the character reveals maximum backstory (and cannot increase further).

**Example:** Protagonist betrays contact's location. The choice applies `ΔR = –30`. If the contact was at `R_c = –20`, the new value is `R_c = –50`. If this is the second betrayal, and the contact was already at `R_c = –60`, the result is clamped to `R_c = –100`.

---

**Formula 3: Undo History Limit**

Undo queue is a fixed-size FIFO:

```
if (_undoQueue.Count >= MAX_UNDO) {
    _undoQueue.Dequeue(); // remove oldest
}
_undoQueue.Enqueue((key, oldValue));
```

**MAX_UNDO = 20 mutations.** During dialogue, only the last 3 choices are undoable. During SCENE_ACTIVE, up to 20 mutations can be undone. Chapter transitions clear the undo queue.

---

**Formula 4: Save State Hash (Integrity Check)**

On save, compute a SHA-256 hash of the serialized JSON to detect corruption on load:

`hash = SHA256(JSON.stringify(nsmState))`

Stored alongside the save file. On load, recompute and compare. If mismatch, reject the save and prompt the player with a data recovery option (load from autosave or return to title).

## Edge Cases

**EC1: Corrupt Save Data**
- **If** `SHA256(loadJSON) ≠ storedHash` at load time: reject the save, log error, prompt player to choose: load autosave or return to title. Never silently proceed with corrupt data.

**EC2: Missing Schema Key on Load**
- **If** the loaded JSON contains a key not in the current chapter's schema: log warning, skip the key, load remaining valid keys. Do not crash. If the missing key was a required flag, chapter may be incompleteable — this is a content authoring error, not a system error.

**EC3: Concurrent Mutations in Same Frame**
- **If** two systems call `Mutate()` for the same key in the same frame: process in call order (no parallel processing). Second mutation's event fires after the first. Subscribers receive both events. No special handling required if call order is deterministic (it is, single-threaded Unity main thread).

**EC4: Undo Past Chapter Boundary**
- **If** player calls `Undo()` during a chapter transition: fail silently. Chapter transitions clear the undo queue. No UI feedback is shown.

**EC5: Undo During Cutscene**
- **If** player calls `Undo()` during `CUTSCENE` state: fail silently. Cutscenes are non-undoable by design. No UI feedback shown.

**EC6: State Change During Load**
- **If** `Load()` is called while another system is processing a `StateChanged` event: queue the Load until the current event processing completes. Use a `_pendingLoad` flag. After all current events drain, execute the pending Load, emit `StateFullyLoaded`.

**EC7: Trust Value at Boundary (0 or 100)**
- **If** a trust mutation would push either trust meter to 0 or 100: clamp to boundary, emit the `StateChanged` event with the clamped value, then emit an additional `TrustBoundaryReached(imperial/underground, 0/100)` event. This second event triggers narrative consequences (e.g., game over condition at T_u = 0).

**EC8: Relationship Value at Extreme (±100)**
- **If** `R_c` reaches –100: character becomes hostile, blocks all further contact. This is a narrative flag `character.{id}.hostile = true` set simultaneously with `R_c = –100`.
- **If** `R_c` reaches +100: character reaches maximum loyalty. No additional flag set. Further positive relationship gains from dialogue choices are applied to a separate `trust_depth` sub-value (not displayed, used for secret ending tracking only).

**EC9: Mutate Unknown Key (Production)**
- **If** a system calls `Mutate("unknown.key", value)` in production (schema open=false): reject, log error, throw `NSMUnknownKeyException`. In development (open=true): accept and auto-register the key with type inferred from value.

**EC10: Undo Queue Empty**
- **If** `Undo()` called with empty queue: fail silently. No error, no exception. UI should grey out the undo button when queue is empty.

**EC11: Loading a Save from a Different Chapter**
- **If** player loads a save whose `chapter.current` differs from the current chapter index (e.g., loading a Ch.3 save while in Ch.5): treat as full state restore. Scene Management calls Episode Structure to load the saved chapter. Do not attempt to "patch" the current chapter's state.

## Dependencies

### Upstream (NSM depends on these)

| System | Dependency Type | Interface |
|--------|---------------|-----------|
| **Save/Load System** | Hard | Calls `NSM.Save()` / `NSM.Load()`. NSM provides serializable state; Save/Load handles file I/O. |
| **Chapter Content Data** | Hard | Provides the NSM schema (key definitions, types, ranges, defaults) at chapter load. NSM will not function without a valid schema loaded. |

### Downstream (these systems depend on NSM)

| System | Dependency Type | Interface |
|--------|---------------|-----------|
| **Dual Trust Economy** | Hard | Subscribes to `StateChanged("trust.*")`. Reads `trust.imperial`, `trust.underground`. Calls `Mutate()` to apply trust changes. |
| **Branching Dialogue System** | Hard | Reads `character.*.relationship`, `flags.*`, `dialogue.history`. Calls `Mutate()` for choices and flags. |
| **Relationship Memory** | Hard | Subscribes to `StateChanged("character.*")`. Calls `Mutate()` for relationship deltas. |
| **Episode Structure** | Hard | Calls `Mutate()` for `chapter.current`, `scene.current`. Subscribes to `StateChanged("chapter.current")`. |
| **Scene Management** | Soft | Subscribes to `StateFullyLoaded` after `Load()` completes. No data coupling — only event timing. |
| **Clue & Intel Collection** | Hard | Reads `clues.*` flags. Calls `Mutate()` to set clue flags when found. |
| **HUD (Trust Meters)** | Hard | Subscribes to `StateChanged("trust.*")` for real-time UI updates. |
| **Notification System** | Soft | Subscribes to `StateChanged("trust.*")` for trust shift toasts. |

### Bidirectional Consistency Check

- If NSM adds a new top-level key namespace (e.g., `"inventory.*"`), the system that owns it must list NSM as an upstream hard dependency.
- If Branching Dialogue expects a flag key that NSM doesn't store, flag the conflict before implementation.

## Tuning Knobs

The following values are adjustable without code changes — they live in a `NSMConfig` ScriptableObject:

| Knob | Default | Safe Range | What Breaks If Too High | What Breaks If Too Low |
|------|---------|-----------|------------------------|------------------------|
| `MAX_UNDO_MUTATIONS` | 20 | 5–50 | Memory usage grows (20 × mutation size) | Player loses too much undo history |
| `MAX_CHOICE_HISTORY` | 200 | 50–∞ | Memory grows unbounded in very long chapters | No practical impact |
| `TRUST_MIN` | 0.0 | fixed | N/A | N/A |
| `TRUST_MAX` | 100.0 | fixed | N/A | N/A |
| `RELATIONSHIP_MIN` | –100.0 | fixed | N/A | N/A |
| `RELATIONSHIP_MAX` | 100.0 | fixed | N/A | N/A |
| `TRUST_DANGER_THRESHOLD` | 25.0 | 10–40 | HUD danger pulse triggers too early or too late | Player doesn't see warning before crisis |
| `RELATIONSHIP_HOSTILE_THRESHOLD` | –100.0 | fixed | N/A | N/A |
| `UNDO_BLOCKED_IN_STATES` | [CUTSCENE, CHAPTER_TRANSITION] | any NSM states | Player can undo cutscenes (narrative inconsistency) | N/A |
| `SCHEMA_STRICT_MODE` | true | bool | If false, new keys auto-register (dev only, never ship) | Unknown keys accepted silently (bugs hidden) |

**Note:** These are NSM-internal tuning knobs. Trust change amounts (ΔT per choice) are defined in the **Dual Trust Economy** GDD, not here.

## Acceptance Criteria

**AC1: State Mutation**
- **GIVEN** NSM is initialized with schema containing `trust.imperial: float [0, 100] = 50`
- **WHEN** `Mutate("trust.imperial", 75)` is called
- **THEN** `Get("trust.imperial")` returns `75`, and `StateChanged("trust.imperial", 50, 75)` event is emitted

**AC2: Clamping**
- **GIVEN** `trust.underground = 95`, schema range [0, 100]
- **WHEN** `Mutate("trust.underground", 110)` is called
- **THEN** the stored value is `100` (not 110), and `StateChanged("trust.underground", 95, 100)` is emitted

**AC3: Undo**
- **GIVEN** `Mutate("trust.imperial", 75)` was the last of 3 mutations in the undo queue
- **WHEN** `Undo()` is called
- **THEN** `Get("trust.imperial")` returns the previous value (50), and `StateChanged("trust.imperial", 75, 50)` is emitted

**AC4: Undo Queue Empty**
- **GIVEN** `Undo()` is called with an empty undo queue
- **THEN** no exception is thrown, no event is emitted, state is unchanged

**AC5: Serialization Round-Trip**
- **GIVEN** NSM state with `trust.imperial=60, trust.underground=40, chapter.current=2`
- **WHEN** `Save()` returns JSON, then `Load(json)` is called
- **THEN** `Get("trust.imperial")=60`, `Get("trust.underground")=40`, `Get("chapter.current")=2`

**AC6: Hash Integrity**
- **GIVEN** a save file with hash mismatch (corrupted JSON)
- **WHEN** `Load()` is attempted
- **THEN** `Load()` returns `Result.Failed`, emits `DataCorrupted` event, does not modify NSM state

**AC7: Subscription Filter**
- **GIVEN** a subscriber calls `Subscribe("trust.*", callback)`
- **WHEN** `Mutate("character.liu.reputation", 10)` is called
- **THEN** the trust subscriber's callback is NOT invoked (key doesn't match "trust.*")
- **AND** `StateChanged("character.liu.reputation", old, new)` is emitted for matching subscribers

**AC8: Schema Unknown Key (Production)**
- **GIVEN** `SCHEMA_STRICT_MODE=true` and schema does not contain key `"new.key"`
- **WHEN** `Mutate("new.key", true)` is called
- **THEN** `NSMUnknownKeyException` is thrown, state is unchanged, no event emitted

**AC9: Trust Boundary Event**
- **GIVEN** `trust.underground = 26`, `TRUST_DANGER_THRESHOLD = 25`
- **WHEN** `Mutate("trust.underground", 20)` is called (crosses threshold)
- **THEN** after `StateChanged`, a `TrustBoundaryReached("underground", 20)` event is emitted

**AC10: Load Clears Undo Queue**
- **GIVEN** undo queue has 10 entries
- **WHEN** `Load(json)` is called successfully
- **THEN** undo queue is empty

## Open Questions

| # | Question | Owner | Target Resolution |
|---|----------|-------|------------------|
| **OQ1** | Should NSM use a full-state-replace on Load, or a merge (patch) strategy that preserves state keys not in the save file? Full-replace is simpler; merge avoids losing new keys added after a save was made. | Systems Designer | Before Save/Load GDD is authored |
| **OQ2** | Do we need a "developer console" that allows `Mutate()` calls to be triggered manually during playtesting? Useful for QA to test specific trust states. | QA Lead | Before first playtest |
| **OQ3** | Should the NSM emit a `CheckpointReached` event that Save/Load auto-subscribes to for autosave triggers? Or does Save/Load poll `IsDirty()`? | Save/Load Designer | Before Save/Load GDD is authored |
| **OQ4** | How does the NSM handle concurrent access from Unity Job threads? (Currently assumed single-threaded main thread only.) If we add background loading or async scene transitions, we may need thread-safe mutation queuing. | Engine Programmer | Before Alpha if async loading is considered |
