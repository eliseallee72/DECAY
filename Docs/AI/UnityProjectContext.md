# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-16.
- Baseline branch: GitHub `step-4`, the current source baseline for the Step 5 first pass. Step 3 was previously validated by Elise with all expected 135 EditMode test cases green; Step 4 adds 20 cases but still requires local Unity validation.
- Current implementation branch: GitHub `step-5`.
- Current state: a visual battle prototype in `SampleScene`; gameplay code is being rebuilt from the authoritative DECAY documentation and the GameMaker v8_21 reference.

## Migration Status

- Step 1 validated: typed stable content IDs, unique dice/effect runtime instance IDs, `BattleConfig`, definitions/catalog, runtime dice state, explicit slot-pair identity, `BattlePhaseTransitionValidator`, and structured `BattleHistory` foundation.
- Step 2 validated: authoritative battle/board/inventory state plus state-driven GameEndCondition, centralized board/inventory transfer coordination, owner-level DECAYED inventory exclusion, unique player permanent identity enforcement, authoritative Fact-context capture, concrete Facts, and 102/102 EditMode test validation.
- Step 3 validated: `MoveDiceRequest`/target/result, authoritative movement Gate context, narrow core movement Gates, and one `MoveDiceController.RequestMove` path that maps approved Setup/Reposition requests to the existing Step 2 Commands and records Facts; all expected 135 EditMode cases passed locally.
- Step 4 first pass implemented: injected `IRandomSource` implementations, face-index `DiceRollResolver`, `ApplyDiceRollCommand`, `DiceRolledFact`, and plain-C# `RollExecutor` with explicit deterministic roll draw order and EditMode coverage. Local Unity validation remains required; expected Step 4 total is 155 EditMode cases.
- Step 5 first pass implemented on `step-5`: `BattleState` now owns authoritative Enemy/Player Setup turn order inside the existing `BattlePhase.Setup`; `MovementPhaseGate` enforces Enemy Setup -> Player Setup through the same `MoveDiceController`; bounded `BattleController` orchestration now owns Enemy Setup completion, Setup -> Rolling + one `RollExecutor` invocation, an explicit Roll completion boundary, EnemyReposition -> PlayerReposition, and PlayerReposition -> DecayProcess. No enemy strategy, DECAY rules, Score rules, or presentation result authority was added to `BattleController`.
- Runtime dice spawning/state-to-view binding and authored roll presentation are still intentionally deferred, so Steps 4-5 remain EditMode-focused.

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
| `Decay.Runtime` | Definitions, IDs, runtime state, rules, and controllers/resolvers | UnityEngine | Views must not become rule authority. |
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
| Ordering | Explicit deterministic phase, actor, and process-specific slot order | Confirmed | `DECAY unity update.txt`, Step 5 flow implementation |
| Setup actor order | `BattlePhase.Setup` remains one phase; `BattleState.CurrentSetupTurn` authoritatively permits Enemy Setup before Player Setup | Implemented, Unity validation pending | Step 5 implementation |
| Round flow | `BattleController` coordinates bounded phase/process handoffs without owning movement, roll randomness, enemy strategy, DECAY, Score, or presentation results | Implemented, Unity validation pending | Step 5 implementation |

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
- Step 5 adds 11 EditMode cases to the Step 4 expected 155, for an expected total of 166 after import. This count is not a claim that the Step 5 suite has passed locally.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Repository inspection/editing | available | connected GitHub repository tooling |
| Unity Editor connection/Console/tests | unavailable | no Unity MCP tools exposed |
| GitHub read/write | available | connected GitHub app |
| GitHub push from local Git | unavailable | connector credentials are not shared with local Git |

## Important Constraints

- The current DECAY documentation and Elise's confirmed decisions outrank old GameMaker implementation details.
- Current battle structure is 2 games, 4 rounds per game, and a maximum battle inventory of 10 dice.
- Preserve unique permanent dice identity and separate Global Inventory, Battle Inventory, runtime state, and visual views. Runtime effect occurrences use `EffectInstanceId` separately from stable `EffectId`.
- Do not store live battle state in ScriptableObject assets.
- Do not infer rules or occupancy from transforms, hierarchy order, object names, animation state, enum ordinal values, or generic `SlotId` sorting.
- Enemy and player Setup/Reposition actions must use the same movement Request/Gate/Command/Fact path; an eventual `EnemyController` plans actions but does not mutate `BoardState` directly.
- Roll presentation may delay the flow at the `BattleController.CompleteRoll` boundary, but it must not choose roll results or advance the phase independently.
- Do not modify scenes/prefabs until the data and referee foundations are established.

## Unknowns And Confidence

- Inspector/prefab bindings and runtime scene composition remain unimplemented.
- Step 2 is Unity-validated by Elise: 102/102 EditMode tests green and Play Mode works.
- Step 3 is Unity-validated by Elise with all expected 135 EditMode cases green.
- Step 4 Unity compilation/Test Runner remains unverified; expected total at Step 4 is 155 EditMode cases.
- Step 5 Unity compilation/Test Runner remains unverified; expected total after the first pass is 166 EditMode cases.
- Setup actor sequencing is no longer deferred: Step 5 implements it as `BattleSetupTurn` inside `BattlePhase.Setup`.
- Real Processing/Tutorial/blocking Gate owners, runtime dice view/input wiring, roll presentation, EnemyController planning, Decay/GameEnd membership orchestration, slot process transitions, and permanent-upgrade/save commit rules remain intentionally deferred; see the running Foundation Change Tracker.

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
