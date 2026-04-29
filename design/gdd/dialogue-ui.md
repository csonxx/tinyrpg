# Dialogue UI

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 1 (Identity Is a Cage) — UI is the window into the double life; trust meters are ever-present reminders

## Overview

Dialogue UI renders the player-facing dialogue interface: the dialogue box with speaker name and animated text, character portraits, choice selection buttons, and the dialogue history panel. It receives display events from the Branching Dialogue System and translates them into visual output.

The UI follows the art bible's visual language (Noto Serif SC typography, grey-lavender palette, hand-painted feel) and must be readable on small mobile screens at arm's length. All text is rendered via the localization system.

**Owned data:** Current UI animation state, choice focus index, history panel visibility. **Not owned:** the text content (comes from Branching Dialogue System), the trust values (owned by Dual Trust Economy).

## Player Fantasy

**Direct.** The dialogue UI is the primary interface the player looks at for the entire game. It should feel like reading an intimate letter, not filling out a form. Text should appear with a gentle reveal (not instant pop-in), choices should feel weighty and considered, and the character's portrait should be present without being distracting.

The emotional tone per the art bible: desaturated, period-appropriate, a little cold. The UI should never feel gamey — no bright colors, no chunky buttons. It should feel like you're reading something from another era.

## Detailed Design

### Core Rules

**Rule 1: Dialogue Box Layout**
The dialogue box is a semi-transparent overlay at the bottom ~35% of the screen (DialogueScene). It contains:
- Speaker name label (top-left of box, bold, Noto Serif SC)
- Dialogue text area (body, Noto Serif SC, 28-32sp)
- Tap indicator (bottom-right, animated chevron when text is complete)

Positioning: player character open → box anchored bottom-left; NPC open → box anchored bottom-right; narration → box anchored bottom-center, name label hidden.

**Rule 2: Text Animation**
Text appears character-by-character at `CHAR_DISPLAY_TIME` rate (30ms/char default). The animation can be interrupted by tap. When complete, a subtle animated chevron (▼) pulses in the bottom-right corner as a "tap to continue" signal.

**Rule 3: Choice Button Layout**
Choice buttons are stacked vertically in the lower portion of the screen (above or overlapping the dialogue box). Maximum 6 choices. Each choice button:
- Background: semi-transparent dark with bone-white border
- Text: Noto Serif SC, centered, left-aligned text
- Focus state: slightly larger scale (1.05x), highlighted border
- Touch target: minimum 60px height for mobile

**Rule 4: Character Portrait**
Character portraits appear above the dialogue box, to the side corresponding to the speaker (player left, NPC right). Portrait uses the character's default `portraitAsset`. Emotion variant portraits are swapped via `PortraitSwap(characterId, emotionName)` signal.

Portrait fade-in: 200ms ease-in-out opacity on scene entry. No hard pop.

**Rule 5: Dialogue History Panel**
Swipe left or right during `DIALOGUE_ACTIVE` opens the history panel (slides in from the right, covering ~70% of screen). It shows the last 20 dialogue entries: `{ speakerName, textPreview, timestamp }`. Tap outside or tap X to dismiss.

**Rule 6: Choice Navigation**
During `CHOICE_ACTIVE`:
- Swipe left → focus moves to previous choice (wraps from first to last)
- Swipe right → focus moves to next choice (wraps from last to first)
- Tap → selects focused choice

### States and Transitions

| State | Description | Valid Transitions |
|-------|-------------|-----------------|
| `HIDDEN` | No dialogue UI visible | → `SHOWING` (on `DisplayText` event) |
| `SHOWING_TEXT` | Text animating or waiting for tap | → `SHOWING_CHOICES` (on CHOICE node), → `HIDDEN` (on scene transition) |
| `SHOWING_CHOICES` | Choice buttons displayed | → `SHOWING_TEXT` (on choice made) |
| `HISTORY_OPEN` | History panel overlaid | → `SHOWING_TEXT` (on dismiss) |

### Interactions with Other Systems

**← Branching Dialogue System:**
- Receives `DisplayText(speakerId, content)` event
- Receives `DisplayChoices(choices[])` event
- Receives `DialogueSceneComplete(sceneId)` → triggers fade-out

**→ Touch Input System:**
- Sends `ChoiceSelected(choiceIndex)` via Touch Input (not direct)
- Sends `AdvanceDialogue()` via tap routing

**→ Character System (via Chapter Content Data):**
- Requests `portraitAsset` for a given `characterId`
- Receives `PortraitSwap(characterId, emotion)` for emotion variants

**← Narrative State Machine:**
- Receives `StateFullyLoaded` → restores UI state from NSM if resuming

## Formulas

**Formula 1: Dialogue Box Vertical Position**
```
dialogueBoxY = screenHeight * 0.65  (anchored to bottom of screen)
dialogueBoxHeight = screenHeight * 0.35
```

**Formula 2: Choice Button Spacing**
```
choiceSpacing = 12px
choiceButtonHeight = 60px minimum
totalChoicesHeight = (choiceButtonHeight * numChoices) + (choiceSpacing * (numChoices - 1))
centerOffsetY = (screenHeight * 0.35 - totalChoicesHeight) / 2
```

**Formula 3: Text Size Scaling**
```
textSize = clamp(28sp - (screenWidth < 720px ? 4 : 0), 20sp, 32sp)
```
Smaller screens get slightly smaller text to fit content.

## Edge Cases

- **If speakerId is null (narration)**: Hide speaker name label, center dialogue text horizontally.
- **If choice text is very long (> 2 lines)**: Truncate with ellipsis after 2 lines. Full text shown in history panel.
- **If character portrait asset is missing**: Show silhouette placeholder (dark shape with no features) rather than crashing.
- **If text animation is interrupted by scene transition**: Immediately show full text, then fade out as scene transition begins.
- **If history panel is open and `DialogueSceneComplete` fires**: Close history panel before processing scene transition.
- **If rapid taps fire during text animation**: Handled by Touch Input System (first tap cancels animation, second advances).

## Dependencies

- Upstream: **Branching Dialogue System** (sends display events)
- Upstream: **Chapter Content Data** (provides character portraits and localization)
- Upstream: **Narrative State Machine** (provides state for resume)
- Downstream: **Touch Input System** (receives routed tap events)

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `TEXT_SIZE_BODY` | 28sp | 20–36sp | Main dialogue text size |
| `TEXT_SIZE_SPEAKER` | 22sp | 16–28sp | Speaker name label size |
| `CHOICE_BUTTON_HEIGHT` | 60px | 44–80px | Minimum touch target height |
| `PORTRAIT_ANIM_DURATION` | 200ms | 100–400ms | Portrait fade-in time |
| `DIALOGUE_BOX_OPACITY` | 0.85 | 0.6–1.0 | Background opacity of dialogue box |

## Acceptance Criteria

**AC1: Text Animates Character by Character**
- **GIVEN** `DisplayText("ZHANG", "今晚的会议...")` is received
- **WHEN** the UI renders
- **THEN** text appears one character at a time at ~30ms/char rate; chevron indicator pulses when complete

**AC2: Choices Display and Navigate**
- **GIVEN** `DisplayChoices([{text:"同意"}, {text:"拒绝"}, {text:"犹豫"}])` is received
- **WHEN** the UI renders
- **THEN** 3 choice buttons appear stacked vertically; swipe left/right navigates focus; focused choice has highlighted border

**AC3: Portrait Fades In**
- **GIVEN** a scene with character "ZHANG" speaking begins
- **WHEN** `DisplayText` with speakerId="ZHANG" is received
- **THEN** ZHANG's portrait fades in over 200ms without popping

**AC4: History Panel Opens and Closes**
- **GIVEN** player is in `SHOWING_TEXT` state
- **WHEN** player swipes left
- **THEN** history panel slides in from the right showing recent dialogue entries
- **WHEN** player taps outside the panel
- **THEN** history panel dismisses

**AC5: Narration Mode Hides Speaker Name**
- **GIVEN** `DisplayText(null, "夜幕降临...")` is received
- **THEN** speaker name label is hidden; dialogue text is centered horizontally

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Do we support keyboard/gamepad input for choice navigation in addition to touch? | UX Designer |
| OQ2 | Should choice buttons have subtle icons (e.g., a shield for defensive choices)? | Art Director |
