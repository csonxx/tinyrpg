# Story: S1-7 — Sample Chapter Content Data

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: should-have
> **Status**: backlog
> **Estimate**: 2 days
> **Owner**: writer
> **Type**: Config/Data
> **ADR**: No ADR applies — content data only
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Author one complete dialogue scene for use in the Sprint 1 vertical slice. The scene must have 5+ TEXT nodes, 2 CHOICE nodes with asymmetric trust shifts, and 3 distinct endings.

## Content Specifications

### Scene: "The Assignment" (ch1_scene1)

**Setup**: Captain YAMAMOTO briefs the player on a shipment operation. The player must navigate loyalty to the imperial authority while protecting underground contacts.

**Characters**: YAMAMOTO (imperial handler), player (protagonist)

**Choice 1** (3 options, asymmetric trust):
- Option A: Loyal compliance → Imperial +8, Underground -3
- Option B: Subtle warning to underground → Imperial -5, Underground +10
- Option C: Silent observation → Imperial +2, Underground +2

**Choice 2** (3 options, different consequences):
- Option A: Invoke LIU incident → Imperial +12, Underground -8
- Option B: Request written orders → Imperial +3, Underground +7
- Option C: Minimal acknowledgment → Imperial +5, Underground +3

**Endings**:
- Ending A (high imperial): YAMAMOTO trusts you fully — but underground contact is lost
- Ending B (balanced): Some trust on both sides — uncertain future
- Ending C (high underground): Underground values you — but imperial suspicion grows

## Technical Requirements

- All text via localization keys (format: `dialogue_ch1_scene1_nodeX`)
- All trust shifts defined in CHOICE nodes
- Portrait IDs: `yamamoto`, `player`

## Acceptance Criteria

- [ ] All TEXT nodes display correctly in Dialogue UI
- [ ] Both CHOICE nodes present 3 options each
- [ ] All trust shifts apply on selection
- [ ] All 3 endings reachable
- [ ] Dialogue cursor saves/restores correctly

## Dependencies

- S1-3 (Branching Dialogue) — needs stable data schema
- Chapter Content Data GDD finalized

## Test Evidence

Location: `production/qa/smoke-[date].md` — smoke check pass verifying all nodes display, all choices apply correct shifts, all 3 endings reachable, cursor persists
