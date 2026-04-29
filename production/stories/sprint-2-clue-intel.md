# S2-5: Clue & Intel Collection

> **Type**: Logic/UI
> **Status**: Ready for Dev
> **Sprint**: 2
> **Estimate**: 2 days
> **Owner**: ui-programmer
> **Dependencies**: S1-1 (NSM), S1-3 (Branching Dialogue)

## Overview

Implement Clue & Intel Collection: boolean clue flags in NSM, registration on dialogue node completion and choice selection, clue gating in CONDITION nodes, and Intelligence Journal UI accessible from pause menu.

## GDD Reference

- `design/gdd/clue-intel-collection.md` — full design spec

## Acceptance Criteria

- [ ] AC1: Node completion with `registerClue` sets NSM `clues.{clueId}` to true
- [ ] AC2: Choice selection can register a clue
- [ ] AC3: CONDITION node evaluating `clues.{clueId} == true` makes gated choice appear
- [ ] AC4: Intelligence Journal shows discovered clues by category; undiscovered as "???"

## Files to Create

- `src/core/narrative/ClueSystem.cs` — clue registration and NSM interface
- `src/ui/journal/IntelligenceJournalUI.cs` — journal screen UI
- `tests/unit/narrative/ClueIntelTests.cs` — unit tests

## Notes

- Intelligence Journal UI is the only UI representation of the clue system
- Clue discovery is entirely data-driven (via Chapter Content Data node definitions)
