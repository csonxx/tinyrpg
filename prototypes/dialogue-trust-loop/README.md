// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the branching dialogue + dual trust system create the intended
// emotional experience where every choice feels consequential with meaningful trade-offs?
// Date: 2026-04-29

# Dialogue + Trust Loop Prototype

## What This Tests
- Dialogue tree traversal (TEXT → CHOICE → TEXT)
- Trust shift application on choices
- Touch input (tap to advance, tap to select)
- Trust bar visualization
- **Core question: Do trust trade-offs feel meaningful?**

## Quick Setup (Unity 2022.3.x LTS)

### 1. Create Unity Project
- Unity Hub → New Project → 3D (or URP if preferred)
- Name: `DialogueTrustPrototype`
- Set Location to: `/Users/tt/goworkspace/src/tinyrpg/prototypes/dialogue-trust-loop/`

### 2. Import Dependencies
- Install **TextMeshPro** (Window → TextMeshPro → Import TMP Essential Resources)
- No other external dependencies needed for prototype

### 3. Create Scene
1. Delete default Cube/Camera
2. Create empty GameObject named `PrototypeScene` → Add `PrototypeScene.cs`
3. Create empty GameObject named `TrustManager` → Add `TrustManager.cs`
4. Create empty GameObject named `DialogueEngine` → Add `DialogueEngine.cs`
5. Create empty GameObject named `DialogueUI` → Add `DialogueUI.cs`
6. Create empty GameObject named `TouchInput` → Add `TouchInput.cs`

### 4. Build UI (Canvas)
1. Canvas (Screen Space - Overlay):
   - Create Panel `TrustPanel` (top-left, 200x100):
     - Create `ImperialBar` Panel (background dark, fill bar, label)
     - Create `UndergroundBar` Panel (background dark, fill bar, label)
   - Create Panel `DialoguePanel` (bottom, ~80% width, 35% height):
     - TextMeshPro `SpeakerText` (top-left, bold)
     - TextMeshPro `BodyText` (body area, scrollable if needed)
     - TextMeshPro `TapIndicator` (bottom-right, "▼" or "Tap")
   - Create Panel `ChoicesPanel` (below dialogue panel):
     - Button prefab (for dynamically created choices)
   - Create Panel `DebugPanel` (top-right corner for log output)

### 5. Wire References in Inspector
```
PrototypeScene:
  TrustManager → TrustManager gameobject
  DialogueEngine → DialogueEngine gameobject
  DialogueUI → DialogueUI gameobject
  ImperialBar → TrustBarUI on imperial bar object
  UndergroundBar → TrustBarUI on underground bar object
  DebugLog → TextMeshPro in debug panel

TrustBarUI (Imperial):
  Fill Image → fill bar image
  Label Text → "Imperial" text
  Value Text → value display

TrustBarUI (Underground):
  Fill Image → fill bar image
  Label Text → "Underground" text
  Value Text → value display

DialogueUI:
  Dialogue Panel → the panel
  Speaker Text → speaker TMP
  Body Text → body TMP
  Tap Indicator → indicator GameObject
  Choices Panel → choices container panel
  Choice Button Prefab → your button prefab
  Choice Container → parent transform for buttons
```

### 6. Run
- Press Play
- Tap screen to advance text
- Tap choice buttons to select
- Watch trust bars shift

## Files
| File | Purpose |
|------|---------|
| `DialogueNode.cs` | Dialogue tree node data structure |
| `TrustManager.cs` | Dual trust economy (imperial + underground) |
| `DialogueEngine.cs` | Tree traversal, choice handling |
| `TrustBarUI.cs` | Visual trust meter bars |
| `DialogueUI.cs` | Dialogue box, speaker, choices |
| `TouchInput.cs` | Tap detection |
| `SampleDialogue.cs` | Hardcoded test dialogue tree |
| `PrototypeScene.cs` | Main orchestrator |

## Sample Dialogue Content
The prototype uses a hardcoded dialogue scene:
- Captain YAMAMOTO gives orders about a shipment
- 2 choice points with 3 options each
- Each choice has a unique trust shift
- 3 different endings based on path taken

## What to Look For When Testing
1. Does each choice feel like it has weight?
2. Do you notice the trust bars shifting?
3. Do the trust shifts feel "fair" — not arbitrary?
4. Does advancing text feel responsive?
5. Would this feel engaging over a full chapter?

## Prototype Constraints (What We Skipped)
- No NSM persistence (trust is lost on restart)
- No save/load
- No scene transitions
- No auto-advance timer
- No haptic feedback
- No localization
- Hardcoded dialogue tree (would come from Chapter Content Data in production)
- UI is placeholder styling (not art-bible compliant)
