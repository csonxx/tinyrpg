# S2-6: Menu System

> **Type**: UI
> **Status**: Backlog
> **Sprint**: 2
> **Estimate**: 3 days
> **Owner**: ui-programmer
> **Dependencies**: S1-2 (Save/Load), S1-4 (Touch Input), S2-4 (Audio — for Settings)

## Overview

Implement Menu System: pause menu with Continue/Save/Load/Settings/Quit, save/load slot screens with metadata display, back navigation at all levels, NSM MENU_OPEN state, and touch input disable/enable on open/close.

## GDD Reference

- `design/gdd/menu-system.md` — full design spec

## Acceptance Criteria

- [ ] AC1: Menu opens on pause button tap; NSM enters MENU_OPEN; audio pauses; touch disabled
- [ ] AC2: Save screen shows 3 manual slots + autosave; slot shows chapter/scene/timestamp/playtime
- [ ] AC3: Load screen restores exact game state; returns player to saved scene
- [ ] AC4: Settings accessible; values persist via Settings System (or hardcoded for MVP)
- [ ] AC5: Back navigation works at every level (Settings→Pause→Close; Load→Pause→Close)

## Files to Create

- `src/ui/menu/PauseMenu.cs` — pause menu MonoBehaviour
- `src/ui/menu/SaveLoadScreen.cs` — save/load slot UI
- `src/ui/menu/SettingsScreen.cs` — settings UI
- `tests/integration/ui/MenuSystemTests.cs` — integration tests (UI interaction)

## Notes

- Depends on S2-4 Audio Management for Settings screen
- Actual Settings System is separate story — Settings screen reads/writes hardcoded prefs for MVP
- Unity UI (UGUI) or UI Toolkit — follow existing UI patterns from S1-5 Dialogue UI
