# HUD (Trust Meters)

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Pillar 3 (Trust Is the Most Fragile Currency) — the meters are the ever-present reminder of the double life

## Overview

HUD (Trust Meters) is the persistent heads-up display that shows the two trust values at all times during gameplay: Imperial Loyalty ( Dusty Ochre) and Underground Trust ( Muted Jade). The HUD is a persistent overlay — never unloaded during gameplay — visible on every scene, including dialogue scenes. It receives real-time updates from Dual Trust Economy and displays animated feedback for every trust shift.

The HUD must be readable at a glance during intense dialogue moments (so the player can assess risk without breaking immersion) while being unobtrusive enough to never dominate the screen. Per the art bible, the meters use muted, desaturated colors — they should feel like ambient information, not a game UI overlay.

**Owned data:** Current visual state of the meter bars (fill percentage, color, animation phase). **Not owned:** the trust values themselves (Dual Trust Economy owns and calculates them).

## Player Fantasy

**Direct.** The trust meters are the most anxiety-inducing element of the game. Every player should feel a small spike when a trust bar moves — especially when it drops. The meters create a constant background tension: "how close am I to losing control of this situation?"

The player should learn to read the meters the way they read a character's face — quickly, subconsciously, and with emotional weight. A meter in the danger zone (≤25) should feel like a warning, not a number.

## Detailed Design

### Core Rules

**Rule 1: Meter Position and Layout**
Two horizontal bars, positioned in the top-left corner of the screen, stacked vertically:
- Imperial Loyalty bar: top (label + fill bar)
- Underground Trust bar: below it (label + fill bar)
Bar width: 120px on standard screens. Bar height: 8px. Gap between bars: 6px.

Labels use the muted semantic colors: Dusty Ochre (#B8925A) for Imperial, Muted Jade (#5E8B7E) for Underground.

**Rule 2: Fill Animation**
When `DualTrust.Apply(ΔT)` is called:
1. Calculate new trust values
2. Animate bar fill from old percentage to new percentage over 400ms (ease-out)
3. If trust dropped below DANGER_THRESHOLD (25): pulse the bar red with a brief flash animation
4. If trust dropped to CRISIS_THRESHOLD (15): trigger a stronger "critical warning" visual (bar border flashes red 3 times)

**Rule 3: Danger Zone Visualization**
When trust ≤ DANGER_THRESHOLD (25):
- The bar fill color shifts from its normal muted color to a warning amber
- Subtle pulsing glow around the bar (amplitude: 0.3 opacity oscillation, 1s period)

When trust ≤ CRISIS_THRESHOLD (15):
- Bar border pulses red
- Label text color shifts to red-tinted
- This is the "red line" moment — the player has very little room for error

**Rule 4: Passive Decay Indication**
When passive decay is active (trust above 0 and no recent choices), a subtle downward arrow icon (▼) appears next to the bar, animated to indicate slow drain. This makes the passive mechanic visible without being alarming.

**Rule 5: Hysteresis on Display Updates**
Trust values change at most once per 100ms for display purposes (prevents UI flickering during rapid changes). The NSM stores the true value; HUD displays the last-sampled value.

**Rule 6: Never Hidden**
The HUD is never hidden during normal gameplay. Only during specific cinematic moments (cutscenes with no player interaction) may the HUD be obscured. It returns immediately when player control resumes.

### States and Transitions

The HUD itself is always `VISIBLE` during gameplay. Internal visual states per bar:

| State | Description | Trigger |
|-------|-------------|---------|
| `NORMAL` | Bar at full fill, normal color | Default |
| `CHANGING` | Bar animating to new fill level | `TrustValueChanged` event |
| `DANGER` | Bar below DANGER_THRESHOLD (≤25), pulsing amber | Trust drops below 25 |
| `CRISIS` | Bar below CRISIS_THRESHOLD (≤15), flashing red | Trust drops below 15 |
| `DECAY_ACTIVE` | Downward arrow icon visible | Passive decay running |

Transitions are driven by `DualTrust.TrustValueChanged` event carrying the new values.

### Interactions with Other Systems

**← Dual Trust Economy:**
- Receives `TrustValueChanged(imperialValue, undergroundValue)` event
- Receives `TrustShiftApplied(deltaImperial, deltaUnderground)` for shift feedback animation
- Receives `PassiveDecayActive(isActive)` to show/hide decay indicator

**→ Notification System:**
- When trust enters DANGER or CRISIS state, emits a notification event (handled by Notification System)

**→ Touch Input System:**
- HUD does not intercept touch — all taps pass through to the dialogue/scene below

## Formulas

**Formula 1: Bar Fill Percentage**
```
fillPercent = clamp(trustValue / 100.0, 0.0, 1.0)
barPixelWidth = fillPercent * BAR_MAX_PIXELS  (BAR_MAX_PIXELS = 120)
```

**Formula 2: Danger Zone Color Blend**
```
if (trustValue <= CRISIS_THRESHOLD):
    blendFactor = 1.0  (full red)
elif (trustValue <= DANGER_THRESHOLD):
    blendFactor = (DANGER_THRESHOLD - trustValue) / (DANGER_THRESHOLD - CRISIS_THRESHOLD)  (0.0 to 1.0 ramp)
else:
    blendFactor = 0.0  (normal color)
color = lerp(NORMAL_COLOR, DANGER_COLOR, blendFactor)
```

**Formula 3: Danger Pulse Animation**
```
pulseOpacity = 0.3 * (1 + sin(time * 2 * PI / 1.0))  // 1s period oscillation

## Edge Cases

- **If trust value is exactly at threshold (25, 15)**: Treat as in the zone — danger zone starts at ≤25, crisis at ≤15.
- **If trust value changes faster than animation can track**: Queue animations; complete previous animation before starting new one (prevents bar jitter).
- **If both meters enter danger zone simultaneously**: Both pulse independently. Do not add additional visual effects.
- **If trust value goes to 0**: Bar fills to 0. Color goes to full red. This is the game-over condition — handled by Dual Trust Economy (not HUD's job to trigger game over).
- **If HUD is rendered over a cutscene**: HUD should be semi-transparent (opacity 0.3) so it doesn't dominate cinematic scenes.

## Dependencies

- Upstream: **Dual Trust Economy** (sends trust value changes)
- Downstream: **Notification System** (receives danger/crisis notifications)
- Engine: Unity UI Toolkit (UTooL) or UGUI Canvas

## Tuning Knobs

| Knob | Default | Range | Affected Behavior |
|------|---------|-------|-----------------|
| `BAR_MAX_PIXELS` | 120px | 80–160px | Physical width of the meter bar |
| `DANGER_THRESHOLD` | 25 | 15–35 | Trust value below which danger state activates |
| `CRISIS_THRESHOLD` | 15 | 5–20 | Trust value below which crisis state activates |
| `ANIM_DURATION` | 400ms | 200–800ms | Duration of bar fill animation |
| `DANGER_PULSE_OPACITY` | 0.3 | 0.1–0.5 | Amplitude of danger zone pulse glow |
| `HUD_OPACITY_IN_CUTSCENE` | 0.3 | 0.1–0.6 | HUD opacity during cutscenes |

## Acceptance Criteria

**AC1: Trust Shift Animates Correctly**
- **GIVEN** Dual Trust Economy emits `TrustValueChanged(55, 45)` followed by `TrustShiftApplied(-10, +5)`
- **WHEN** the events are processed
- **THEN** Imperial bar animates from current position to 45% fill over 400ms; Underground bar animates from current position to 50% fill

**AC2: Danger Zone Activates at 25**
- **GIVEN** trust value is 26
- **WHEN** `TrustValueChanged(25, X)` is received
- **THEN** the bar enters DANGER state; pulsing amber glow is visible

**AC3: Crisis Zone Activates at 15**
- **GIVEN** trust value is 16
- **WHEN** `TrustValueChanged(15, X)` is received
- **THEN** the bar enters CRISIS state; border flashes red 3 times

**AC4: Passive Decay Indicator**
- **GIVEN** no player choices for 120 seconds
- **WHEN** `PassiveDecayActive(true)` is received
- **THEN** a downward arrow icon appears next to the affected bar

**AC5: HUD Persists Across Scenes**
- **GIVEN** player is in DialogueScene with HUD visible
- **WHEN** Episode Structure loads a new scene
- **THEN** HUD remains visible throughout the transition without flickering off

## Open Questions

| # | Question | Owner |
|---|----------|-------|
| OQ1 | Should the HUD have a subtle "foreshadowing" animation when trust is trending toward danger? | UX Designer |
| OQ2 | Do the trust meters appear during cutscenes? | Game Designer |
