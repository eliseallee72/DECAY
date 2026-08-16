# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-16.
- Baseline branch: GitHub `step-4`, the source baseline for Step 5.
- Current implementation branch: GitHub `step-5`.
- Unity version: 6000.5.8f1.
- Current state: visual battle prototype plus a plain-C# gameplay foundation being rebuilt from DECAY's authoritative documentation and the GameMaker v8_21 fallback behavior.

## Migration Status

- Step 1 validated: stable content/runtime identity, configuration, dice runtime state, slot-pair identity, phase validation, and BattleHistory foundation.
- Step 2 validated: authoritative BattleState, BoardState, BattleInventoryState, GlobalInventoryState, movement command primitives, GameEndCondition, and concrete state Facts. Elise validated 102/102 EditMode tests.
- Step 3 validated: one MoveDiceRequest -> Gate -> Command -> Fact path for Setup/Reposition movement. Elise validated all expected 135 EditMode cases.
- Step 4 first pass: injected IRandomSource, DiceRollResolver, ApplyDiceRollCommand, DiceRolledFact, and RollExecutor with deterministic roll draw order. Local Step 4 Unity validation remains required; expected total is 155 EditMode cases.
- Step 5 revised/audited first pass: explicit authoritative `EnemySetup -> PlayerSetup -> Rolling -> EnemyReposition -> PlayerReposition -> DecayProcess` flow, bounded BattleController orchestration, shared movement authority across both sides, an explicit Roll completion seam for future blocking presentation, and internal-only low-level phase/full-board Roll entry points. Expected total after import is now 168 EditMode cases; local Unity validation is still required.

## Architecture

- One authoritative owner per mutable fact.
- `BattleState.CurrentPhase` is the single authority for battle phase and actor-order stages.
- `BoardState` owns slot occupancy; `BattleInventoryState` owns battle-inventory membership.
- Enemy and Player movement use the same `MoveDiceController` Request/Gate/Command/Fact route.
- `EnemySetup` permits Enemy inventory/board movement; `PlayerSetup` permits Player inventory/board movement.
- `EnemyReposition` permits Enemy board-only movement; `PlayerReposition` permits Player board-only movement.
- `BattleController` coordinates bounded flow but does not choose enemy moves, validate movement, select random faces, calculate DECAY/SCORE, or own presentation results.
- `BattlePhaseController` remains the low-level phase-change authority used by `BattleController`, but its mutation entry point is internal so external gameplay/view/AI assemblies cannot bypass BattleController.
- `RollExecutor` remains the logical Roll authority. Its full-board ExecuteRoll entry point is internal and is invoked by `BattleController.RequestRoll` after entering Rolling.
- `BattleController.CompleteRoll` is the future blocking-presentation completion boundary; presentation never chooses or changes the roll result.
- Step 5 intentionally stops after entering DecayProcess.

## Setup Phase Migration Safety

- The previous Step 5 sub-turn design (`BattlePhase.Setup` + mutable `CurrentSetupTurn`) is superseded.
- `EnemySetup` retains numeric enum value 0 so old serialized `Setup` data continues to map to the enemy-first setup stage.
- Existing phase numeric identities Rolling through BattleEnd remain 1 through 8.
- `PlayerSetup` is value 9.
- `BattlePhase.Setup` remains only as an obsolete migration alias to `EnemySetup`; new runtime code must use explicit phase names.
- `BattleSetupTurn`, the derived `BattleState.CurrentSetupTurn` compatibility projection, and `ApplyEnemySetupCompleted` remain temporarily only for older movement-test fixtures. They do not store competing runtime authority and should be removed after those fixtures are migrated.
- `MoveDiceDenialReason.ActingSideDoesNotMatchSetupPhase` is canonical; the old SetupTurn denial name is retained only as an obsolete numeric alias.
- The superseded `CompleteEnemySetupCommand` and `SetupTurnChangedFact` were removed; enemy setup completion records the normal `PhaseChangedFact` through the phase authority.

## Reference Architecture Audit

- CardHouse's narrow `Gate<T>` / `GateCollection<T>` permission composition remains a useful model, but DECAY deliberately avoids its public mutable group membership and presentation-coupled authority patterns.
- DECAY's movement Gates return typed denial reasons, read authoritative state, and do not perform mutations; approved Commands/Facts remain separate.
- DECAY's `BoardInventoryTransferExecutor` coordinates BoardState/BattleInventoryState transfers without becoming a third owner of dice location.
- spire-codex is treated as reverse-engineering-derived architecture evidence rather than direct source code. Its exposed typed powers/commands, stackability, enemy move state machines, event preconditions/choices, and structured gameplay variables reinforce DECAY's definition/runtime split, source-aware effect instances, explicit command/fact mutations, and future plan-based EnemyController direction.
- Do not copy reference-project implementation details that conflict with DECAY's stricter deterministic, single-authority, and presentation-separated requirements.

## Environment / Packages

- Render pipeline: Universal Render Pipeline 17.5.0.
- Input system: Unity Input System 1.20.0; gameplay input wiring is not yet implemented.
- Tests: Unity Test Framework 1.7.0.
- Build scene currently detected: `Assets/Scenes/SampleScene.unity`.
- Target presentation: desktop 16:9, default 1920x1080.
- No Unity Editor/MCP or Unity CI runner is connected in this environment, so compilation and Test Runner results cannot be claimed here.

## Important Constraints

- DECAY glossary/rules and Elise's confirmed decisions outrank older implementation details.
- Current battle structure is 2 games, 4 rounds per game, maximum battle inventory 10.
- Runtime effects use distinct `EffectInstanceId` values separately from stable `EffectId`.
- Do not store live battle state in ScriptableObject definitions.
- Do not infer gameplay facts from transforms, hierarchy order, object names, animation state, enum ordinal arithmetic, or generic collection iteration order.
- Do not give EnemyController a privileged BoardState mutation path; it must submit the same approved movement requests.
- Do not expose lower-level process or phase mutation methods merely for convenience; public gameplay APIs should preserve orchestration boundaries.
- Do not introduce duplicate per-dice/view processing flags. Add processing/blocking Gates only when a real active process/presentation owner exists.
- Runtime dice views, Roll presentation, EnemyController planning, Decay/Save resolution, Score, RoundEnd/GameEnd cleanup, and permanent save/upgrade semantics remain deferred.

## Known Deferred Robustness Question

- `BattleController.RequestRoll` enters Rolling before `RollExecutor` resolves its full random plan. RollExecutor itself resolves all random choices before mutating dice, so an invalid scripted/random source cannot leave a half-rolled board. A fatal invariant/script-source exception can still leave the battle phase at Rolling without a completed Roll boundary. This is an exceptional failure-policy question, not a normal gameplay denial; do not invent rollback until process-failure semantics are explicitly designed.

## Validation

- Step 2: 102/102 EditMode tests validated locally by Elise.
- Step 3: all expected 135 EditMode tests validated locally by Elise.
- Step 4: expected 155 EditMode tests; Unity validation pending.
- Step 5 audited first pass: expected 168 EditMode tests; Unity validation pending.
- `ArchitectureBoundaryTests` specifically protect the internal-only `BattlePhaseController.Handle` and `RollExecutor.ExecuteRoll` seams.
- Required local check: open in Unity 6000.5.8f1, confirm no new red Console errors, then run all `Decay.Tests.EditMode` tests.

## Source Files / References Inspected

- `DECAY unity update.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker_Step5_Revision.txt`
- Step-4/Step-5 implementation and tests
- GameMaker v8_21 fallback implementation where still relevant
- CardHouse Gate/GateCollection, CardGroup, and EventChain architecture references
- spire-codex README/reverse-engineering architecture evidence for cards, powers, enemy state machines, event preconditions/choices, and structured data

<!-- unity-onboarding:generated:end -->
