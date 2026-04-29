# S3-4: Branching Path Resolution

> **Type**: Logic/Integration
> **Status**: Ready for Dev
> **Sprint**: 3
> **Estimate**: 3 days
> **Owner**: gameplay-programmer
> **Dependencies**: S2-1 (Episode Structure), S2-3 (Relationship Memory), S2-5 (Clue & Intel)

## GDD Reference
- `design/gdd/episode-structure.md` (Rule 5: Branching Path Selection)

## Acceptance Criteria
- [ ] AC1: CONDITION nodes evaluate NSM keys (trust values, relationship values, clue flags) to select branch
- [ ] AC2: Multiple conditions can be combined with AND/OR logic
- [ ] AC3: Branching choices are stored in NSM for save/resume
- [ ] AC4: Dead-end branches (no valid next scene) fall back to episode complete
- [ ] AC5: At least 3 branching paths reachable in sample chapter content

## Files to Create
- `src/core/narrative/BranchingResolver.cs` — condition evaluation and path selection
- `src/core/narrative/ConditionExpression.cs` — expression parser for CONDITION nodes
- `tests/unit/narrative/BranchingResolverTests.cs` — unit tests
