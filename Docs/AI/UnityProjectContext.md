# DECAY Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: repository root (`Assets/`, `Packages/`, and `ProjectSettings/`).
- Last analyzed: 2026-08-16.
- Canonical validated baseline: GitHub `step-8` (Unity-validated by Elise: Step 8 v2 compiles and all 230 EditMode + 8 PlayMode tests pass).
- Current local implementation pass: first playable battle-input pass built from accepted `step-8`; no GitHub branch is created during local validation.
- Unity version: 6000.5.8f1.
- Current state: visual battle prototype plus a plain-C# gameplay foundation being rebuilt from DECAY's authoritative documentation and the GameMaker v8_21 fallback behavior.

## Migration Status

- Step 1 validated: stable content/runtime identity, configuration, dice runtime state, slot-pair identity, phase validation, and BattleHistory foundation.
- Step 2 validated: authoritative BattleState, BoardState, BattleInventoryState, GlobalInventoryState, movement command primitives, GameEndCondition, and concrete state Facts. Elise validated 102/102 EditMode tests.
- Step 3 validated: one MoveDiceRequest -> Gate -> Command -> Fact path for Setup/Reposition movement. Elise validated all expected 135 EditMode cases.
- Step 4 is included in the later validated Step 5 baseline: injected IRandomSource, DiceRollResolver, ApplyDiceRollCommand, DiceRolledFact, and RollExecutor with deterministic roll draw order.
- Step 5 validated: explicit authoritative `EnemySetup -> PlayerSetup -> Rolling -> EnemyReposition -> PlayerReposition -> DecayProcess` flow, bounded BattleController orchestration, shared movement authority across both sides, recoverable Roll-source fallback before commit, a read-only Roll completion prerequisite, and internal-only low-level phase/full-board Roll entry points. Elise confirmed all 174 EditMode checks passed in Unity 6000.5.8f1.
- Step 6 validated: BattleBootstrapper/BattleRuntime composition, read-only tracked battle roster enumeration, BattleCompositionRoot, ID-bound DiceView spawning, explicit SlotId presentation anchors, authoritative view reconciliation, and Player drag/drop submission through MoveDiceController. Elise confirmed 183 EditMode + 6 PlayMode tests passed and runtime dice spawned correctly. Known presentation follow-up: Enemy Battle Inventory dice are currently visible in the integration row; final enemy presentation should only show enemy dice once placed on the Board.
  Step 6 input review also notes that drag hit scanning currently aborts on an Enemy DiceView hit; revisit if camera overlap makes this obstruct Player picking.
- Step 7 validated: DecayResolver + DecayExecutor, process-local ordered WILLSAVE identity, 1..6 simultaneous pair decisions, DECAYED/SAVED outcomes, Broken/Unstable propagation, causal DECAY Facts, completion receipt/gate, and BattleController `RequestDecay -> CompleteDecay -> ScoreProcess` orchestration. Elise confirmed 209 EditMode + 7 PlayMode tests passed.
- Step 8 validated: ScoreState/ScoreResolver/ScoreExecutor, explicit SCORE completion, Unstable post-score ejection/break cleanup, round/game score rollover, between-game slot/DECAYED reset, Game 1 -> Game 2 progression, and final BattleEnd outcome. Elise confirmed the corrected Step 8 build compiles and all tests pass.
- Current playable pass: one shared Setup phase, EnemyController + replaceable setup planner, invisible Enemy Battle Inventory presentation, immediate Enemy setup placement through shared movement authority, and HourglassView click -> RequestRoll with an authored-presentation completion hook.

## Architecture

- One authoritative owner per mutable fact.
- `BattleState.CurrentPhase` is the single authority for battle phase and actor-order stages.
- `BoardState` owns slot occupancy; `BattleInventoryState` owns battle-inventory membership.
- Enemy and Player movement use the same `MoveDiceController` Request/Gate/Command/Fact route.
- `Setup` permits both Enemy and Player inventory/board movement through the same movement Gates. Enemy setup planning executes immediately on entering Setup but does not create a Player lockout/sub-turn.
- `EnemyReposition` permits Enemy board-only movement; `PlayerReposition` permits Player board-only movement.
- `BattleController` coordinates bounded flow but does not choose enemy moves, validate movement, select random faces, calculate DECAY/SCORE, or own presentation results.
- `BattlePhaseController` remains the low-level phase-change authority used by `BattleController`, but its mutation entry point is internal so external gameplay/view/AI assemblies cannot bypass BattleController.
- `RollExecutor` remains the logical Roll authority. Its full-board ExecuteRoll entry point is internal and is invoked by `BattleController.RequestRoll` after entering Rolling.
- Runtime construction of `RollExecutor` requires both a primary and an injected fallback `IRandomSource`. Only `RecoverableRandomSourceException` may trigger fallback; invariant/programming failures are not converted into random gameplay results.
- A recoverable primary-source failure discards the entire uncommitted primary Roll plan and resolves the full plan from fallback. It never mixes partial scripted values with fallback values.
- `RollExecutionResult` is an immutable phase-local completion receipt, not a second owner of dice values. `DiceRuntimeState` remains authoritative for current faces.
- `RollCompletionGate` is read-only. `BattleController.CompleteRoll` cannot leave Rolling unless the current game/round has a successful Roll receipt whose participant resolutions still match authoritative dice faces.
- `BattleController.CompleteRoll` remains the future blocking-presentation completion boundary; presentation never chooses or changes the roll result.
- Step 7 resolves the full logical DECAY pass. Step 8 now closes ScoreProcess -> RoundEnd -> GameEnd/next round -> next game/BattleEnd with separate completion receipts so presentation remains downstream of rules.

## Setup / Enemy Planning Architecture

- `BattlePhase.Setup` is the single setup phase and is the current authority. The prior Step 5 `EnemySetup` / `PlayerSetup` phase split is superseded by Elise's clarified rule.
- Enemy setup placement is immediate production orchestration, not a permission sub-turn: Player setup movement is legal during the same Setup phase.
- `EnemyController` owns coordination only. An injected `IEnemySetupPlanner` reads BattleState/BoardState/BattleInventoryState and returns intended `MoveDiceRequest`s; the shared `MoveDiceController` remains the only movement approval/mutation path.
- The first `FillAvailableEnemySlotsPlanner` deterministically fills empty Unbroken Enemy slots left-to-right from stable Enemy Battle Inventory order. It is a replaceable strategy, not a temporary direct-state shortcut.
- Enemy Battle Inventory remains authoritative but has no visible presentation row. Enemy inventory DiceViews are hidden/non-interactable until BoardState places them.
- `EnemyReposition` and `PlayerReposition` remain distinct ordered phases. The setup unification does not collapse reposition sequencing.
- `HourglassView` collects click input during Setup and submits `RequestRoll`; it does not change phase or roll state directly. `NotifyRollPresentationComplete` is the future animation/presenter completion hook that allows the existing Roll completion gate to enter EnemyReposition.

## Reference Architecture Audit

- CardHouse's narrow `Gate<T>` / `GateCollection<T>` permission composition remains a useful model, but DECAY deliberately avoids its public mutable group membership and presentation-coupled authority patterns.
- DECAY's movement Gates return typed denial reasons, read authoritative state, and do not perform mutations; approved Commands/Facts remain separate.
- DECAY's `BoardInventoryTransferExecutor` coordinates BoardState/BattleInventoryState transfers without becoming a third owner of dice location.
- spire-codex is treated as reverse-engineering-derived architecture evidence rather than direct source code. Its exposed typed powers/commands, stackability, enemy move state machines, event preconditions/choices, and structured gameplay variables reinforce DECAY's definition/runtime split, source-aware effect instances, explicit command/fact mutations, and future plan-based EnemyController direction.
- Do not copy reference-project implementation details that conflict with DECAY's stricter deterministic, single-authority, and presentation-separated requirements.

## Environment / Packages

- Render pipeline: Universal Render Pipeline 17.5.0.
- Input system: Unity Input System 1.20.0; Player dice drag/drop uses MoveDiceController, and the current playable pass adds HourglassView Setup click -> RequestRoll. Drawer/carousel controls remain deferred.
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
- Logical runtime dice creation/view binding and baseline Player drag/drop are validated from Step 6, logical Decay/Save from Step 7, and Score/lifecycle from validated Step 8. The current pass adds the Enemy setup planning boundary, hides Enemy inventory presentation, and adds Setup hourglass input. Enemy reposition strategy, authored Roll/Decay/Save/Score animation sequencing, Drawer/carousel presentation, Gamble, tutorial logic, and permanent save/upgrade semantics remain later work.

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
- Step 6: 183/183 EditMode and 6/6 PlayMode validated locally by Elise; runtime spawn smoke test also passed.
- Step 7 validated: 209 EditMode + 7 PlayMode.
- Step 8 v2: 230 EditMode + 8 PlayMode validated locally by Elise.
- Current playable pass adds new/updated tests; Unity compilation/Test Runner validation is pending until Elise imports this package.
- `ArchitectureBoundaryTests` protect the internal-only `BattlePhaseController.Handle` / `RollExecutor.ExecuteRoll` seams and require an explicit fallback source on public RollExecutor construction.
- Required local check: open in Unity 6000.5.8f1, confirm no new red Console errors, then run all `Decay.Tests.EditMode` and `Decay.Tests.PlayMode` tests.

## Source Files / References Inspected

- `DECAY unity update.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker.txt`
- `Docs/AI/DECAY_Foundation_Change_Tracker_Step5_Revision.txt`
- Step-4/Step-5 implementation and tests
- GameMaker v8_21 fallback implementation where still relevant
- CardHouse Gate/GateCollection, CardGroup, and EventChain architecture references
- spire-codex README/reverse-engineering architecture evidence for cards, powers, enemy state machines, event preconditions/choices, and structured data

<!-- unity-onboarding:generated:end -->
