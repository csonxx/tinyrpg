# Clue & Intel Collection

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 2 (No Choice Is Perfect)

## Overview

Clue & Intel Collection tracks information the player has discovered — documents, overheard conversations, physical evidence, and verified facts — across the narrative. Clues unlock new dialogue options, reveal hidden choices, and are referenced in later scenes to demonstrate the player's knowledge.

This is the investigation element of the game: paying attention to details pays off. The system is intentionally simple — clues are boolean flags in NSM, not a complex inventory system.

## Player Fantasy

**Indirect.** Players feel it as "I know something they don't" and "I can use this to my advantage." There is no dedicated clue inventory UI — clues surface naturally in dialogue as new options or as referenced in conversation.

## Core Rules

**Rule 1: Clue Registration**
Clues are NSM boolean keys under `clues.{clueId}`. When a clue is discovered (triggered by dialogue node completion, choice selection, or scene inspection), the key is set to `true`.

**Rule 2: Clue Sources**
- **Dialogue discovery**: Reaching a specific dialogue node registers `clues.{clueId}`
- **Choice discovery**: Making a specific choice registers a clue
- **Scene inspection**: Tapping on an interactive object in a scene (via long-press)

**Rule 3: Clue Gating**
CONDITION nodes in the dialogue tree can reference `clues.{clueId}` as part of their expression. If true, locked dialogue options become available.

**Rule 4: Clue Journal (Visual Representation)**
A dedicated "Intelligence Journal" menu item (accessible from pause menu) shows all discovered clues grouped by category: Documents, Conversations, Evidence. Categories match the art bible's clue types. Undiscovered clues are shown as "???" — this is the only UI representation of the clue system.

## Interactions

- Upstream: **Branching Dialogue System** (triggers clue registration on node/choice completion)
- Downstream: **Dialogue UI** (unlocked choices appear based on clue conditions)
- Downstream: **Menu System** (Intelligence Journal accessible from pause menu)

## Formulas

No complex formulas — clues are simple boolean flags.

## Acceptance Criteria

**AC1: Clue Registers on Node Completion**
- **GIVEN** player reaches a dialogue node with `registerClue: "clue_zhang_affair"`
- **WHEN** the node completes
- **THEN** NSM `clues.clue_zhang_affair` is set to `true`

**AC2: Clue Unlocks Dialogue**
- **GIVEN** player has discovered `clue_zhang_affair`
- **WHEN** dialogue reaches a CONDITION node evaluating `clues.clue_zhang_affair == true`
- **THEN** the previously hidden choice "Blackmail ZHANG" becomes available

**AC3: Intelligence Journal Shows Discovered Clues**
- **GIVEN** player has discovered 3 clues
- **WHEN** player opens Intelligence Journal from pause menu
- **THEN** 3 clues are listed with names and discovery moments; remaining clues show as "???"
