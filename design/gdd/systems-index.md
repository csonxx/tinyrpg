# Systems Index: 雾中誓言

> **Status**: Draft
> **Created**: 2026-04-29
> **Last Updated**: 2026-04-29
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

雾中誓言 is a narrative RPG / visual novel with a dual trust economy at its core. The player manages two simultaneous trust meters (enemy faction and underground resistance) across branching dialogue choices in an episode-based story set in 1940s occupied East Asia. The mechanical complexity is weighted toward data-driven narrative systems rather than real-time gameplay systems.

**Design pillars:** Identity Is a Cage / No Choice Is Perfect / Trust Is the Most Fragile Currency

**Core loop:** Read dialogue → Analyze subtext → Choose response → Trust shifts → Narrative advances

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|-----------|------------|
| 1 | Save/Load System | Persistence | MVP | Designed | design/gdd/save-load-system.md | — |
| 2 | Audio Management | Audio | MVP | Designed | design/gdd/audio-management.md | — |
| 3 | Scene Management | Core | MVP | Designed | design/gdd/scene-management.md | Save/Load System |
| 4 | Touch Input System | Input | MVP | Designed | design/gdd/touch-input-system.md | Scene Management |
| 5 | Narrative State Machine | Core | MVP | Designed | design/gdd/narrative-state-machine.md | Save/Load, Scene Management |
| 6 | Episode Structure | Narrative | MVP | Designed | design/gdd/episode-structure.md | Narrative State Machine, Scene Management |
| 7 | Dual Trust Economy | Core | MVP | Designed | design/gdd/dual-trust-economy.md | Narrative State Machine |
| 8 | Branching Dialogue System | Narrative | MVP | Designed | design/gdd/branching-dialogue-system.md | Narrative State Machine, Touch Input |
| 9 | Relationship Memory | Narrative | Vertical Slice | Designed | design/gdd/relationship-memory.md | Narrative State Machine |
| 10 | Clue & Intel Collection | Narrative | Vertical Slice | Designed | design/gdd/clue-intel-collection.md | Narrative State Machine |
| 11 | Chapter Content Data | Narrative | MVP | Designed | design/gdd/chapter-content-data.md | — |
| 12 | Dialogue UI | UI | MVP | Designed | design/gdd/dialogue-ui.md | Branching Dialogue System |
| 13 | HUD (Trust Meters) | UI | MVP | Designed | design/gdd/hud-trust-meters.md | Dual Trust Economy |
| 14 | Menu System | UI | MVP | Designed | design/gdd/menu-system.md | Save/Load System |
| 15 | Notification System | UI | Vertical Slice | Designed | design/gdd/notification-system.md | Dual Trust Economy |
| 16 | Settings System | Meta | Vertical Slice | Designed | design/gdd/settings-system.md | Audio Management |
| 17 | Accessibility System | Meta | Vertical Slice | Designed | design/gdd/accessibility-system.md | Dialogue UI |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | Infrastructure systems everything depends on |
| **Input** | Platform-specific input handling |
| **Persistence** | Save state and continuity |
| **Audio** | Music, SFX, and voice management |
| **Narrative** | Story delivery, dialogue, trust, relationships, clues |
| **UI** | Player-facing interface, HUD, menus |
| **Meta** | Settings, accessibility, analytics |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function. Without these, you can't test "is the dual-trust dialogue loop fun?" | First playable prototype | Design FIRST |
| **Vertical Slice** | Required for one complete, polished chapter. Demonstrates the full experience with art, audio, and UI. | Vertical slice / demo | Design SECOND |
| **Alpha** | All features present in rough form. | Alpha milestone | Design THIRD |
| **Full Vision** | Polish, edge cases, all chapters, full content. | Beta / Release | Design as needed |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Save/Load System** — All narrative state must be serializable; this is the persistence foundation
2. **Chapter Content Data** — All narrative content lives in data; this is the content foundation (not a software system but a data architecture)
3. **Audio Management** — Music and SFX play independently of other systems

### Core Layer (depends on foundation)

1. **Scene Management** — depends on: Save/Load System — loads/unloads scenes, manages transitions
2. **Touch Input System** — depends on: Scene Management — touch gestures in scene context
3. **Narrative State Machine** — depends on: Save/Load System, Scene Management — tracks all narrative state globally

### Feature Layer (depends on core)

1. **Episode Structure** — depends on: Narrative State Machine, Scene Management — manages chapter flow and transitions
2. **Dual Trust Economy** — depends on: Narrative State Machine — tracks imperial and underground trust values
3. **Branching Dialogue System** — depends on: Narrative State Machine, Touch Input System — dialogue tree traversal and choice selection
4. **Relationship Memory** — depends on: Narrative State Machine — tracks character relationship values
5. **Clue & Intel Collection** — depends on: Narrative State Machine — tracks discovered clues and intel flags

### Presentation Layer (depends on features)

1. **Dialogue UI** — depends on: Branching Dialogue System — renders dialogue boxes, speaker names, choice buttons
2. **HUD (Trust Meters)** — depends on: Dual Trust Economy — displays imperial and underground trust bars
3. **Menu System** — depends on: Save/Load System — pause, settings, save/load slots
4. **Notification System** — depends on: Dual Trust Economy — trust change feedback toasts

### Meta Layer

1. **Settings System** — depends on: Audio Management — volume, text speed, auto-save
2. **Accessibility System** — depends on: Dialogue UI — text size, colorblind support, reduce motion

---

## Recommended Design Order

| Order | System | Priority | Layer | Est. Effort |
|-------|--------|----------|-------|-------------|
| 1 | Save/Load System | MVP | Foundation | S |
| 2 | Narrative State Machine | MVP | Core | M |
| 3 | Scene Management | MVP | Core | S |
| 4 | Touch Input System | MVP | Core | S |
| 5 | Dual Trust Economy | MVP | Feature | M |
| 6 | Branching Dialogue System | MVP | Feature | L |
| 7 | Episode Structure | MVP | Feature | M |
| 8 | Chapter Content Data | MVP | Foundation | M |
| 9 | Audio Management | MVP | Foundation | S |
| 10 | Dialogue UI | MVP | Presentation | M |
| 11 | HUD (Trust Meters) | MVP | Presentation | S |
| 12 | Menu System | MVP | Presentation | S |
| 13 | Relationship Memory | Vertical Slice | Feature | M |
| 14 | Clue & Intel Collection | Vertical Slice | Feature | M |
| 15 | Notification System | Vertical Slice | Presentation | S |
| 16 | Settings System | Vertical Slice | Meta | S |
| 17 | Accessibility System | Vertical Slice | Meta | S |

**Design sequence rationale:**
- State Machine first because EVERYTHING narrative depends on it — if the state model is wrong, everything built on top is wrong
- Trust Economy second because it's the unique hook — design it before dialogue so dialogue can use it well
- Dialogue System third because it's the core player-facing experience
- Presentation layer (UI, HUD, Menu) after the systems they wrap

---

## Circular Dependencies

- **None found.** The dependency graph is acyclic. The separation between Narrative State Machine (data tracker) and Branching Dialogue System (data consumer) avoids any circularity.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| **Branching Dialogue System** | Technical / Scope | State explosion — exponential content growth without modular scene architecture. Each additional choice branch multiplies content requirements. | Design modular scene architecture early. MVP validates with minimum 3 major choice points only. |
| **Narrative State Machine** | Technical | Single point of failure — all other narrative systems depend on it. A wrong model is expensive to fix later. | Prototype state serialization early. Test save/load round-trips before building dependent systems. |
| **Dual Trust Economy** | Design | Trust meters may feel like a game UI overlay rather than organic story consequence. Violates Pillar 3 if mechanical. | Design trust changes as narrative events first, UI updates second. Trust shifts should feel like natural story consequences. |
| **Clue & Intel Collection** | Scope | Investigation segments risk feeling like fetch quests. | MVP defers this system entirely. Design carefully in Vertical Slice phase. |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 17 |
| Design docs started | 17 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems (10) | 10/10 designed |
| Vertical Slice systems (7) | 7/7 designed |

---

## Next Steps

- [ ] Run `/design-review` on completed GDDs (in fresh session)
- [ ] Run `/consistency-check` to verify cross-GDD consistency
- [ ] Run `/gate-check pre-production` when all MVP GDDs are reviewed
- [ ] `/prototype dialogue` to validate branching dialogue core loop
- [ ] `/sprint-plan new` to plan first implementation sprint
