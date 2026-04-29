# Notification System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 3 (Trust Is the Most Fragile Currency) — notifications make trust consequences visible

## Overview

Notification System displays brief toast messages that inform the player of events occurring beneath the surface — trust shift summaries ("Imperial trust decreased"), system messages, and achievement-style feedback. Notifications are transient (auto-dismiss after 2–3 seconds) and non-blocking (they never interrupt gameplay or require player action).

This system is what makes the Dual Trust Economy's invisible mechanics feel visible without cluttering the screen with constant numbers.

## Core Rules

**Rule 1: Notification Types**
- `TRUST_SHIFT`: Shows trust change with directional indicator (▲/▼) and color (Dusty Ochre for Imperial, Muted Jade for Underground)
- `DANGER_ENTER`: "Trust is at risk" — shown when trust drops to or below DANGER_THRESHOLD (25)
- `SECRET_REVEALED`: "Something was discovered" — shown when a clue is registered
- `RELATIONSHIP_CHANGE`: "ZHANG's attitude toward you has changed"

**Rule 2: Display Behavior**
Notifications appear at the top-center of the screen, below the HUD. They slide in from above, hold for 2.5 seconds, and slide out. Stack vertically if multiple fire simultaneously (max 3 visible, older ones dismissed).

**Rule 3: Color Coding**
Trust-related notifications use the muted semantic colors from the art bible palette — not bright neon, but desaturated tones that fit the visual identity.

## Dependencies

- Upstream: **Dual Trust Economy** (emits trust shift events)
- Upstream: **Branching Dialogue System** (emits clue discovery events)
- Upstream: **Relationship Memory** (emits relationship change events)

## Tuning Knobs

| Knob | Default | Range |
|-------|---------|-------|
| `NOTIFICATION_DURATION` | 2500ms | 1500–4000ms |
| `MAX_VISIBLE` | 3 | 1–5 |
| `POSITION_Y` | 120px from top | 80–200px |

## Acceptance Criteria

**AC1: Trust Shift Notification Fires**
- **GIVEN** player makes a choice that shifts imperial trust by -5
- **WHEN** the choice is processed
- **THEN** a notification appears: "Imperial trust ▼ -5"

**AC2: Danger Zone Notification**
- **GIVEN** imperial trust drops from 26 to 25
- **WHEN** the shift is applied
- **THEN** a notification appears: "Imperial trust is at risk" in amber color
