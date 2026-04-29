# Story: S1-5 — Dialogue UI

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: must-have
> **Status**: ready-for-dev
> **Estimate**: 3 days
> **Owner**: ui-programmer
> **Type**: UI
> **ADR**: No ADR applies — Unity UI Toolkit patterns
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Implement the player-facing dialogue interface: speaker name + animated text box, choice selection buttons, and character portrait anchoring.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#) — UI Toolkit (UIToolkit) preferred, UGUI acceptable
- **GDD Ref**: `design/gdd/dialogue-ui.md`
- **Art Bible**: Noto Serif SC font, grey-lavender palette, semi-transparent dialogue box

## UI Layout

```
[Character Portrait — left or right based on speaker]
[Speaker Name Label — bold, Noto Serif SC]
[Dialogue Text — body, animated character-by-character]
[Tap Indicator ▼ — pulses when text complete]

[Choice 1 Button]
[Choice 2 Button]   ← Choice buttons stack vertically
[Choice 3 Button]
```

## Text Animation

- **Speed**: 30ms per character (configurable)
- **Interrupt**: Tap cancels animation, shows full text immediately
- **Completion signal**: Chevron pulses when text fully displayed

## Choice Layout

- Max 6 choices (per design doc)
- Touch target: minimum 60px height
- Focus navigation: swipe left/right
- Selected: tap

## Visual States

| State | Speaker Label | Choice Buttons |
|-------|-------------|---------------|
| TEXT (player) | Hidden | Hidden |
| TEXT (NPC) | Visible, right-anchored | Hidden |
| TEXT (narration) | Hidden | Hidden |
| CHOICE | Visible | Visible, stacked |

## Acceptance Criteria

- [ ] Text animates character-by-character at 30ms/char
- [ ] Tap during animation → cancel, show full text
- [ ] Tap when complete → advance dialogue
- [ ] Choices display with correct text and touch targets
- [ ] Swipe left/right navigates choice focus
- [ ] Portrait anchor: player character → left, NPC → right
- [ ] Narration (speakerId=null) → no speaker label, centered text
- [ ] Dialogue history panel slides in on swipe

## Open Questions

| OQ1 | Support keyboard/gamepad in addition to touch? | UX Designer |
|-----|---------------------------------------------|-------------|
| OQ2 | Choice button icons (shield, etc.)? | Art Director |

## Dependencies

- S1-3 (Branching Dialogue) — receives `DisplayText` and `DisplayChoices` events
- S1-4 (Touch Input) — receives routed tap events

## Test Evidence

Location: `production/qa/evidence/` — manual walkthrough doc with screenshot evidence for text animation, choice layout, portrait anchoring, and history panel
