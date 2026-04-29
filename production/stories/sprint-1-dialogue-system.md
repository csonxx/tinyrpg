# Story: S1-3 — Branching Dialogue System

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: must-have
> **Status**: ready-for-dev
> **Estimate**: 4 days
> **Owner**: gameplay-programmer

---

## Overview

Implement the dialogue tree traversal engine: TEXT → CHOICE → TEXT flow, choice processing with trust shift application, NSM cursor tracking, and scene completion detection.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#)
- **Data Format**: `Dictionary<string, DialogueNode>` loaded from Chapter Content Data
- **GDD Ref**: `design/gdd/branching-dialogue-system.md`

## Dialogue Node Types

| Type | Behavior |
|------|----------|
| `TEXT` | Display text, wait for tap, advance to `nextNodeId` |
| `CHOICE` | Display choices, wait for selection, advance to selected `nextNodeId`, apply `trustShift` |
| `CONDITION` | Evaluate `conditionExpr` against NSM, branch to `trueNextNodeId` or `falseNextNodeId` |
| `END` | Emit `DialogueSceneComplete`, transition back to scene management |

## Core Rules

1. **Dialogue Cursor**: `dialogue.cursor = { sceneId, nodeId }` stored in NSM
2. **Choice Processing**: When CHOICE node reached:
   - Display choices via DialogueUI
   - On selection: apply `trustShift` via NSM event → TrustManager
   - Log to `dialogue.choiceHistory[]`
   - Advance to selected `nextNodeId`
3. **Text Auto-Advance**: Configurable delay (default: 5s for short text)
4. **Conditional Branching**: CONDITION nodes evaluate NSM keys (e.g., `trust.imperial >= 50`)

## Trust Shift Clamping

Per design: `clamp(Δ, -10, +10)` applied at the dialogue engine level before passing to TrustManager.

## NSM Keys Managed

| Key | Type | Description |
|-----|------|-------------|
| `dialogue.cursor.sceneId` | string | Current scene |
| `dialogue.cursor.nodeId` | string | Current node |
| `dialogue.choiceHistory` | array | All choices made this session |
| `dialogue.visitedNodes` | array | All nodes visited |

## Acceptance Criteria

- [ ] TEXT node: display text, wait for tap, advance to `nextNodeId`
- [ ] CHOICE node: display 2-3 choices, wait for selection, apply trust shift
- [ ] CHOICE node: apply `trustShift` clamped to ±10
- [ ] CONDITION node: evaluate `trust.imperial >= 50` → correct branch
- [ ] END node: emit `DialogueSceneComplete`
- [ ] Dialogue cursor persists across save/load
- [ ] Choice history recorded in NSM
- [ ] Rapid tap during text animation: first tap cancels animation, second advances

## Open Questions

| OQ2 | Do we support "secret" choices hidden until condition is met? | Game Designer — deferred |
|-----|-------------------------------------------------------------|-----------------------|

## Dependencies

- S1-1 (NSM) — dialogue cursor stored in NSM
- S1-2 (Save/Load) — cursor must survive save/load
- Touch Input (S1-4) — sends choice selection signals
- Dialogue UI (S1-5) — receives display events
