# Accessibility System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: (Meta — ensures the game is playable by the widest possible audience)

## Overview

Accessibility System provides accommodations for players with different needs: text size scaling, colorblind modes, reduce-motion option, and screen reader support hooks. These settings live in Settings System and are consumed by rendering and UI systems.

This is a vertical slice system — the core game is playable without accessibility features, but they must be supported from the start to avoid retrofitting.

## Core Rules

**Rule 1: Text Size Scaling**
Multiplier applied to all UI text sizes: 1.0x (default), 1.25x, 1.5x, 2.0x. Stored as `accessibility.textScale` in Settings.

**Rule 2: Colorblind Modes**
Supported modes: None, Protanopia (red-green), Deuteranopia, Tritanopia. Adjusts palette mapping for trust meters and UI elements. Stored as `accessibility.colorblindMode`.

**Rule 3: Reduce Motion**
When enabled: disables all non-essential animations (text reveal remains, transition animations use instant cuts). Stored as `accessibility.reduceMotion`. This also disables particle effects.

**Rule 4: Screen Reader Support**
All UI elements carry accessibility labels (`AccessibilityLabel` component in Unity UI). The system exposes a text description of the current screen to the platform's screen reader API when requested.

## Dependencies

- Downstream: **Dialogue UI** (text scaling, colorblind palette swap)
- Downstream: **HUD Trust Meters** (colorblind palette swap)
- Downstream: **Scene Management** (reduce motion affects transitions)
- Downstream: **Menu System** (screen reader labels on all menu items)
