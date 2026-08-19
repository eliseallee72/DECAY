# DECAY Visual Framework Pass 1

This pass is framework-only. The DECAY Unity Update / Code Best Practices remain the implementation authority; the Visual Battle Integration Update describes presentation requirements but does not move gameplay authority into Views.

## Editor-authored presentation contract

- Fixed authored animation belongs in Unity Animation Clips / Animator Controllers.
- `DiceView`, `HourglassView`, `RoundCounterView`, `BattleBoardView`, and `SlotView` expose serialized Animator bindings for those authored states.
- One-shot authored clips report actual completion through the public `Notify...PresentationComplete` Animation Event methods on their View.
- If a binding is not configured, the request completes immediately. No placeholder visual is created.
- `BattlePresentationSettings` exposes known future pacing and coded-motion tuning without applying or choosing those values in Pass 1.

## Battle flow framework

- `BattlePresentationDirector` is explicitly referenced by `BattleCompositionRoot` in the battle scene.
- Enemy setup results are authoritative before `PresentEnemySetup` requests per-dice authored presentation.
- Roll rules resolve first without immediately reconciling final faces when Roll is initiated through the Director.
- Dice/Hourglass/Round Counter authored Roll hooks are requested through their Views.
- Blocking Roll presentation reports completion through Animation Events, then authoritative face sprites are reconciled.
- Optional Face Reveal hooks run after final face reconciliation and before the battle enters Enemy Reposition.
- Enemy Reposition exposes a board-wide authored cue hook; the cue completion enters Player Reposition.
- During Player Reposition, predictive Targeted / WillDecay / Save-source presentation is refreshed from authoritative Decay preview data.
- Pass 1 does not implement Decay/Score visual sequencing; the existing immediate logical fallback remains until that later pass.

## Predictive DECAY authority

`DecayProcessResolver` is shared by committed `DecayExecutor` execution and read-only preview. Both use the same `DecayResolver` and the same ephemeral WILLSAVE sequencing state. Presentation receives immutable preview output and does not inspect roll values or maintain a Save queue.

`DecayExecutionResult` now retains the authoritative pair decision context needed by future Decay presentation, including source/target information, save use/creation, outcome, and resulting slot state. It contains no visual timing.

## Scene framework added

The battle scene contains empty presentation components ready for editor assignment:

- `BattlePresentationDirector` on `BattleCompositionRoot`
- `BattleBoardView` on `BOARD`
- `RoundCounterView` on `ROUNDCOUNTER`
- `SlotView` on all 12 slot objects
- existing `HourglassView` and spawned `DiceView` expose new Animator binding fields

No new sprite, text, animation clip, Animator Controller, color treatment, runtime visual object, or GameMaker asset is added by this pass.
