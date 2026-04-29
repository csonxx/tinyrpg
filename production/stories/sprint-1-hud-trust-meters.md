# Story: S1-6 — HUD Trust Meters

> **Epic**: Core Loop MVP
> **Sprint**: 1
> **Priority**: should-have
> **Status**: backlog
> **Estimate**: 2 days
> **Owner**: ui-programmer

---

## Overview

Implement the persistent trust meter display: two horizontal bars (Imperial Loyalty + Underground Trust) in the top-left corner, with danger zone and crisis visualizations.

## Technical Guidance

- **Engine**: Unity 2022.3.x LTS (C#) — UI Toolkit or UGUI
- **GDD Ref**: `design/gdd/hud-trust-meters.md`
- **Art Bible**: Dusty Ochre (#B8925A) for Imperial, Muted Jade (#5E8B7E) for Underground

## Visual Specifications

| Element | Imperial | Underground |
|---------|---------|-------------|
| Normal color | Dusty Ochre | Muted Jade |
| Danger color (≤25) | Amber pulse | Amber pulse |
| Crisis color (≤15) | Red flash | Red flash |
| Bar size | 120px × 8px | 120px × 8px |

## Acceptance Criteria

- [ ] Both bars visible at all times during gameplay
- [ ] Bars animate from old value to new value (400ms ease-out)
- [ ] Below 25: amber pulse glow
- [ ] Below 15: red border flash (3 times)
- [ ] Passive decay indicator (▼ icon) appears after 120s of no choices
- [ ] Bars persist across scene transitions without flicker

## Dependencies

- S1-1 (NSM) — trust values stored in NSM
- S1-8 (Dual Trust) — emits `TrustValueChanged` events
