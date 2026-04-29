# S2-1: Episode Structure

> **Type**: Logic/Integration
> **Status**: Ready for Dev
> **Sprint**: 2
> **Estimate**: 3 days
> **Owner**: gameplay-programmer
> **Dependencies**: S1-1 (NSM), S1-3 (Branching Dialogue), S1-7 (Chapter Content Data)

## Overview

Implement Episode Structure: the system that manages chapter/scene hierarchy, scene sequencing, chapter+episode completion, and branching path resolution within an episode.

## GDD Reference

- `design/gdd/episode-structure.md` — full design spec

## Acceptance Criteria

- [ ] AC1: Chapter completes when last scene ends — NSM chapter.current increments; autosave triggers; next chapter loads
- [ ] AC2: Episode ends after final chapter — EPISODE_COMPLETE state entered; credits display; return to menu
- [ ] AC3: Mid-episode save/resume — reading NSM chapter.current and dialogue.cursor restores exact position
- [ ] AC4: Flashback scene uses FADE_BLACK transition

## Files to Create

- `src/core/narrative/EpisodeStructure.cs` — main MonoBehaviour
- `src/core/narrative/EpisodeState.cs` — state enum and data
- `tests/unit/narrative/EpisodeStructureTests.cs` — unit tests

## Notes

- MVP validates linear sequence only; branching path resolution is Sprint 3
- Episode Structure manages sequencing; it does not own dialogue content (Chapter Content Data does)
