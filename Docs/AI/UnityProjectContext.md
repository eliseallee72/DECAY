# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-16.
- Canonical validated baseline: GitHub `FINAL-step-5`.
- Current local implementation pass: Step 6 first pass built from the validated Step 5 v2.1 source matching `FINAL-step-5` core/scene hashes; no Step 6 GitHub branch is created during local validation.
- Unity version: 6000.5.8f1.
- Current state: visual battle prototype plus a plain-C# gameplay foundation being rebuilt from DECAY's authoritative documentation and the GameMaker v8_21 fallback behavior.

## Migration Status

- Step 1 validated: stable content/runtime identity, configuration, dice runtime state, slot-pair identity, phase validation, and BattleHistory foundation.
- Step 2 validated: authoritative BattleState, BoardState, BattleInventoryState, GlobalInventoryState, movement command primitives, GameEndCondition, and concrete state Facts. Elise validated 102/102 EditMode tests.
- Step 3 validated: one MoveDiceRequest -> Gate -> Command -> Fact path for Setup/Reposition movement. Elise validated all expected 135 EditMode cases.
- Step 4 is included in the later validated Step 5 baseline: injected IRandomSource, DiceRollResolver, ApplyDiceRollCommand, DiceRolledFact, and RollExecutor with deterministic roll draw order.
- Step 5 validated: explicit authoritative `EnemySetup -> PlayerSetup -> Rolling -> EnemyReposition -> PlayerReposition -> DecayProcess` flow, bounded BattleController orchestration, shared movement authority across both sides, recoverable Roll-source fallback before commit, a read-only Roll completion prerequisite, and internal-only low-level phase/full-board Roll entry points. Elise confirmed all 174 EditMode checks passed in Unity 6000.5.8f1.
- Step 6 first pass: BattleBootstrapper/BattleRuntime composition, read-only tracked battle roster enumeration, BattleCompositionRoot, ID-bound DiceView spawning, explicit SlotId presentation anchors, authoritative view reconciliation, and Player drag/drop submission through MoveDiceController. Expected totals after import: 183 EditMode + 6 PlayMode; Unity validation pending.

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
- Runtime construction of `RollExecutor` requires both a primary and an injected fallback `IRandomSource`. Only `RecoverableRandomSourceException` may trigger fallback; invariant/programming failures are not converted into random gameplay results.
- A recoverable primary-source failure discards the entire uncommitted primary Roll plan and resolves the full plan from fallback. It never mixes partial scripted values with fallback values.
- `RollExecutionResult` is an immutable phase-local completion receipt, not a second owner of dice values. `DiceRuntimeState` remains authoritative for current faces.
- `RollCompletionGate` is read-only. `BattleController.CompleteRoll` cannot leave Rolling unless the current game/round has a successful Roll receipt whose participant resolutions still match authoritative dice faces.
- `BattleController.CompleteRoll` remains the future blocking-presentation completion boundary; presentation never chooses or changes the roll result.
- Step 5 intentionally stops after entering DecayProcess.

## Setup Phase Migration Safety

- The abandoned `BattlePhase.Setup` + `BattleSetupTurn` architecture has been fully removed from runtime code and tests.
- `BattleState.CurrentPhase` is the sole battle-phase/actor-order authority. There is no `CurrentSetupTurn`, `ApplyEnemySetupCompleted`, or `BattlePhase.Setup` compatibility API.
- Setup and reposition side mismatches use the same canonical `MoveDiceDenialReason.ActingSideDoesNotMatchPhase`; no setup-specific denial or numeric compatibility alias remains.
- Runtime denial enums use names rather than hand-maintained compatibility numbers. `BattlePhase` retains explicit serialization identities because Unity may serialize structural enums, but Step 5 v2 re-baselines them in the actual phase sequence now that no abandoned Setup compatibility remains. Flow never uses enum arithmetic or ordinal ordering.
- The superseded `CompleteEnemySetupCommand` and `SetupTurnChangedFact` remain deleted; enemy setup completion records the normal `PhaseChangedFact` through the phase authority.

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
- Logical runtime dice creation/view binding and baseline Player drag/drop now exist in the Step 6 first pass. Roll animation presentation, EnemyController planning, Decay/Save resolution, Score, RoundEnd/GameEnd cleanup, Drawer/carousel presentation, and permanent save/upgrade semantics remain deferred.

## Roll Failure / Recovery Policy

- Recoverable random-source failure is now resolved inside the `Rolling` phase. The primary full Roll plan is prepared before dice mutation; if the source throws `RecoverableRandomSourceException`, that uncommitted plan is discarded and the entire Roll is retried from the injected fallback source.
- This fallback is deliberately narrow. Board/inventory inconsistencies, invalid dice state, command invariant failures, or fallback-source programming errors are not swallowed or randomized through.
- Leaving `Rolling` requires the read-only Roll completion prerequisite to pass. A previous-round face alone cannot satisfy it because completion is tied to the current game/round execution receipt.
- Presentation cancellation/failure semantics after logical commit are still deferred until the real Presentation/Process executor exists. Once a Roll is committed, later presentation failure must reconcile visuals to authoritative state rather than undo gameplay.
- A truly fatal invariant failure still needs the future battle/process abort or restart policy; Step 5 intentionally does not invent generic rollback/transaction infrastructure.

## Validation

- Step 2: 102/102 EditMode tests validated locally by Elise.
- Step 3: all expected 135 EditMode tests validated locally by Elise.
- Step 4 behavior is covered by the later validated Step 5 baseline.
- Step 5: 174/174 EditMode checks validated locally by Elise in Unity 6000.5.8f1.
- Step 6 first pass: expected 183 EditMode + 6 PlayMode; Unity compilation/Test Runner validation pending.
- `ArchitectureBoundaryTests` protect the internal-only `BattlePhaseController.Handle` / `RollExecutor.ExecuteRoll` seams and require an explicit fallback source on public RollExecutor construction.
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
