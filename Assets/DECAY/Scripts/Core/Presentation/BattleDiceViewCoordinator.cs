using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Creates one DiceView per tracked battle dice and reconciles views from authoritative state.
    /// Authoritative semantic location is resolved to editor-authored presentation destinations; rendered transforms
    /// remain presentation state and may later move toward those destinations without changing gameplay ownership.
    /// </summary>
    public sealed class BattleDiceViewCoordinator
    {
        private readonly BattleRuntime _runtime;
        private readonly DiceCatalog _diceCatalog;
        private readonly DiceView _defaultDiceViewPrefab;
        private readonly Transform _diceViewRoot;
        private readonly BattleSceneDiceLayout _layout;
        private readonly Dictionary<DiceInstanceId, DiceView> _viewsByDiceId = new Dictionary<DiceInstanceId, DiceView>();

        public BattleDiceViewCoordinator(
            BattleRuntime runtime,
            DiceCatalog diceCatalog,
            DiceView defaultDiceViewPrefab,
            Transform diceViewRoot,
            BattleSceneDiceLayout layout)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _diceCatalog = diceCatalog ?? throw new ArgumentNullException(nameof(diceCatalog));
            _defaultDiceViewPrefab = defaultDiceViewPrefab ?? throw new ArgumentNullException(nameof(defaultDiceViewPrefab));
            _diceViewRoot = diceViewRoot ?? throw new ArgumentNullException(nameof(diceViewRoot));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            if (!_diceCatalog.TryValidate(out string catalogError))
                throw new ArgumentException(catalogError, nameof(diceCatalog));
            if (!_layout.TryValidate(out string layoutError))
                throw new ArgumentException(layoutError, nameof(layout));
        }

        public int SpawnedViewCount => _viewsByDiceId.Count;

        public void SpawnTrackedDiceViews()
        {
            IReadOnlyList<DiceInstanceId> trackedDiceIds = _runtime.BattleInventoryState.TrackedDiceIds;
            var spawnPlan = new List<(DiceInstanceId DiceId, DiceView Prefab)>();

            for (int i = 0; i < trackedDiceIds.Count; i++)
            {
                DiceInstanceId diceId = trackedDiceIds[i];
                if (_viewsByDiceId.ContainsKey(diceId))
                    continue;

                DiceRuntimeState diceState = _runtime.BattleInventoryState.GetDice(diceId);
                DiceDefinition definition = _diceCatalog.GetRequired(diceState.DefinitionId);
                DiceView prefab = ResolveViewPrefab(definition);
                if (!prefab.TryValidate(out string viewError))
                    throw new InvalidOperationException($"Dice definition {definition.Id} resolves to an invalid DiceView prefab: {viewError}");
                spawnPlan.Add((diceId, prefab));
            }

            for (int i = 0; i < spawnPlan.Count; i++)
            {
                (DiceInstanceId diceId, DiceView prefab) = spawnPlan[i];
                DiceView view = UnityEngine.Object.Instantiate(prefab, _diceViewRoot);
                view.name = $"DiceView_{diceId}";
                view.Bind(diceId);
                _viewsByDiceId.Add(diceId, view);
            }

            ReconcileAll();
        }

        /// <summary>
        /// Hard reconciliation fallback. This is intentionally separate from normal destination updates so skipped,
        /// cancelled, disabled, or interrupted presentation can always return to authoritative state immediately.
        /// </summary>
        public void ReconcileAll(bool invokeRecoveryHooks = false)
        {
            IReadOnlyList<DiceInstanceId> trackedDiceIds = _runtime.BattleInventoryState.TrackedDiceIds;
            for (int i = 0; i < trackedDiceIds.Count; i++)
            {
                DiceInstanceId diceId = trackedDiceIds[i];
                if (!_viewsByDiceId.TryGetValue(diceId, out DiceView view))
                    throw new InvalidOperationException($"Tracked dice {diceId} does not have a spawned DiceView.");
                ReconcileOne(diceId, view, true, invokeRecoveryHooks);
            }
            ReconcileSlotConditions(invokeRecoveryHooks);
        }

        /// <summary>
        /// Refreshes authoritative destination/content without snapping the rendered transform. This is the seam later
        /// coded movement will consume for swaps, placement, inventory return, shops, and other presentation motion.
        /// </summary>
        internal void RefreshAllDestinations()
        {
            IReadOnlyList<DiceInstanceId> trackedDiceIds = _runtime.BattleInventoryState.TrackedDiceIds;
            for (int i = 0; i < trackedDiceIds.Count; i++)
            {
                DiceInstanceId diceId = trackedDiceIds[i];
                if (!_viewsByDiceId.TryGetValue(diceId, out DiceView view))
                    throw new InvalidOperationException($"Tracked dice {diceId} does not have a spawned DiceView.");
                ReconcileOne(diceId, view, false, false);
            }
            ReconcileSlotConditions(false);
        }

        public bool TryGetView(DiceInstanceId diceId, out DiceView view) => _viewsByDiceId.TryGetValue(diceId, out view);

        internal void ReconcileDiceVisualState(DiceInstanceId diceId)
        {
            if (!_viewsByDiceId.TryGetValue(diceId, out DiceView view))
                throw new InvalidOperationException($"Tracked dice {diceId} does not have a spawned DiceView.");
            ReconcileOne(diceId, view, false, false);
        }

        internal void ApplyPredictiveDecayPreview(DecayPreviewResult preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            ClearPredictiveDecayPresentation();
            for (int i = 0; i < preview.Pairs.Count; i++)
            {
                ApplyPredictiveSide(preview.Pairs[i].Enemy);
                ApplyPredictiveSide(preview.Pairs[i].Player);
            }
        }

        internal void ClearPredictiveDecayPresentation()
        {
            foreach (DiceView view in _viewsByDiceId.Values)
                view.ClearPredictiveDecayPresentation();
        }

        internal void CancelAllPresentation()
        {
            foreach (DiceView view in _viewsByDiceId.Values)
                view.CancelAllPresentation();
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                CancelSlotPresentation(new SlotId(Side.Enemy, number));
                CancelSlotPresentation(new SlotId(Side.Player, number));
            }
        }

        private DiceView ResolveViewPrefab(DiceDefinition definition)
        {
            if (definition.ViewPrefab == null)
                return _defaultDiceViewPrefab;
            DiceView diceView = definition.ViewPrefab.GetComponent<DiceView>();
            if (diceView == null)
                throw new InvalidOperationException($"Dice definition {definition.Id} ViewPrefab does not contain a DiceView component.");
            return diceView;
        }

        private void ReconcileOne(DiceInstanceId diceId, DiceView view, bool snapRenderedTransform, bool invokeRecoveryHook)
        {
            DiceRuntimeState diceState = _runtime.BattleInventoryState.GetDice(diceId);
            DiceDefinition definition = _diceCatalog.GetRequired(diceState.DefinitionId);

            if (diceState.IsDecayedForCurrentGame)
            {
                view.SetVisualContent(ResolveSprite(definition, diceState, false), false);
                view.SetInteractionSurfaceEnabled(false);
                if (snapRenderedTransform) view.ReconcileAuthoritativePresentation(invokeRecoveryHook);
                return;
            }

            if (_runtime.BoardState.TryGetSlotOfDice(diceId, out SlotId slotId))
            {
                view.SetPresentationDestination(_layout.GetBoardDiceDestination(slotId));
                view.SetVisualContent(ResolveSprite(definition, diceState, true), true);
                // Board visibility and interaction are separate. Movement permission is still decided by MoveDice Gates.
                view.SetInteractionSurfaceEnabled(true);
                if (snapRenderedTransform) view.ReconcileAuthoritativePresentation(invokeRecoveryHook);
                return;
            }

            if (_runtime.BattleInventoryState.IsInInventory(diceId))
            {
                if (diceState.Owner == Side.Enemy)
                {
                    view.SetVisualContent(ResolveSprite(definition, diceState, false), false);
                    view.SetInteractionSurfaceEnabled(false);
                    if (snapRenderedTransform) view.ReconcileAuthoritativePresentation(invokeRecoveryHook);
                    return;
                }

                IReadOnlyList<DiceInstanceId> inventoryDiceIds = _runtime.BattleInventoryState.InventoryDiceIds(Side.Player);
                int displayIndex = IndexOf(inventoryDiceIds, diceId);
                view.SetPresentationDestination(_layout.GetPlayerInventoryDiceDestination(displayIndex, inventoryDiceIds.Count));
                view.SetVisualContent(ResolveSprite(definition, diceState, false), true);
                view.SetInteractionSurfaceEnabled(true);
                if (snapRenderedTransform) view.ReconcileAuthoritativePresentation(invokeRecoveryHook);
                return;
            }

            view.SetVisualContent(ResolveSprite(definition, diceState, false), false);
            view.SetInteractionSurfaceEnabled(false);
            if (snapRenderedTransform) view.ReconcileAuthoritativePresentation(invokeRecoveryHook);
        }

        private void ReconcileSlotConditions(bool invokeRecoveryHooks)
        {
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ReconcileSlotCondition(new SlotId(Side.Enemy, number), invokeRecoveryHooks);
                ReconcileSlotCondition(new SlotId(Side.Player, number), invokeRecoveryHooks);
            }
        }

        private void ReconcileSlotCondition(SlotId slotId, bool invokeRecoveryHook)
        {
            if (_layout.TryGetSlotView(slotId, out SlotView slotView))
                slotView.ReconcileCondition(_runtime.BoardState.GetSlot(slotId).Condition, invokeRecoveryHook);
        }

        private void CancelSlotPresentation(SlotId slotId)
        {
            if (_layout.TryGetSlotView(slotId, out SlotView slotView))
                slotView.CancelAllPresentation();
        }

        private void ApplyPredictiveSide(DecayPreviewSide side)
        {
            if (!side.HasDice || !side.DiceId.IsValid) return;
            if (_viewsByDiceId.TryGetValue(side.DiceId, out DiceView view))
                view.SetPredictiveDecayPresentation(side.IsTargeted, side.IsWillDecay, side.WillCreateSave);
        }

        private static Sprite ResolveSprite(DiceDefinition definition, DiceRuntimeState diceState, bool isOnBoard)
        {
            if (diceState.HasCurrentFace
                && definition.TryGetFace(diceState.CurrentFaceIndex, out DiceFaceDefinition face)
                && face.Sprite != null)
                return face.Sprite;

            Sprite preferred = isOnBoard ? definition.BoardSprite : definition.InventorySprite;
            if (preferred != null) return preferred;
            return isOnBoard ? definition.InventorySprite : definition.BoardSprite;
        }

        private static int IndexOf(IReadOnlyList<DiceInstanceId> diceIds, DiceInstanceId diceId)
        {
            for (int i = 0; i < diceIds.Count; i++)
                if (diceIds[i] == diceId) return i;
            throw new InvalidOperationException($"Dice {diceId} is reported in Battle Inventory but has no presentation index.");
        }
    }
}
