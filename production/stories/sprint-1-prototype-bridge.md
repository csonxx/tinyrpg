# Story: S1-9 — Prototype to Production Bridge

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: nice-to-have
> **Status**: backlog
> **Estimate**: 1 day
> **Owner**: gameplay-programmer
> **Type**: Logic
> **ADR**: No ADR applies — architecture pattern validation
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Extract key learnings from the dialogue prototype (`prototypes/dialogue-trust-loop/`) and apply them to the production NSM event design. Specifically: validate that the event bus pattern and trust shift signal format work for the production architecture.

## Guidance

The prototype validated:
- Trust shift formula: `clamp(Δ, -10, +10)`
- Choice hesitation timing suggests choices feel weighty
- Trust bar animation: 400ms ease-out

**Do not copy prototype code.** Only extract design patterns.

## Acceptance Criteria

- [x] NSM event format reviewed against prototype learnings
- [x] Trust shift clamping confirmed at ±10
- [x] Event signal names validated (e.g., `Trust.Changed` vs `TrustValueChanged`)
- [x] Prototype learnings documented in this story

## Findings

### NSM Event Format: VALIDATED

- Event bus pattern confirmed: glob-pattern routing (`trust.*`, `dialogue.*`) over direct C# events
- Event keys follow `subject.verb` naming: `trust.boundary`, `nsm.state`, `nsm.undo`, `dialogue.trust_shift`
- Prototype direct-events pattern evolved correctly to production event bus

### Trust Shift Clamping: CONFIRMED AT ±10

- `DialogueEngine.MAX_TRUST_SHIFT = 10f`
- `Mathf.Clamp(rawTrustShift, -MAX_TRUST_SHIFT, MAX_TRUST_SHIFT)` enforced at choice selection
- Tests verify: `CHOICE_Node_TrustShift_ClampedAtPlus10`, `CHOICE_Node_TrustShift_ClampedAtMinus10`
- Clamped value emitted via `DialogueTrustShiftEvent.ClampedShift` for UI animation

### Event Signal Names: VALIDATED

- Production uses `trust.boundary` (not `TrustValueChanged`) - appropriate for boundary-only events
- Dialogue events: `dialogue.node_changed`, `dialogue.scene_complete`, `dialogue.choices_displayed`, `dialogue.trust_shift`
- NSM events: `nsm.state`, `nsm.undo`, `nsm.schema_validation_failed`
- No naming conflicts; pattern is extensible

### Prototype Learnings Not Yet Implemented (Out of Scope)

- Danger zone (≤25) and crisis zone (≤15) events - not required by current story
- Trust bar 400ms ease-out animation - UI implementation detail
- These remain as potential future enhancements

### Test Coverage

- `NarrativeStateMachineTests.cs`: 30+ tests covering Mutate, Set, Get, Subscribe, Undo, Serialize, TrustBoundary
- `DialogueEngineTests.cs`: 25+ tests covering TEXT/CHOICE/CONDITION/END nodes, trust clamping, cursor persistence, auto-advance

## Dependencies

- S1-1 (NSM)
- S1-3 (Branching Dialogue)
- Prototype at `prototypes/dialogue-trust-loop/`

## Test Evidence

Location: `production/qa/evidence/` — code review sign-off confirming event format, signal names, and trust shift clamping documented
