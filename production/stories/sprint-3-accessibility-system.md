# S3-3: Accessibility System

> **Type**: Logic/UI
> **Status**: Ready for Dev
> **Sprint**: 3
> **Estimate**: 2 days
> **Owner**: ui-programmer
> **Dependencies**: S1-5 (Dialogue UI)

## GDD Reference
- `design/gdd/accessibility-system.md`

## Acceptance Criteria
- [ ] AC1: Text size (Small/Normal/Large) scales all dialogue text and UI labels
- [ ] AC2: Colorblind mode — Deuteranopia (green-weak) and Protanopia (red-weak) shift palette via shader
- [ ] AC3: Reduce motion — skips all fade transitions, uses CROSSFADE instead
- [ ] AC4: All interactive elements (buttons, choices) have Unity UI Toolkit label bindings for screen readers
- [ ] AC5: Settings persist via Settings System

## Files to Create
- `src/core/accessibility/AccessibilitySystem.cs` — central coordinator
- `src/core/accessibility/TextSizeMode.cs` — enum + scaling logic
- `src/core/accessibility/ColorblindMode.cs` — shader color matrix presets
- `src/ui/accessibility/AccessibilitySettingsUI.cs` — settings panel
- `tests/unit/core/AccessibilitySystemTests.cs` — unit tests
