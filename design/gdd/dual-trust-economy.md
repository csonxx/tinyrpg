# Dual Trust Economy

> **Status**: Designed
> **Author**: Claude Code (design-system)
> **Last Updated**: 2026-04-29
> **Implements Pillar**: Trust Is the Most Fragile Currency

## Overview

The Dual Trust Economy is the central mechanical hook of 雾中誓言 — the system that makes this game unlike other narrative RPGs. The player simultaneously maintains two trust meters: **Imperial Loyalty** (how much the Japanese occupation authorities and their collaborator network trust the protagonist) and **Underground Trust** (how much the CCP resistance network trusts the protagonist). Both start at a moderate value (~40–50) and shift with every meaningful choice.

**The core tension:** Progress with one side makes the other harder. Helping the resistance earns underground trust but requires actions that compromise the protagonist's cover with the occupation. Succeeding as a collaborator builds imperial trust but requires betraying the people and values the protagonist secretly serves. The system is zero-sum in structure but not strictly zero-sum in practice — sometimes both can fall.

**What this system owns:**
- Trust shift amounts per dialogue choice (defined in Chapter Content Data, consumed by this system)
- Decay over time when no choices are made (very slow passive decay, keeps both meters from stagnating)
- Danger zone detection (when either meter crosses below `TRUST_DANGER_THRESHOLD`)
- Crisis detection (when either meter hits 0, or when underground trust hits critical low and the underground organization cuts contact)
- Special state: **Parity Crisis** (when both meters are within 10 points of each other, both in danger zone — creates unique narrative flag)

**What this system does NOT own:**
- UI display of the meters (that's HUD's job)
- Narrative consequences of crisis states (that's Branching Dialogue System's job — this system emits events, Branching Dialogue consumes them)
- Trust change amounts for specific choices (those are authored in Chapter Content Data, this system applies them)

## Player Fantasy

**Direct.** The player sees the trust meters constantly. They are a source of persistent low-grade anxiety — not because they flash red constantly, but because the player knows that every choice shifts one or both of them, and every shift has permanent consequences.

**What the player feels:**
- **Tension at all times** — the meters are always visible, always ticking in the back of the player's mind during every dialogue. There is no "safe" moment where neither meter matters.
- **Cost of success** — when a choice boosts underground trust, the player feels the cost: they had to perform betrayal of someone, or reveal something, or compromise their cover slightly. Trust gains feel like victories with blood on them.
- **Danger awareness** — when a meter enters the danger zone, the player feels urgency. Not panic, but the cold knowledge that one more wrong choice in this state ends everything.
- **Impossibility of full safety** — there is no state where both meters are "comfortable." Even at optimal balance, the player knows the floor could collapse.

**Pillar connection:**
- Pillar 3 (Trust Is the Most Fragile Currency): The meters make trust visible and numeric. Every point lost in underground trust is a point that took a full chapter to build back. The numbers make the fragility visceral, not abstract.
- Pillar 1 (Identity Is a Cage): The dual meters physically embody the cage. The player cannot maximize both. Every choice is a negotiation between two incompatible versions of self-presentation.

## Detailed Design

### Core Rules

**Rule 1: Dual Tracking**
At all times, the game tracks two independent trust values in NSM:
- `trust.imperial` (float, 0–100)
- `trust.underground` (float, 0–100)

Both start at `40` at game start (Chapter 1, before the game's opening crisis). Neither starts at 50 — the asymmetry of starting trust levels can be tuned per-chapter.

**Rule 2: Choice-Driven Trust Shifts**
Every meaningful dialogue choice in Chapter Content Data specifies:
- `trustShiftImperial` (float, typically –20 to +15)
- `trustShiftUnderground` (float, typically –15 to +20)
- `isSecret` (bool) — if true, the trust shift is not shown in the UI and the player does not see which meter shifted. The NSM event is still emitted.

**Symmetry rule:** Choices that benefit one side must cost the other, BUT the cost/benefit is not always equal. Some choices sacrifice a large amount of underground trust for a small gain in imperial trust. The asymmetry IS the design.

**Rule 3: Passive Decay**
Each meter decays by `DECAY_AMOUNT` per `DECAY_INTERVAL` of real time (not game time) when no choices have been made in the last `DECAY_GRACE_PERIOD`:

`decay active = (currentTime - lastChoiceTime) > DECAY_GRACE_PERIOD`

Decay is applied as: `T = max(T - DECAY_AMOUNT, 0)` — never goes negative from decay alone.

DECAY_AMOUNT = 0.5 points
DECAY_INTERVAL = 30 seconds
DECAY_GRACE_PERIOD = 120 seconds

Purpose: prevents meters from stagnating when players pause to think. Very slow — players should not notice decay during active dialogue. Designed to affect players who leave the game running and return.

**Rule 4: Danger Zone**
Each meter has a danger threshold (`TRUST_DANGER_THRESHOLD = 25`). When a meter crosses below this threshold:
1. The meter emits `TrustBoundaryReached(meter, newValue)` event (handled by NSM automatically)
2. HUD receives `StateChanged` and enters danger pulse mode
3. Notification System may display a warning (see Pillar 3 design test — warning must not be too explicit or it defeats the ambiguity)
4. Narrative state `state.dangerMode.{meter}` is set to `true`

**Rule 5: Critical Zone (Underground Trust only)**
When `trust.underground < 15`:
- The underground organization initiates a **trust verification protocol** — a special narrative sequence where the protagonist must prove their loyalty through a high-stakes task
- If `trust.underground = 0`: the underground cuts contact. The protagonist loses access to underground contacts until `trust.underground > 20` through specific redemption choices. This is a **game state change** — not a game over, but a structural shift in available options.

**Rule 6: Imperial Trust Crisis**
When `trust.imperial < 15`:
- The protagonist is placed under investigation. New dialogue options become available that reflect the increased scrutiny.
- If `trust.imperial = 0`: the protagonist is arrested. This is a failure ending.

**Rule 7: Parity Crisis**
When BOTH meters are within 10 points of each other AND both are below 40:
- Special narrative flag `state.parityCrisis = true` is set
- This triggers a unique scene type: the protagonist must navigate a situation where both sides are suspicious simultaneously
- Pillar 1 design test in action: the cage tightens when both identities are equally compromised

### States and Transitions

The Dual Trust Economy does not have its own state machine — it reacts to events. It has two internal tracking variables that define its reactive behavior:

| Internal State | Entry Condition | Exit Condition | UI Behavior |
|---|---|---|---|
| **Normal** | Both meters > 25 | Either meter ≤ 25 | Standard meter display |
| **Imperial Danger** | `trust.imperial ≤ 25` | `trust.imperial > 30` | Imperial meter pulses (diagonal shear animation) |
| **Underground Danger** | `trust.underground ≤ 25` | `trust.underground > 30` | Underground meter pulses |
| **Dual Danger** | Both meters ≤ 25 simultaneously | Either meter > 30 | Both meters pulse, background atmosphere shifts colder |
| **Imperial Crisis** | `trust.imperial < 15` | `trust.imperial ≥ 20` | Imperial meter rapid pulse, investigation UI overlay |
| **Underground Crisis** | `trust.underground < 15` | `trust.underground ≥ 20` | Underground meter rapid pulse, underground contact scene triggers |
| **Imperial Arrest** | `trust.imperial = 0` | N/A (failure ending) | Game over screen |
| **Underground Cut** | `trust.underground = 0` | `trust.underground > 20` (redemption arc) | Underground contacts greyed out, investigation arc begins |

### Interactions with Other Systems

**→ Narrative State Machine (NSM):**
- Reads `trust.imperial`, `trust.underground` via `NSM.Get()`
- Calls `NSM.Mutate("trust.imperial", newValue)` and `NSM.Mutate("trust.underground", newValue)` when choices are made
- Subscribes to `StateChanged("trust.*")` to detect external trust modifications (e.g., save-loaded with different values)

**→ HUD (Trust Meters):**
- Receives `StateChanged("trust.*")` events and updates display
- Receives danger/crisis state transitions to trigger appropriate visual modes
- Contract: HUD displays the current value and state; it does not own the trust logic

**→ Branching Dialogue System:**
- Reads current trust values to determine which dialogue options are available
- Reads danger/crisis states to inject crisis-specific dialogue nodes
- Reads `state.parityCrisis` to gate the parity crisis scene
- Calls `Mutate()` for trust shifts defined in Chapter Content Data
- Contract: Dialogue System is the consumer of trust state; Trust Economy is the authority on trust rules

**→ Episode Structure:**
- Reads `trust.imperial` and `trust.underground` at chapter start to set initial values
- May call `Mutate()` for chapter-specific trust events (e.g., a scene where the protagonist earns a formal imperial commendation)
- Subscribes to crisis state transitions to load appropriate crisis scenes
- Contract: Episode Structure sequences the narrative; Trust Economy determines which narrative paths are open

**→ Notification System:**
- Subscribes to `StateChanged("trust.*")` to display trust shift feedback
- Shift amounts are displayed as a toast: "+15" in jade color or "–10" in ochre color, briefly (800ms), non-blocking
- Does not show secret trust shifts (`isSecret = true`)
- Contract: Notification System formats and displays feedback; Trust Economy defines what feedback to show

## Formulas

**Formula 1: Trust Application**

`T_result = clamp(T_current + ΔT, 0.0, 100.0)`

After every dialogue choice, the system applies the authored `ΔT` values:

```
T_imperial = clamp(T_imperial + ΔT_imperial, 0.0, 100.0)
T_underground = clamp(T_underground + ΔT_underground, 0.0, 100.0)
NSM.Mutate("trust.imperial", T_imperial)
NSM.Mutate("trust.underground", T_underground)
```

**Typical ΔT ranges:**

| Choice Type | ΔT Imperial | ΔT Underground | Notes |
|---|---|---|---|
| Imperial performance (public) | +5 to +15 | –5 to –15 | Most common; gains imperial trust cost underground |
| Underground loyalty (private) | –5 to –15 | +5 to +15 | Mirror of above |
| Betray underground contact | –10 to –20 | –15 to –30 | Major cost; only available in crisis states |
| Protect resistance asset | –5 to –10 | +10 to +20 | High underground gain; moderate imperial risk |
| Refuse imperial order | +0 to –5 | +5 to +10 | Imperial loyalty holds (you're defying orders, not exposing identity) |
| Complete imperial mission | +10 to +20 | –5 to –10 | High imperial gain; underground cost is relatively small |
| Secret resistance action | +0 | +5 to +15 | No imperial cost (action was covert); underground gains |

---

**Formula 2: Passive Decay**

```
elapsed = currentTime - lastChoiceTime
if (elapsed > DECAY_GRACE_PERIOD):
    ticks = floor((elapsed - DECAY_GRACE_PERIOD) / DECAY_INTERVAL)
    T_imperial = max(T_imperial - (ticks * DECAY_AMOUNT), 0.0)
    T_underground = max(T_underground - (ticks * DECAY_AMOUNT), 0.0)
    if ticks > 0: emit StateChanged events
```

DECAY_AMOUNT = 0.5 per tick
DECAY_INTERVAL = 30 seconds
DECAY_GRACE_PERIOD = 120 seconds (no decay in first 2 minutes after last choice)

---

**Formula 3: Danger Threshold Check**

```
isImperialDanger = T_imperial <= TRUST_DANGER_THRESHOLD
isUndergroundDanger = T_underground <= TRUST_DANGER_THRESHOLD
isParityCrisis = abs(T_imperial - T_underground) <= 10.0 AND T_imperial < 40 AND T_underground < 40
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| TRUST_DANGER_THRESHOLD | D | float | fixed = 25.0 | Below this: danger state |
| TRUST_CRISIS_THRESHOLD | C | float | fixed = 15.0 | Below this: crisis state (investigation/cut contact) |
| Parity distance | P | float | [0, 100] | `abs(T_imperial - T_underground)` |
| Parity crisis threshold | P_c | float | fixed = 10.0 | `P <= 10 AND both < 40` |

---

**Formula 4: Redemption Recovery Rate**

After an underground cut contact (`T_underground = 0`), redemption choices restore trust at 50% of normal rate:

`T_underground_redemption = clamp(T_underground + (ΔT * 0.5), 0.0, 100.0)`

This reflects the underground's suspicion — even when the protagonist proves themselves, trust rebuilds more slowly than it was lost. Recovery from 0 to 20 takes significantly longer than a normal loss of 20 points.

## Edge Cases

**EC1: Both Meters Hit 0 Simultaneously**
- **If** both `trust.imperial = 0` and `trust.underground = 0` in the same choice (requires a specifically authored "betray everything" choice)
- **Then** the imperial arrest ending takes priority. The underground cut contact is moot because the protagonist is already imprisoned. This is a special failure ending: "Exposed on All Fronts."

**EC2: Recovery Above Crisis Threshold**
- **If** `T_underground` is at 0 (cut contact) and a redemption choice would push it to, e.g., 22
- **Then** the value is set to `22` — above the crisis threshold of 20, contact is restored immediately. No gradual reintroduction. The contact reappears in the next available scene.

**EC3: Parity Crisis + Individual Crisis Simultaneously**
- **If** `isParityCrisis = true` AND `isUndergroundDanger = true` (or imperial)
- **Then** the individual crisis takes narrative priority. Parity crisis is a narrative state; the danger state is more immediately dangerous.

**EC4: Trust Shift from NSM Load**
- **If** `NSM.Load()` restores trust values that were modified externally (different save file)
- **Then** Dual Trust Economy receives `StateFullyLoaded` event. It should NOT re-apply decay on load. The loaded state is the authoritative state. Decay resumes from the loaded timestamp's `lastChoiceTime`.

**EC5: Delta That Would Increase Above 100**
- **If** a choice has `ΔT_imperial = +20` and current is `T_imperial = 85`
- **Then** clamp to 100 and emit `TrustBoundaryReached("imperial", 100)`. The boundary event at 100 is informational only (celebration possible) — the NSM clamps to 100, no different from any other trust level.

**EC6: Secret Trust Shifts**
- **If** a choice has `isSecret = true`
- **Then** the shift is applied to NSM state but no notification is shown. The `StateChanged` event is still emitted. Only systems that receive `StateChanged` can know a shift happened. The player sees no feedback — this is used for actions whose trust implications are not yet revealed to the player (e.g., a hidden choice that only later reveals its consequences).

**EC7: Decay During Crisis State**
- **If** a meter is in danger/crisis state and decay would push it further down
- **Then** decay continues. A player in danger/crisis who is inactive still decays. There is no decay pause for being in a dangerous state — this would make danger too comfortable.

**EC8: Trust Shift Due to Narrative Event (Not Choice)**
- **If** an Episode Structure narrative event applies a trust shift (e.g., "the protagonist is publicly commended by the imperial government")
- **Then** it calls `NSM.Mutate()` directly, same as a dialogue choice. The Dual Trust Economy system detects the change via `StateChanged` subscription and updates its internal state tracking. Same decay rules apply.

## Dependencies

### Upstream

| System | Type | Interface |
|--------|------|----------|
| **Narrative State Machine** | Hard | Stores `trust.imperial`, `trust.underground`. Emits `StateChanged`. |
| **Chapter Content Data** | Hard | Defines `ΔT` values per choice and `isSecret` flags. Dual Trust Economy consumes these at choice time. |

### Downstream

| System | Type | Interface |
|--------|------|----------|
| **HUD (Trust Meters)** | Hard | Subscribes to `StateChanged("trust.*")` and danger state changes. Reads values. |
| **Branching Dialogue System** | Hard | Reads trust values and danger states to gate options. Does not write trust. |
| **Notification System** | Soft | Receives `StateChanged("trust.*")` to display shift toasts. |
| **Episode Structure** | Soft | May apply narrative trust events via `Mutate()`. Reads crisis states to sequence crisis scenes. |

### Bidirectional Consistency
- If Chapter Content Data authors a new choice type with an unexpected `ΔT` range, flag during content review. The system supports any `ΔT` range — but narrative designers must respect the trust economy design.
- If Branching Dialogue needs to read trust for gating, it reads from NSM directly — no redundant trust state in Branching Dialogue.

## Tuning Knobs

All values in a `TrustEconomyConfig` ScriptableObject:

| Knob | Default | Safe Range | Effect |
|------|---------|-----------|--------|
| `TRUST_DANGER_THRESHOLD` | 25.0 | 15–35 | How close to empty before danger state triggers. Higher = more lenient, easier game. |
| `TRUST_CRISIS_THRESHOLD` | 15.0 | 10–20 | How close to empty before crisis triggers. |
| `PARITY_DISTANCE_THRESHOLD` | 10.0 | 5–20 | Distance between meters for parity crisis. |
| `PARITY_BOTH_BELOW` | 40.0 | 30–50 | Both must be below this for parity crisis. |
| `DECAY_AMOUNT` | 0.5 | 0.1–2.0 | Points lost per decay tick. Higher = meters drain faster when idle. |
| `DECAY_INTERVAL` | 30s | 10s–120s | How often decay ticks. Higher = slower drain. |
| `DECAY_GRACE_PERIOD` | 120s | 30s–300s | Time after last choice before decay starts. |
| `REDEMPTION_RATE_MULTIPLIER` | 0.5 | 0.25–0.75 | Multiplier on ΔT during redemption recovery. |
| `CRISIS_RECOVERY_THRESHOLD` | 20.0 | 15–25 | Trust value needed to exit crisis state. |
| `DANGER_RECOVERY_HYSTERESIS` | 30.0 | 25–35 | Trust must exceed this (not just cross threshold) to exit danger state. Prevents flickering. |

**What breaks if tuning is wrong:**
- `DANGER_THRESHOLD` too high → danger state triggers constantly, becomes noise
- `DANGER_THRESHOLD` too low → player is in crisis before they realize it, feels unfair
- `DECAY_GRACE_PERIOD` too short → meters drain during active play pauses, frustrating
- `DECAY_GRACE_PERIOD` too long → meters never decay, making idle players never lose trust
- `REDEMPTION_RATE_MULTIPLIER` too high → redemption is too easy, underground trust becomes renewable
- `REDEMPTION_RATE_MULTIPLIER` too low → redemption is almost impossible, underground cut = permanent narrative lock

## Acceptance Criteria

**AC1: Choice Shifts Trust**
- **GIVEN** `trust.imperial = 50, trust.underground = 50`, a choice with `ΔT_imperial = +10, ΔT_underground = -10`
- **WHEN** the choice is selected
- **THEN** NSM receives `Mutate("trust.imperial", 60)` and `Mutate("trust.underground", 40)`

**AC2: Secret Shift**
- **GIVEN** a secret choice with `ΔT_imperial = -15, isSecret = true`
- **WHEN** the choice is selected
- **THEN** Notification System receives no event, but `trust.imperial` is updated in NSM

**AC3: Danger State Entry**
- **GIVEN** `trust.underground = 26`, `TRUST_DANGER_THRESHOLD = 25`
- **WHEN** `Mutate("trust.underground", 24)` is called
- **THEN** `TrustBoundaryReached("underground", 24)` is emitted, HUD danger pulse activates, `state.dangerMode.underground = true`

**AC4: Crisis State Entry (Underground)**
- **GIVEN** `trust.underground = 16`, `TRUST_CRISIS_THRESHOLD = 15`
- **WHEN** `Mutate("trust.underground", 14)` is called
- **THEN** crisis state activates, underground contact verification sequence triggers

**AC5: Cut Contact (Underground = 0)**
- **GIVEN** `trust.underground = 5`
- **WHEN** `Mutate("trust.underground", -3)` is called, result clamped to 0
- **THEN** underground contacts are greyed out in UI, redemption arc begins on next scene

**AC6: Parity Crisis**
- **GIVEN** `trust.imperial = 35, trust.underground = 33`
- **WHEN** `Mutate("trust.imperial", 34)` is called
- **THEN** `state.parityCrisis = true` is set (distance = 1, both < 40)

**AC7: Danger State Exit (Hysteresis)**
- **GIVEN** `trust.underground = 24` (in danger), hysteresis = 30
- **WHEN** a choice applies `ΔT = +8` (result = 32)
- **THEN** danger state exits — 32 > 30 (hysteresis threshold), pulse stops, `state.dangerMode.underground = false`

**AC8: Redemption Recovery Rate**
- **GIVEN** `trust.underground = 0` (cut contact), redemption choice with `ΔT = +20`
- **WHEN** the choice is applied with `REDEMPTION_RATE_MULTIPLIER = 0.5`
- **THEN** effective delta = +10, new value = 10. Contact remains cut (below 20 threshold).

**AC9: Passive Decay**
- **GIVEN** last choice time was 200 seconds ago, `DECAY_GRACE_PERIOD = 120s, DECAY_INTERVAL = 30s`
- **WHEN** decay check runs
- **THEN** `ticks = floor((200-120)/30) = 2`. Each meter loses `2 × 0.5 = 1.0` point.

**AC10: NSM Load Restores Correct State**
- **GIVEN** a save file with `trust.imperial = 30, trust.underground = 45`
- **WHEN** Load is called
- **THEN** trust values are restored, decay resumes from saved timestamp, danger states recalculated from restored values

## Open Questions

| # | Question | Owner | Target Resolution |
|---|----------|-------|------------------|
| **OQ1** | Should there be a "trust momentum" mechanic — where consecutive choices in the same direction (several imperial-positive choices in a row) produce incrementally larger shifts? This would make streaks feel powerful but risky. | Game Designer | Before first playtest |
| **OQ2** | Should underground cut contact (T=0) ever be a story-required failure state, or only a player-caused condition? If story requires it for a particular ending, the pacing of trust erosion needs to be re-examined. | Narrative Director | Before Episode 1 content is finalized |
| **OQ3** | How does the system handle simultaneous multi-choice scenes (e.g., a choice that offers 3 options, each with different trust implications)? Does the player see all trust deltas before choosing, or only after? Showing deltas in advance risks making the choice transactional. Not showing them risks frustration. | Game Designer | Before dialogue system GDD is finalized |
| **OQ4** | Do we need a "trust forecast" in the HUD — a subtle hint of what the next choice might cost? This could be an optional accessibility feature, not shown by default. | UX Designer | During Vertical Slice |
