# Story: S1-8 — Dual Trust Economy

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: nice-to-have
> **Status**: backlog
> **Estimate**: 1 day
> **Owner**: gameplay-programmer
> **Type**: Logic
> **ADR**: No ADR applies — standard economy formula patterns
> **Manifest Version**: N/A — manifest not yet created

---

## Overview

Implement the dual trust tracking with danger/crisis thresholds and passive decay. Integrates with NSM and emits events to HUD and Notification System.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#)
- **GDD Ref**: `design/gdd/dual-trust-economy.md`

## Trust Ranges

| Threshold | Imperial | Underground |
|-----------|----------|-------------|
| Start | 40 | 40 |
| Danger | ≤25 | ≤25 |
| Crisis | ≤15 | ≤15 |
| Max | 100 | 100 |

## Events Emitted

| Event | Payload | When |
|-------|--------|------|
| `TrustValueChanged` | (imperial, underground) | After any change |
| `TrustShiftApplied` | (deltaImperial, deltaUnderground) | After choice |
| `DangerZoneEntered` | meterName | Trust crosses below 25 |
| `CrisisEntered` | meterName | Trust crosses below 15 |
| `PassiveDecayActive` | isActive | Decay starts/stops |

## Acceptance Criteria

- [ ] Trust values stored in NSM as `trust.imperial` and `trust.underground`
- [ ] ApplyShift clamps to ±10 per choice
- [ ] Passive decay: -0.5 per 30s after 120s grace
- [ ] Danger event fires at exactly 25
- [ ] Crisis event fires at exactly 15
- [ ] Parity crisis detected when both within 10 points AND both ≤25

## Dependencies

- S1-1 (NSM) — reads/writes trust values
- S1-6 (HUD) — receives TrustValueChanged
- Notification System — receives danger/crisis events

## Test Evidence

Location: `tests/unit/dual-trust/` — unit tests covering ApplyShift, Clamping, PassiveDecay, DangerZoneThreshold, CrisisThreshold, ParityCrisis
