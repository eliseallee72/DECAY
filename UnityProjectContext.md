# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-15.
- Baseline branch: GitHub `step-1`, validated in Unity by Elise before Step 2 began.
- Current state: a visual battle prototype in `SampleScene`; gameplay code is being rebuilt from the authoritative DECAY documentation and the GameMaker v8_21 reference.

## Migration Status

- Step 1 validated: typed stable content IDs, unique dice/effect runtime instance IDs, `BattleConfig`, definitions/catalog, runtime dice state, explicit slot-pair identity, `BattlePhaseTransitionValidator`, and structured `BattleHistory` foundation.
- Step 2 second pass implemented locally: authoritative battle/board/inventory state plus state-driven GameEndCondition, centralized board/inventory transfer coordination, owner-level DECAYED inventory exclusion, unique player permanent identity enforcement, authoritative Fact-context capture, concrete Facts, and expanded EditMode tests.
- Step 3 next: movement Requests/Gates and the single permission path separating Setup inventory exchange from board-only Reposition. Do not wire player views directly to Commands. Decay/Score resolvers and game-end cleanup remain separate later process work.

## Confirmed Environment

- Unity version: 6000.5.8f1.
- Render pipeline: Universal Render Pipeline 17.5.0.
- Input system: Unity Input System 1.20.0 is configured; no first-party gameplay input code exists yet.
- Target: desktop PC presentation at 16:9, defaulting to 1920x1080.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.5.0 | Confirmed | `Packages/manifest.json` |
| Input | Unity Input System 1.20.0 | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions` |
| Tests | Unity Test Framework 1.7.0 | Confirmed | `Packages/manifest.json` |
| Networking | No first-party multiplayer implementation | Confirmed | repository code and packages |
| Unity MCP | No Unity MCP provider or callable Unity Editor tools detected | Confirmed | package/config/tool inspection |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scenes/` | Battle visual prototype scene | Confirmed | `SampleScene.unity` |
| `Assets/Sprites/` | Imported DECAY art and materials | Confirmed | repository tree |
| `Assets/Settings/` | URP and volume assets | Confirmed | repository tree |
| `Assets/DECAY/Scripts/` | First-party DECAY runtime code | Confirmed | migration implementation |
| `Assets/DECAY/Tests/` | DECAY EditMode/PlayMode tests | Confirmed | migration implementation |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Decay.Runtime` | Definitions, IDs, runtime state, rules, and later controllers/resolvers | UnityEngine | Views must not become rule authority. |
| `Decay.Tests.EditMode` | Deterministic rule and state tests | `Decay.Runtime`, Unity Test Framework | Editor only. |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/SampleScene.unity` only.
- Likely startup scene: `SampleScene`.
- Scene loading flow: none implemented.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Authority | One authoritative owner for each mutable fact | Confirmed | `DECAY unity update.txt` |
| Content | Immutable editor-authored ScriptableObject definitions | Confirmed | `DECAY unity update.txt` |
| Runtime | Plain C# battle state separated from definitions and views | Confirmed | `DECAY unity update.txt` |
| Presentation | Views display completed state and submit requests; they do not enforce rules | Confirmed | `DECAY unity update.txt` |
| Ordering | Explicit deterministic phase and slot order | Confirmed | `DECAY unity update.txt` |

## Coding Conventions

- Namespace root: `Decay`.
- Public types/members: PascalCase; private fields: `_camelCase`.
- Serialized fields: `[SerializeField] private` with read-only public access.
- Use `Dice`, including for one gameplay object.
- Important gameplay and presentation values remain editor-tunable without duplicating runtime authority.

## Testing And Validation

- EditMode tests: supported through `Decay.Tests.EditMode`.
- PlayMode tests: not established yet.
- CI/build validation: none detected.
- Unity Editor validation must be run locally until an Editor/MCP or CI environment is connected.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Repository inspection/editing | available | Codex workspace |
| Unity Editor connection/Console/tests | unavailable | no Unity MCP tools exposed |
| GitHub read | available | repository remote |
| GitHub push from local Git | unavailable | connector credentials are not shared with local Git |

## Important Constraints

- The current DECAY documentation and Elise's confirmed decisions outrank old GameMaker implementation details.
- Current battle structure is 2 games, 4 rounds per game, and a maximum battle inventory of 10 dice.
- Preserve unique permanent dice identity and separate Global Inventory, Battle Inventory, runtime state, and visual views. Runtime effect occurrences use `EffectInstanceId` separately from stable `EffectId`.
- Do not store live battle state in ScriptableObject assets.
- Do not infer rules or occupancy from transforms, hierarchy order, object names, animation state, enum ordinal values, or generic `SlotId` sorting.
- Do not modify scenes/prefabs until the data and referee foundations are established.

## Unknowns And Confidence

- Inspector/prefab bindings and runtime scene composition remain unimplemented.
- Step 2 Unity compilation and Test Runner results are unverified until this package is opened in Unity 6000.5.8f1.
- Movement Gate shape, Decay/GameEnd membership orchestration, slot process transitions, and permanent-upgrade/save commit rules remain intentionally deferred; see the running Foundation Change Tracker.

## Source Files Inspected

- `DECAY unity update.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker.txt`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- GameMaker v8_21 global inventory, battle inventory, and battle dice Create scripts
- CardHouse `CardDefinition`, `CardGroup`, `CardGroupSettings`, `Gate`, `GateCollection`, and movement/operator references
- spire-codex README/architecture plus extracted card and monster parser evidence used as an architecture reference, not as direct game source

<!-- unity-onboarding:generated:end -->
