# Menu System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Meta — pause and save infrastructure; enables the player to step away and return)

## Overview

Menu System manages all game menus: the pause menu (accessible via pause button or OS back gesture), the save/load screen, and the settings screen. It is the player's way of stepping out of the narrative — saving progress, adjusting volume, or exiting to title. It operates as an overlay that pauses the game state beneath it.

The pause menu is the primary entry point for the Save/Load System and Settings System, which are displayed as sub-screens within the menu system.

**Owned data:** Menu navigation state, which sub-screen is open. **Not owned:** the individual settings values (Settings System), save slot data (Save/Load System).

## Player Fantasy

**Indirect.** Players use the menu system briefly — to save, to adjust settings, or to step away. It should feel unobtrusive and efficient: open quickly, find what you need, return to the game. It should not feel like leaving the game world — the menu's visual style matches the game's desaturated, period-appropriate aesthetic so it feels like part of the same world.

## Detailed Design

### Core Rules

**Rule 1: Menu Trigger**
Menu opens via:
- Pause button (tapped in top-right corner of HUD)
- OS back gesture (Android back button, iOS swipe-from-edge)
- Home button press (OS-level, game goes to background — treated as implicit pause)

**Rule 2: Pause on Open**
When the menu opens, the game loop pauses (Narrative State Machine enters `MENU_OPEN` state). Audio pauses with it. The menu overlay fades in over 200ms.

**Rule 3: Menu Structure**
```
PauseMenu
├── Title: "雾中誓言"
├── Continue  → closes menu, resumes game
├── Save Game  → opens SaveScreen
├── Load Game  → opens LoadScreen
├── Settings   → opens SettingsScreen
└── Quit to Title  → confirms, returns to title screen
```

**Rule 4: Save Screen**
Displays 3 manual save slots + autosave slot. Each slot shows: chapter name, scene name, timestamp, play time. Overwriting a slot requires a confirmation dialog: "Overwrite this save?" with Confirm/Cancel.

**Rule 5: Load Screen**
Same slot display as Save. Loading a save emits `MenuClosed` and then `SaveSystem.Load(slotId)`.

**Rule 6: Settings Screen**
Displays: Volume (Music, SFX, Voice), Text Speed (Slow/Medium/Fast/Auto), Haptic Feedback (On/Off), Auto-Advance (On/Off). All settings persist to device storage via Settings System.

**Rule 7: Back Navigation**
On all sub-screens (Save, Load, Settings), a back button returns to the Pause Menu. On the Pause Menu, back button/gesture closes the menu and resumes the game.

### States and Transitions

| State | Description | Valid Transitions |
|-------|-------------|-----------------|
| `CLOSED` | No menu visible | → `OPEN_PAUSE` (on menu trigger) |
| `OPEN_PAUSE` | Pause menu visible | → `CLOSED` (on Continue/back), → `OPEN_SAVE`, → `OPEN_LOAD`, → `OPEN_SETTINGS` |
| `OPEN_SAVE` | Save screen visible | → `OPEN_PAUSE` (on back) |
| `OPEN_LOAD` | Load screen visible | → `OPEN_PAUSE` (on back), → `CLOSED` (on load) |
| `OPEN_SETTINGS` | Settings screen visible | → `OPEN_PAUSE` (on back) |
| `CONFIRM_OVERWRITE` | Overwrite confirmation dialog | → `OPEN_SAVE` (on cancel), → `SAVING` (on confirm) |
| `SAVING` | Save in progress | → `OPEN_SAVE` (on complete) |

### Interactions with Other Systems

**→ Narrative State Machine:**
- On menu open: NSM enters `MENU_OPEN` state
- On menu close: NSM returns to previous state

**→ Touch Input System:**
- On menu open: `TouchInput.Disable()`
- On menu close: `TouchInput.Enable()`

**→ Save/Load System:**
- Save screen calls `SaveSystem.Save(slotId)`
- Load screen calls `SaveSystem.Load(slotId)`

**→ Settings System:**
- Settings screen reads/writes settings via `Settings.Get(key)` / `Settings.Set(key, value)`

**→ Audio Management:**
- On menu open: `Audio.PauseMusic()`, `Audio.PauseSFX()`
- On menu close: `Audio.ResumeMusic()`, `Audio.ResumeSFX()`

## Formulas

Menu System has no complex formulas — it manages navigation state and delegates to other systems.

## Edge Cases

- **If player opens menu during a cutscene**: Allow it — cutscene pauses. Menu is accessible at any time.
- **If player tries to load a corrupt save**: Save/Load System shows error dialog; player returns to Load screen. Do not crash.
- **If player tries to overwrite the autosave**: Allow it without special confirmation (autosave is not precious).
- **If player is in the final scene of an episode and opens menu**: All options work normally.
- **If device receives phone call while menu is open**: Menu closes, game pauses at OS level. On return, menu reopens automatically.

## Dependencies

- Upstream: **Save/Load System** (provides slot data for Save/Load screens)
- Upstream: **Settings System** (provides settings values for Settings screen)
- Downstream: **Narrative State Machine** (drives pause/resume)
- Downstream: **Touch Input System** (enables/disables input)
- Downstream: **Audio Management** (pause/resume audio)

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `MENU_FADE_DURATION` | 200ms | 100–400ms | Menu overlay fade-in/out duration |
| `CONFIRM_DIALOGUE_ENABLED` | true | bool | Require confirmation before overwrite |

## Acceptance Criteria

**AC1: Menu Opens and Pauses Game**
- **GIVEN** player taps pause button
- **WHEN** menu opens
- **THEN** game loop pauses (NSM in MENU_OPEN); audio pauses; menu overlay fades in over 200ms

**AC2: Save Game Writes to Slot**
- **GIVEN** player is on Save screen with slot 1 selected and empty
- **WHEN** player taps "Save"
- **THEN** `SaveSystem.Save(1)` is called; slot 1 now shows current chapter, scene, timestamp, play time

**AC3: Load Game Restores State**
- **GIVEN** player is on Load screen with slot 1 containing a valid save
- **WHEN** player taps "Load" on slot 1
- **THEN** menu closes; game state is restored; player is in the saved scene

**AC4: Settings Persist After Menu Close**
- **GIVEN** player adjusts text speed to Fast in Settings
- **WHEN** player closes menu and reopens it
- **THEN** text speed is still Fast

**AC5: Back Navigation Works at Every Level**
- **GIVEN** player is in Settings sub-screen
- **WHEN** back button is tapped
- **THEN** Returns to Pause Menu
- **WHEN** back is tapped again
- **THEN** Menu closes, game resumes

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should there be a "Continue" save slot (last played position) separate from manual slots? | Game Designer |
| OQ2 | Do we support cloud save via mobile platform account (iCloud/Google Play Games)? | Producer |
