# Story: S1-9 — Prototype to Production Bridge

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: nice-to-have
> **Status**: backlog
> **Estimate**: 1 day
> **Owner**: gameplay-programmer

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

- [ ] NSM event format reviewed against prototype learnings
- [ ] Trust shift clamping confirmed at ±10
- [ ] Event signal names validated (e.g., `Trust.Changed` vs `TrustValueChanged`)
- [ ] Prototype learnings documented in this story

## Dependencies

- S1-1 (NSM)
- S1-3 (Branching Dialogue)
- Prototype at `prototypes/dialogue-trust-loop/`
