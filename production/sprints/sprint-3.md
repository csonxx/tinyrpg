# Sprint 3 -- 2026-05-11 to 2026-05-22

## Sprint Goal

Complete Vertical Slice: Settings System, Notification System, Accessibility System, and Branching Path Resolution — finishing all systems needed for a polished first playable chapter.

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for integration, bug fixes
- Available: **8 effective days**

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|---------------------|
| S3-1 | **Settings System** — volume (music/sfx/voice), text speed, haptic feedback, auto-advance toggles, JSON persistence to device storage | gameplay-programmer | 2 | S2-4 (Audio) | All settings persist after app restart; volume changes apply immediately; text speed affects dialogue animation |
| S3-2 | **Notification System** — trust change toast (+8 Imperial! / -5 Underground!), danger zone warnings, milestone notifications, queue-based display | gameplay-programmer | 2 | S1-8 (Dual Trust) | Toast appears on trust change; danger zone shows amber warning; crisis shows red flash; notifications queue correctly |
| S3-3 | **Accessibility System** — text size (small/normal/large), colorblind mode (deuteranopia/protanopia), reduce motion (skip fade transitions), screen reader labels | ui-programmer | 2 | S1-5 (Dialogue UI) | Text scales correctly; colorblind modes adjust palette; reduce motion skips fades; all interactive elements have labels |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|---------------------|
| S3-4 | **Branching Path Resolution** — evaluate CONDITION nodes referencing NSM state, player choices, relationship values, clue flags; dynamically select next scene | gameplay-programmer | 3 | S2-1 (Episode Structure), S2-3 (Relationship Memory), S2-5 (Clue & Intel) | Conditional branches route correctly based on NSM state; relationship values gate options; clues unlock hidden paths |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Branching path complexity — state explosion as branches multiply | Medium | Medium | MVP: max 2-level deep branching; full tree traversal in Sprint 4 |
| Accessibility system requires art team coordination for color variants | Low | Medium | Use shader-based color matrix transforms; no new art assets needed |

## Dependencies on External Factors

- **No external dependencies** — all systems use existing NSM data and existing UI components
- **Unity scene assets**: Not required — all systems work with existing dialogue UI infrastructure

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-3.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS
- [ ] No S1 or S2 bugs in delivered features
- [ ] Code reviewed and merged
