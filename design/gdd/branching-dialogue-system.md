# Branching Dialogue System

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 1 (Identity Is a Cage), Pillar 2 (No Choice Is Perfect)

## Overview

Branching Dialogue System traverses a dialogue tree defined in Chapter Content Data, managing which dialogue node to display, processing player choice input, calculating trust consequences via the Dual Trust Economy, and updating the Narrative State Machine with progress. It is the central engine of the game's core loop — every conversation, interrogation, and relationship moment flows through this system.

The dialogue tree is a directed graph of nodes. Each node is one of: `TEXT` (speaker narration/line), `CHOICE` (branching decision point), `CONDITION` (branch gate), or `END` (scene conclusion). Choices are not cosmetic — each one carries measurable consequences recorded in NSM and fed to Dual Trust Economy.

**Owned data:** Dialogue tree cursor position, choice history log, per-scene visited-node set. **Not owned:** the trust values themselves (Dual Trust Economy owns those), the narrative content (Chapter Content Data owns that).

## Player Fantasy

**Direct.** Every choice the player makes in dialogue should feel consequential — not just as a narrative beat but as a strategic decision with trade-offs. The system must make the player hesitate before choosing, wonder "did I pick the right one?", and later discover the consequences in changed relationships and narrative paths.

The fantasy is not information mastery ("I found the optimal path") but moral weight — each choice in this world of grey morality costs something. The dialogue UI should never feel like a game menu; it should feel like a conversation you're afraid to get wrong.

Reference: The player should have the same feeling as in The Invisible Guardian — where choosing to lie to someone's face produces a visible, lasting consequence in how characters treat you later.

## Detailed Design

### Core Rules

**Rule 1: Dialogue Tree Structure**
A dialogue tree is a directed graph stored in Chapter Content Data. Each node has:
- `nodeId`: unique identifier within the scene
- `type`: `TEXT` | `CHOICE` | `CONDITION` | `END`
- `speakerId`: which character is speaking (null for narration)
- `content`: the dialogue/lore text
- `choices`: array of `{ text, nextNodeId, trustShift: { imperial, underground }, condition? }` (for CHOICE nodes)
- `conditionExpr`: boolean expression referencing NSM keys (for CONDITION nodes)
- `nextNodeId`: default next node (for TEXT/END nodes)

**Rule 2: Dialogue Cursor**
The system maintains a `dialogueCursor: { sceneId, nodeId }` in NSM (key: `dialogue.cursor`). This is the single source of truth for "where am I in the dialogue tree". All navigation advances or branches from this cursor.

**Rule 3: Choice Processing**
When a CHOICE node is reached:
1. Display all choices to the player via Dialogue UI
2. Wait for Touch Input System's `ChoiceSelected(choiceIndex)` signal
3. Apply `trustShift` of selected choice via `DualTrust.Apply(choice.trustShift)`
4. Log choice to `dialogue.choiceHistory[]` in NSM: `{ nodeId, choiceIndex, choiceText, trustShift, timestamp }`
5. Advance cursor to `nextNodeId` of selected choice
6. Emit `DialogueNodeChanged(nodeId)` for UI

**Rule 4: Conditional Branching**
CONDITION nodes evaluate `conditionExpr` against NSM state:
- If true → advance to `trueNextNodeId`
- If false → advance to `falseNextNodeId`
Evaluation is synchronous and deterministic. No player input is involved.

**Rule 5: Text Node Auto-Advance**
TEXT nodes auto-advance after `TEXT_DISPLAY_DURATION` (tuned via `AUTO_ADVANCE_DELAY`). Player tap interrupts auto-advance and immediately moves to next node. Once text is fully displayed (text animation complete), auto-advance timer starts.

**Rule 6: Scene Completion**
When an END node is reached:
1. Emit `DialogueSceneComplete(sceneId)` to NSM
2. NSM transitions to `SCENE_ACTIVE` state
3. Episode Structure is notified to load next scene

### States and Transitions

| State | Description | Valid Transitions |
|-------|-------------|-----------------|
| `INACTIVE` | No dialogue loaded | → `LOADING` (on `StartDialogue(sceneId)`) |
| `LOADING` | Dialogue tree being fetched from Chapter Content Data | → `DISPLAYING_TEXT` (on tree loaded) |
| `DISPLAYING_TEXT` | Text node animating or waiting for tap | → `AWAITING_CHOICE` (on CHOICE node), → `AWAITING_TAP` (on TEXT node after auto-advance timeout), → `COMPLETE` (on END node) |
| `AWAITING_CHOICE` | Waiting for player choice input | → `PROCESSING_CHOICE` (on `ChoiceSelected`) |
| `AWAITING_TAP` | Waiting for player tap to advance | → `DISPLAYING_TEXT` (on tap) |
| `PROCESSING_CHOICE` | Applying trust shift and updating NSM | → `DISPLAYING_TEXT` |
| `COMPLETE` | Scene finished | → `INACTIVE` (on `EndDialogue`) |

### Interactions with Other Systems

**← Touch Input System:**
- Touch Input System sends `ChoiceSelected(choiceIndex)` during `CHOICE_ACTIVE` context
- Touch Input System sends `AdvanceDialogue()` during `DIALOGUE_ACTIVE` context

**← Narrative State Machine:**
- NSM stores `dialogue.cursor` (current scene/node), `dialogue.choiceHistory[]`
- NSM emits `StateFullyLoaded` → Branch Dialogue resumes from saved cursor
- NSM provides `conditionExpr` evaluation context for CONDITION nodes

**→ Dual Trust Economy:**
- On choice selection: calls `DualTrust.Apply(trustShift)` with the choice's delta values
- No return value — trust is owned by Dual Trust Economy

**→ Chapter Content Data:**
- Requests dialogue tree for a given `sceneId`
- Returns full node graph for the scene

**→ Dialogue UI:**
- Receives `DisplayText(speakerId, content)` events
- Receives `DisplayChoices(choices[])` events
- Receives `DialogueSceneComplete(sceneId)` for transition orchestration

**→ Episode Structure:**
- Receives `DialogueSceneComplete(sceneId)` to trigger next scene load

## Formulas

**Formula 1: Choice Count Presentation**
The number of choices presented at a CHOICE node is `choices.length`. Typical range: 2–4 choices. Maximum hard-coded limit: 6 (beyond which choice UI becomes unreadable on mobile).

**Formula 2: Trust Shift Application**
When a choice with `trustShift = { imperial: ΔI, underground: ΔU }` is selected:
```
DualTrust.Apply({ imperial: clamp(ΔI, -10, +10), underground: clamp(ΔU, -10, +10) })
```
Values are clamped to ±10 per choice to prevent single-choice catastrophic swings.

**Formula 3: Auto-Advance Delay**
```
autoAdvanceTime = textDisplayDuration + (charCount * CHAR_DISPLAY_TIME)
```
Where `textDisplayDuration = 500ms` (minimum display time regardless of text length) and `CHAR_DISPLAY_TIME = 30ms/char` (animated text reveal rate).

## Edge Cases

- **If a CHOICE node has zero choices**: This is a data error. Log warning, emit `DialogueSceneComplete(sceneId)` as if END node was reached.
- **If `nextNodeId` points to a non-existent node**: Log error, emit `DialogueSceneComplete(sceneId)` to prevent infinite loop.
- **If trust shift is missing from a choice (null)**: Treat as `{ imperial: 0, underground: 0 }` — no trust change.
- **If condition expression references a non-existent NSM key**: Treat as `false` — do not allow through. Log warning for data correction.
- **If player rapidly taps during text animation**: First tap: cancel animation, show full text. Second tap (within 300ms): advance to next node. (Handled by Touch Input System.)
- **If scene is loaded but NSM has no cursor**: Initialize cursor to the scene's `startNodeId` from Chapter Content Data.
- **If dialogue is resumed from save with no saved cursor**: Initialize to start of scene.
- **If choice count exceeds 6**: Cap at 6 in code; log warning for content correction.

## Dependencies

- Upstream: **Narrative State Machine** (stores dialogue cursor, choice history, provides condition evaluation)
- Upstream: **Touch Input System** (sends choice and advance signals)
- Upstream: **Chapter Content Data** (provides dialogue tree data)
- Downstream: **Dual Trust Economy** (receives trust shift calls)
- Downstream: **Dialogue UI** (receives display events)
- Downstream: **Episode Structure** (receives scene complete events)

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `MAX_CHOICES` | 6 | 2–6 | Maximum choices per CHOICE node |
| `AUTO_ADVANCE_DELAY` | 500ms | 200–2000ms | Minimum time before auto-advance |
| `CHAR_DISPLAY_TIME` | 30ms/char | 10–60ms/char | Text animation speed |
| `TRUST_SHIFT_CAP` | 10 | 5–20 | Maximum trust delta per single choice |
| `RAPID_TAP_THRESHOLD` | 300ms | 200–500ms | Second tap within this window = skip to next node |

## Acceptance Criteria

**AC1: Choice Triggers Trust Shift**
- **GIVEN** player is at a CHOICE node with 3 options; option 2 has `{ imperial: +5, underground: -3 }`
- **WHEN** player selects option 2
- **THEN** `DualTrust.Apply({ imperial: +5, underground: -3 })` is called; NSM `dialogue.choiceHistory` records the choice

**AC2: Condition Gate Branches Correctly**
- **GIVEN** NSM has `trust.imperial = 60`
- **WHEN** dialogue reaches a CONDITION node evaluating `trust.imperial >= 50`
- **THEN** cursor advances to `trueNextNodeId`

**AC3: Scene Completes on END Node**
- **GIVEN** dialogue cursor reaches an END node
- **WHEN** the node is processed
- **THEN** `DialogueSceneComplete(sceneId)` is emitted; NSM state transitions to `SCENE_ACTIVE`

**AC4: Dialogue Resumes After Save Load**
- **GIVEN** player saved mid-dialogue at node "ch1_scene3_node7"
- **WHEN** player loads the save
- **THEN** dialogue resumes at "ch1_scene3_node7" with the same choice history

**AC5: Text Auto-Advances**
- **GIVEN** player is at a TEXT node with 200 characters and has not tapped
- **WHEN** `AUTO_ADVANCE_DELAY` (500ms + 200×30ms = 6.5s) elapses
- **THEN** cursor advances to the next node automatically

**AC6: Tap Interrupts Text Animation**
- **GIVEN** player is at a TEXT node with text still animating
- **WHEN** player taps
- **THEN** text animation cancels, full text is shown, auto-advance timer resets

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should choices ever be hidden until certain NSM conditions are met (secret choices)? | Game Designer |
| OQ2 | Do we support a "rewind" mechanic that lets players replay a choice? (This would require branching structure to be a DAG, not a tree.) | Game Designer |
| OQ3 | Should the dialogue system support voice-over audio cues? | Audio Director |
