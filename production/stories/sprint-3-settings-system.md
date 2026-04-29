# S3-1: Settings System

> **Type**: Logic/Config
> **Status**: Ready for Dev
> **Sprint**: 3
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Dependencies**: S2-4 (Audio Management)

## GDD Reference
- `design/gdd/settings-system.md`

## Acceptance Criteria
- [ ] AC1: Volume settings (Music, SFX, Voice) apply immediately to all audio output
- [ ] AC2: Text speed (Slow/Medium/Fast) affects dialogue animation duration
- [ ] AC3: Haptic feedback toggle enables/disables vibration on choice selection
- [ ] AC4: Auto-advance toggle controls whether TEXT nodes auto-progress after animation completes
- [ ] AC5: All settings persist to device storage and restore on app restart

## Files to Create
- `src/core/settings/SettingsSystem.cs` — main MonoBehaviour, JSON file persistence
- `src/core/settings/SettingsData.cs` — serializable settings data class
- `tests/unit/core/SettingsSystemTests.cs` — unit tests
