# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-16.
- Baseline branch: GitHub `step-4`, the source baseline for Step 5.
- Current implementation branch: GitHub `step-5`.
- Unity version: 6000.5.8f1.
- Current state: visual battle prototype plus a plain-C# gameplay foundation being rebuilt from DECAY's authoritative documentation and GameMaker v8_21 fallback behavior.

## Migration Status

- Step 1 validated: stable content/runtime identity, configuration, dice runtime state, slot-pair identity, phase validation, and BattleHistory foundation.
- Step 2 validated: authoritative BattleState, BoardState, BattleInventoryState, GlobalInventoryState, movement command primitives, GameEndCondition, and concrete state Facts. Elise validated 102/102 EditMode tests.
- Step 3 validated: one MoveDiceRequest -> Gate -> Command -> Fact path for Setup/Reposition movement. Elise validated all expected 135 EditMode cases.
- Step 4 first pass: injected IRandomSource, DiceRollResolver, ApplyDiceRollCommand, DiceRolledFact, and RollExecutor with deterministic roll draw order. Local Step 4 Unity validation remains required; expected total is 155 EditMode cases.
- Step 5 revised first pass: explicit authoritative `EnemySetup -> PlayerSetup -> Rolling -> EnemyReposition -> PlayerReposition -> DecayProcess` flow, bounded BattleController orchestration, shared movement authority across both sides, and an explicit Roll completion seam for future blocking presentation. Expected total after import remains 166 EditMode cases; local Unity validation is still required.

## Architecture

- One authoritative owner per mutable fact.
- `BattleState.CurrentPhase` is the single authority for battle phase and actor-order stages.
- `BoardState` owns slot occupancy; `BattleInventoryState` owns battle-inventory membership.
- Enemy and Player movement use the same `MoveDiceController` Request/Gate/Command/Fact route.
- `EnemySetup` permits Enemy inventory/board movement; `PlayerSetup` permits Player inventory/board movement.
- `EnemyReposition` permits Enemy board-only movement; `PlayerReposition` permits Player board-only movement.
- `BattleController` coordinates bounded flow but does not choose enemy moves, validate movement, select random faces, calculate DECAY/SCORE, or own presentation results.
- `BattlePhaseController` remains the phase-change authority used by `BattleController`.
- `RollExecutor` remains the logical Roll authority. `BattleController.RequestRoll` invokes it once after entering Rolling.
- `BattleController.CompleteRoll` is the future blocking-presentation completion boundary; presentation never chooses or changes the roll result.
- Step 5 intentionally stops after entering DecayProcess.

## Setup Phase Migration Safety

- The previous Step 5 sub-turn design (`BattlePhase.Setup` + mutable `CurrentSetupTurn`) is superseded.
- `EnemySetup` retains numeric enum value 0 so old serialized `Setup` data continues to map to the enemy-first setup stage.
- Existing phase numeric identities Rolling through BattleEnd remain 1 through 8.
- `PlayerSetup` is value 9.
- `BattlePhase.Setup` remains only as an obsolete migration alias to `EnemySetup`; new runtime code must use explicit phase names.
- `BattleSetupTurn`, the derived `BattleState.CurrentSetupTurn` compatibility projection, and `ApplyEnemySetupCompleted` remain temporarily only for older movement-test fixtures. They do not store competing runtime authority and should be removed after those fixtures are migrated.
- The superseded `CompleteEnemySetupCommand` and `SetupTurnChangedFact` were removed; enemy setup completion now records the normal `PhaseChangedFact` through `BattlePhaseController`.

## Environment / Packages

- Render pipeline: Universal Render Pipeline 17.5.0.
- Input system: Unity Input System 1.20.0; gameplay input wiring is not yet implemented.
- Tests: Unity Test Framework 1.7.0.
- Build scene currently detected: `Assets/Scenes/SampleScene.unity`.
- Target presentation: desktop 16:9, default 1920x1080.
- No Unity Editor/MCP or CI runner is connected in this environment, so compilation and Test Runner results cannot be claimed here.

## Important Constraints

- DECAY glossary/rules and Elise's confirmed decisions outrank older implementation details.
- Current battle structure is 2 games, 4 rounds per game, maximum battle inventory 10.
- Runtime effects use distinct `EffectInstanceId` values separately from stable `EffectId`.
- Do not store live battle state in ScriptableObject definitions.
- Do not infer gameplay facts from transforms, hierarchy order, object names, animation state, enum ordinal arithmetic, or generic collection iteration order.
- Do not give EnemyController a privileged BoardState mutation path; it must submit the same approved movement requests.
- Do not introduce duplicate per-dice/view processing flags. Add processing/blocking Gates only when a real active process/presentation owner exists.
- Runtime dice views, Roll presentation, EnemyController planning, Decay/Save resolution, Score, RoundEnd/GameEnd cleanup, and permanent save/upgrade semantics remain deferred.

## Validation

- Step 2: 102/102 EditMode tests validated locally by Elise.
- Step 3: all expected 135 EditMode tests validated locally by Elise.
- Step 4: expected 155 EditMode tests; Unity validation pending.
- Step 5 revised first pass: expected 166 EditMode tests; Unity validation pending.
- Required local check: open in Unity 6000.5.8f1, confirm no new red Console errors, then run all `Decay.Tests.EditMode` tests.

## Source Files / References Inspected

- `DECAY unity update.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker_Step5_Revision.txt`
- Step-4 implementation and tests
- GameMaker v8_21 fallback implementation where still relevant
- CardHouse and spire-codex as architecture references only

<!-- unity-onboarding:generated:end -->
