# Sprint 1 -- 2026-05-11 to 2026-05-22

## Sprint Goal

**Deliver a playable vertical slice**: one complete dialogue scene with the full NSM-driven core loop (trust shifts, choice branching, save/load, tap input).

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for integration, bug fixes, design doc updates
- Available: **8 effective days**

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-1 | **Narrative State Machine** — flat KV store, event subscription, mutation interface, NSM state machine (8 states), undo queue, schema validation | gameplay-programmer | 5 | — | NSM accepts `Mutate(event, delta)`; state serializes to/from JSON; undo works; events route to subscribers |
| S1-2 | **Save/Load System (MVP)** — JSON serialization of NSM state, 3 manual slots + autosave slot, SHA256 hash integrity, corrupt save rejection | gameplay-programmer | 3 | S1-1 | Save writes valid JSON; Load restores exact NSM state; hash mismatch shows error dialog |
| S1-3 | **Branching Dialogue System** — dialogue tree traversal (TEXT/CHOICE/CONDITION/END), choice processing, trust shift application, cursor persistence | gameplay-programmer | 4 | S1-1, S1-2 | Tree traverses correctly; choices trigger trust shifts; cursor saves/restores on save/load |
| S1-4 | **Touch Input System** — tap detection (300ms/20px), context-based routing (DIALOGUE_ACTIVE vs CHOICE_ACTIVE), enable/disable/block states | gameplay-programmer | 2 | S1-3 | Tap during text → advance; tap during choices → select; no ghost taps during transitions |
| S1-5 | **Dialogue UI** — speaker name, text animation (30ms/char), choice buttons (3-4 max), portrait anchor (player left/NPC right) | ui-programmer | 3 | S1-3 | Text animates; tap skips animation; choices display and navigate; speaker/narration modes work |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-6 | **HUD Trust Meters** — imperial + underground bars (top-left), danger zone at ≤25 (amber pulse), crisis at ≤15 (red flash) | ui-programmer | 2 | S1-1 (Dual Trust) | Bars animate on trust change; danger/crisis visuals trigger correctly |
| S1-7 | **Sample Chapter Content Data** — 1 scene with 5+ TEXT nodes, 2 CHOICE nodes with asymmetric trust shifts, 3 endings | writer + game-designer | 2 | S1-3 | Dialogue tree loads from data; all choices reachable; trust shifts apply correctly |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-8 | **Dual Trust Economy** — trust tracking (0-100), danger/crisis thresholds, notification on threshold crossing | gameplay-programmer | 1 | S1-1 | Trust values clamp correctly; danger zone events fire at right thresholds |
| S1-9 | **Prototype to Production Bridge** — extract prototype's `TrustManager` + `DialogueEngine` into NSM-driven architecture | gameplay-programmer | 1 | S1-1, S1-3 | Prototype learnings inform NSM event design |

## Carryover from Previous Sprint

None — this is the first sprint.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| NSM scope creep (single source of truth is tempting to over-design) | High | High | Hard limit: 5 days for NSM. If not done, defer to Sprint 2. |
| Dialogue tree data format not finalized | Medium | Medium | Use prototype's simple Dict<string, DialogueNode> format; Chapter Content Data schema refinement is Sprint 2 work |
| Solo dev unfamiliar with Unity | High | Medium | Daily /help sessions; prototype code as reference; keep Unity patterns simple (no DOTS/ECS) |

## Dependencies on External Factors

- **Chapter Content Data schema** must be finalized before S1-7 (writer needs stable schema to write dialogue)
- **Art bible** already complete — UI styling follows established palette

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] NSM passes unit tests for: mutate, undo, serialization, event routing
- [ ] Save/Load round-trip verified (save → load → exact state restored)
- [ ] One complete dialogue scene playable from start to end
- [ ] All 3 ending paths reachable through different choice combinations
- [ ] Trust bars visible and updating correctly
- [ ] No S1 (crash) or S2 (major bug) bugs in delivered features
- [ ] All code in `src/` (not `prototypes/`)
- [ ] Story files created for Sprint 2
