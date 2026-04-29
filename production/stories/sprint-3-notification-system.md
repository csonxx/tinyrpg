# S3-2: Notification System

> **Type**: Logic/UI
> **Status**: Ready for Dev
> **Sprint**: 3
> **Estimate**: 2 days
> **Owner**: gameplay-programmer
> **Dependencies**: S1-8 (Dual Trust Economy)

## GDD Reference
- `design/gdd/notification-system.md`

## Acceptance Criteria
- [ ] AC1: Trust change toast appears on every CHOICE selection showing delta (+8 Imperial! / -5 Underground!)
- [ ] AC2: Danger zone (≤25) triggers amber pulse warning toast
- [ ] AC3: Crisis zone (≤15) triggers red flash alert toast
- [ ] AC4: Notifications queue and display sequentially if multiple fire in rapid succession
- [ ] AC5: Toast auto-dismisses after 2 seconds or on tap

## Files to Create
- `src/ui/notifications/NotificationSystem.cs` — toast queue and display logic
- `src/ui/notifications/TrustToast.cs` — individual toast UI
- `tests/unit/ui/NotificationSystemTests.cs` — unit tests
