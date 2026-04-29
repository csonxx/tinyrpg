# S2-3: Relationship Memory

> **Type**: Logic
> **Status**: Ready for Dev
> **Sprint**: 2
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Dependencies**: S1-1 (NSM), S1-3 (Branching Dialogue)

## Overview

Implement Relationship Memory: per-character relationshipValue (0–100) stored in NSM, relationshipShift applied from dialogue choices, memoryFlags (boolean per character) for tracking key interactions, and passive decay after grace period.

## GDD Reference

- `design/gdd/relationship-memory.md` — full design spec

## Acceptance Criteria

- [ ] AC1: Relationship shifts accumulate — clamp(oldValue + delta, 0, 100)
- [ ] AC2: Memory flags persist and gate dialogue — `relationships.ZHANG.sawThrough_lie` enables conditional dialogue
- [ ] AC3: Relationship decay applies after 120s grace period; ticks every 60s
- [ ] AC4: Multiple character shifts from single choice work — `{ ZHANG: +10, LIU: -5 }`

## Formula

```
newRelationship = clamp(oldRelationship + delta, 0, 100)
decayPerTick = RELATIONSHIP_DECAY_RATE * (secondsSinceLastInteraction / 60)
```

## Files to Create

- `src/core/narrative/RelationshipMemorySystem.cs` — main MonoBehaviour
- `src/core/narrative/RelationshipShift.cs` — immutable struct
- `tests/unit/narrative/RelationshipMemoryTests.cs` — unit tests
