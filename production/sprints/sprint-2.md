# Sprint 2 -- 2026-05-11 to 2026-05-22

## Sprint Goal

Complete the scene/episode infrastructure and relationship memory system, enabling multi-scene chapters with persistent character relationships and clue discovery.

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for integration, bug fixes
- Available: **8 effective days**

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|---------------------|
| S2-1 | **Episode Structure** — chapter/scene hierarchy, scene sequencing, chapter+episode completion, branching path resolution, NSM state management | gameplay-programmer | 3 | S1-1, S1-3, S1-7 | Chapter completes when last scene ends; episode ends after final chapter; mid-episode save/resume works; flashback uses FADE_BLACK |
| S2-2 | **Scene Management** — Unity SceneManager wrapper, async loading, FADE_GREY/FADE_BLACK/CROSSFADE transitions, scene stack (push overlay), Addressables preload | gameplay-programmer | 2 | S1-1, S1-2 | LoadScene emits SceneReady; transition animations play correctly; PushOverlay/Pop works; preload eliminates visible loading |
| S2-3 | **Relationship Memory** — per-character relationshipValue (0–100) in NSM, relationshipShift from dialogue choices, memoryFlags (bool per character), passive decay | gameplay-programmer | 2 | S1-1, S1-3 | Relationship shifts accumulate; memory flags gate dialogue; decay applies after grace period |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|---------------------|
| S2-4 | **Audio Management** — BGM/SFX/Voice event handling, scene-linked BGM (SceneReady trigger), menu pause/resume, volume multipliers from Settings | gameplay-programmer | 2 | S1-1 | Scene load triggers correct BGM; menu open pauses audio; volume settings apply |
| S2-5 | **Clue & Intel Collection** — clue NSM keys (bool), registration on node/choice completion, clue gating in CONDITION nodes, Intelligence Journal UI | ui-programmer | 2 | S1-1, S1-3 | Clues register on node completion; gated choices appear when clue discovered; journal shows discovered clues and undiscovered as "???" |
| S2-6 | **Menu System** — pause menu, save/load screen, back navigation, NSM MENU_OPEN state, touch input disable/enable | ui-programmer | 3 | S1-2, S1-4, S2-4 (Settings) | Menu opens and pauses game; save/load slots display correctly; back navigation works at every level; Settings screen accessible |

## Carryover from Previous Sprint

None — all Sprint 1 Must Haves are complete.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Scene Management Addressables integration complexity | Medium | Medium | Use simple async LoadAssetAsync; defer advanced pooling to later sprint |
| Episode Structure branching logic state explosion | Medium | Medium | MVP validates with linear sequence only; branching path resolution is Sprint 3 |
| Menu System depends on Settings which depends on Audio — long dependency chain | Low | Medium | Audio (S2-4) is small; can run in parallel with S2-1/S2-2 |

## Dependencies on External Factors

- **Unity scene assets**: Scene Management requires actual Unity scene files (.unity) — art team must deliver scene backgrounds for at least one chapter before this can be fully validated
- **Chapter Content Data**: Episode Structure reads scene sequence from data; ensure S1-7 chapter data covers multi-scene chapter for testing

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-2.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged
