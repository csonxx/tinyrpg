# Audio Management

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Foundation — atmosphere and emotional tone; supports all pillars through music and sound)

## Overview

Audio Management handles all game audio: background music (BGM), sound effects (SFX), and character voice cues. It is the audio middleware between Unity's audio system and the game logic. It responds to game events (scene load, dialogue advance, menu open) to trigger appropriate audio.

Audio is entirely event-driven — no audio is hardcoded into scenes. Each audio event carries an audio key that references an Addressables audio asset.

## Core Rules

**Rule 1: Audio Event Types**
- `BGM_PLAY(audioKey)`: Starts looping BGM for the current scene
- `BGM_STOP()`: Stops current BGM with fade-out (500ms default)
- `SFX_PLAY(audioKey)`: Plays a one-shot sound effect
- `VOICE_PLAY(audioKey)`: Plays a character voice line (dialogue accompaniment)
- `VOICE_STOP()`: Interrupts currently playing voice

**Rule 2: Volume Control**
Master volume, BGM volume, SFX volume, and Voice volume are controlled via Settings System. All audio output is multiplied by the corresponding volume multiplier (0.0–1.0).

**Rule 3: Pause Behavior**
When NSM enters `MENU_OPEN` or `CUTSCENE`: audio pauses. When NSM returns to `DIALOGUE_ACTIVE`: audio resumes. This is driven by Menu System signals.

**Rule 4: Scene-Linked BGM**
Each scene definition in Chapter Content Data carries a `sceneMusic` field (Addressables key). When `SceneManagement` emits `SceneReady`, Audio Management starts that scene's BGM if it differs from the current BGM.

**Rule 5: Dialogue-Linked Voice**
Branching Dialogue System triggers `VOICE_PLAY` when displaying a dialogue line if a voice asset exists for that line in Chapter Content Data.

## Dependencies

- Upstream: **Scene Management** (triggers BGM on scene change)
- Upstream: **Branching Dialogue System** (triggers voice on dialogue display)
- Upstream: **Menu System** (triggers pause/resume)
- Upstream: **Settings System** (volume multipliers)

## Tuning Knobs

| Knob | Default | Range |
|-------|---------|-------|
| `BGM_FADE_DURATION` | 500ms | 200–1000ms |
| `SFX_DEFAULT_VOLUME` | 0.8 | 0.0–1.0 |
| `VOICE_DEFAULT_VOLUME` | 1.0 | 0.0–1.0 |

## Acceptance Criteria

**AC1: Scene Load Triggers BGM**
- **GIVEN** scene "ch1_scene2" has `sceneMusic: "bmg_ch1_tension"`
- **WHEN** `SceneReady("ch1_scene2")` fires
- **THEN** BGM "bmg_ch1_tension" begins playing

**AC2: Menu Open Pauses Audio**
- **GIVEN** BGM is playing
- **WHEN** NSM enters `MENU_OPEN`
- **THEN** BGM pauses at current position

**AC3: Volume Settings Apply**
- **GIVEN** user sets BGM volume to 0.5 in Settings
- **WHEN** BGM plays
- **THEN** output volume is multiplied by 0.5
