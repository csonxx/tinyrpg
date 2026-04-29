# Settings System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Meta — player comfort and preference)

## Overview

Settings System stores and manages all player preferences: audio volumes, text display speed, auto-advance behavior, haptic feedback toggle, and language selection. Settings are persisted to device storage (PlayerPrefs on mobile) and loaded on game startup.

Settings values are consumed by other systems at runtime — the Settings System is the single source of truth for player preferences.

## Core Rules

**Rule 1: Settings Keys**
| Key | Type | Default |
|-----|------|---------|
| `volume.music` | float (0.0–1.0) | 0.7 |
| `volume.sfx` | float (0.0–1.0) | 0.8 |
| `volume.voice` | float (0.0–1.0) | 1.0 |
| `text.speed` | enum (slow/medium/fast/auto) | auto |
| `auto.advance` | bool | true |
| `haptic.enabled` | bool | true |
| `language` | string (locale code) | system_locale |

**Rule 2: Persistence**
Settings are saved to `PlayerPrefs` immediately on change and loaded on game startup before the main menu appears.

**Rule 3: Signal Broadcasting**
When any setting changes, the Settings System emits a signal (e.g., `Settings.HapticEnabled = false`) so subscribing systems (Touch Input, Audio Management) can react immediately.

## Dependencies

- Downstream: **Audio Management** (subscribes to volume settings)
- Downstream: **Touch Input System** (subscribes to haptic setting)
- Downstream: **Dialogue UI** (subscribes to text speed and auto-advance)
