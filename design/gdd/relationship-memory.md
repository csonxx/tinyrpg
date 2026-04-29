# Relationship Memory

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 1 (Identity Is a Cage), Pillar 2 (No Choice Is Perfect)

## Overview

Relationship Memory tracks how each character in the game feels about the player over time — not just the two trust factions, but individual NPCs. It stores a `relationshipValue` per character (0–100), records key interaction moments (whether the player was honest, kind, cold, etc.), and surfaces this data to the Dialogue System so characters react appropriately based on past encounters.

This system is what makes the game feel like characters remember you — returning to a character weeks later should feel different based on how you treated them.

## Player Fantasy

**Indirect.** Players feel it as "that character seems to trust me" or "she's been cold since that incident." The UI never explicitly shows a relationship meter — it manifests in dialogue tone, available conversation topics, and whether characters reveal information freely.

## Core Rules

**Rule 1: Relationship Values**
Each character has a `relationshipValue` (0–100) stored in NSM under `relationships.{characterId}`. Starting value is 50 (neutral).

**Rule 2: Relationship Shifts**
Choices that affect a specific character (not just faction trust) trigger a relationship shift. Each choice in Chapter Content Data can carry an optional `relationshipShift: { characterId: delta }`. Multiple characters can be affected by one choice.

**Rule 3: Memory Flags**
Beyond numeric value, specific past choices set `memoryFlags` per character: `[{ characterId }.{ flagName }]`. Examples: `ZHANG.sawThrough_lie`, `LIU.kept_secret`, `FENG.refused_order`. These are boolean NSM keys that unlock conditional dialogue.

**Rule 4: Passive Decay**
Relationships decay at `RELATIONSHIP_DECAY_RATE` per 60 seconds of dialogue after `RELATIONSHIP_DECAY_GRACE_PERIOD` (120 seconds of no interaction). Decay is slow — meaningful interactions create lasting impressions; minor interactions fade.

## Interactions

- Upstream: **Branching Dialogue System** (applies relationship shifts on choices)
- Downstream: **Dialogue UI** (character tone/portrait expression may reflect relationship)
- Downstream: **Branching Dialogue System** (condition expressions check relationship values)

## Formulas

```
newRelationship = clamp(oldRelationship + delta, 0, 100)
decayPerTick = RELATIONSHIP_DECAY_RATE * (secondsSinceLastInteraction / 60)
```

## Acceptance Criteria

**AC1: Relationship Shifts Accumulate**
- **GIVEN** ZHANG's relationship is 60
- **WHEN** player makes a choice with `relationshipShift: { ZHANG: +10 }`
- **THEN** NSM `relationships.ZHANG` becomes 70

**AC2: Memory Flags Persist**
- **GIVEN** player lied to ZHANG in chapter 1
- **WHEN** dialogue with ZHANG resumes in chapter 3
- **THEN** condition `relationships.ZHANG.sawThrough_lie == true` gates dialogue options

**AC3: Relationship Decay Applies**
- **GIVEN** player has not interacted with FENG for 300 seconds
- **WHEN** the decay tick fires (every 60s)
- **THEN** FENG's relationship decays by `RELATIONSHIP_DECAY_RATE * 5`
