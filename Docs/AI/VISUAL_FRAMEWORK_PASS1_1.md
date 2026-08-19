# DECAY Visual Framework Pass 1.1

This pass is framework-only. The DECAY Unity Update / Code Best Practices remain the implementation authority; the Visual Battle Integration Update describes presentation requirements but never moves gameplay authority into Views.

## Authority-first presentation contract

- Input surfaces only raise Requests. Views and Animators never advance battle phases or complete gameplay processes.
- `BattleCompositionRoot` is the Unity bridge that submits Hourglass requests to authoritative gameplay, receives the approved result, then asks `BattlePresentationDirector` to display it.
- `BattlePresentationDirector` receives authoritative results plus completion callbacks. It never calls gameplay `Request...` or `Complete...` methods.
- Blocking authored presentation reports actual completion through Animation Events. The authoritative bridge decides what gameplay operation follows that completion.
- A presentation skip cancels transient visual work, hard-reconciles Views to current authoritative state, then reports completion through the authoritative continuation so required flow cannot remain blocked.
- Battle shutdown/restart cancellation uses a separate no-continuation path: it reconciles presentation without advancing gameplay that is being abandoned.

## Editor-authored presentation contract

- Fixed authored animation belongs in Unity Animation Clips / Animator Controllers.
- `DiceView`, `HourglassView`, `RoundCounterView`, `BattleBoardView`, and `SlotView` expose serialized Animator bindings.
- Persistent authoritative visual values are exposed separately from one-shot transition animations. Slot condition, Hourglass phase, and Round number may therefore be restored after interruption without relying on the last animation frame.
- If an authored binding is not configured, the presentation request completes immediately. No placeholder visual is created.
- `BattlePresentationSettings` exposes known future pacing and coded-motion tuning without choosing or applying final values in this pass.

## Semantic location vs rendered position

- Gameplay owns semantic location through `BoardState`, `BattleInventoryState`, `SlotId`, and movement results.
- `BattleSceneDiceLayout` maps semantic locations to editor-authored scene anchors and local offsets.
- `DiceView` stores the current presentation destination separately from its rendered Transform.
- `BattleDiceViewCoordinator.RefreshAllDestinations` can refresh authoritative destinations without moving rendered transforms.
- `BattleDiceViewCoordinator.ReconcileAll` is the explicit hard-snap fallback used after cancellation, rejection, scene recovery, or while a later movement presentation is not yet implemented.
- Player/enemy swap motion itself remains intentionally deferred to the coded-motion pass.
- Drag plane/lift direction use an editor-authored scene surface rather than a hardcoded world axis.

## Interaction separation

- Hourglass click input is event-driven through the Input System rather than polled in `Update`.
- The Hourglass raises one interaction request; current authoritative battle phase determines which request is legal.
- Dice visual visibility and collider availability are separate APIs. A renderer being hidden does not automatically control its interaction surface.
- Dice drag press/release are Input System events. `Update` remains only for genuinely continuous pointer-following while a drag is active; the released move still goes through the existing `MoveDiceRequest` Gates.

## Predictive DECAY authority

`DecayProcessResolver` remains shared by committed `DecayExecutor` execution and read-only preview. Presentation receives immutable preview output and does not inspect roll values or maintain a Save queue.

During Player Reposition, Targeted / WillDecay / Save-source indicators are populated only from the authoritative Decay preview. Future effects that alter those predictions must extend authoritative preview logic rather than DiceView logic.

## Effect presentation framework

- The previous single generic Effect trigger/idle has been replaced with typed `EffectId + PresentationChannel` editor mappings.
- An `EffectPresentationRequest` can carry stable effect identity, effect instance identity, source Dice, target Dice, and presentation channel without interpreting effect rules.
- Multiple effect mappings on one DiceView may be active independently, allowing future ordered or overlapping effect presentations instead of collapsing all effects into one Animator state.
- Actual effect animations and effect execution sequencing are not implemented in this pass.

## Slot reconciliation

- `SlotView` now has a persistent editor-authored `SlotCondition` presentation binding in addition to Break/Unstable/Checked transition hooks.
- Hard reconciliation reads the current `BoardState` condition for every slot and restores that persistent state.
- Therefore a break/unstable animation is only a transition into an authoritative state; it is never the owner of whether the slot is Broken or Unstable.

## Deliberately deferred

This pass does **not** implement player swap movement, enemy swap movement, Decay/Score sequencing, timing/stagger execution, final easing, authored animation clips, placeholder visual effects, or authored effect animations. The current bare-playable Decay/Score progression remains in the authoritative Unity bridge until the later visual-process pass replaces that fallback with blocking presentation.
