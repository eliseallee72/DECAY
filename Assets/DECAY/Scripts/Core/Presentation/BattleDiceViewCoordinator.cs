using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Creates one DiceView per tracked battle dice and reconciles those views from authoritative state.
    /// Immediate reconciliation is the safe fallback presentation path; later animated presentation may
    /// delay between Facts but must still converge to the same authoritative result.
    /// </summary>
    public sealed class BattleDiceViewCoordinator
    {
        private readonly BattleRuntime _runtime;
        private readonly DiceCatalog _diceCatalog;
        private readonly DiceView _defaultDiceViewPrefab;
        private readonly Transform _diceViewRoot;
        private readonly BattleSceneDiceLayout _layout;
        private readonly Dictionary<DiceInstanceId, DiceView> _viewsByDiceId = new Dictionary<DiceInstanceId, DiceView>();
        private readonly Dictionary<SlotId, GameObject> _brokenSlotMarkers = new Dictionary<SlotId, GameObject>();

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
            {
                throw new ArgumentException(catalogError, nameof(diceCatalog));
            }

            if (!_layout.TryValidate(out string layoutError))
            {
                throw new ArgumentException(layoutError, nameof(layout));
            }
        }

        public int SpawnedViewCount => _viewsByDiceId.Count;

        public void SpawnTrackedDiceViews()
        {
            IReadOnlyList<DiceInstanceId> trackedDiceIds = _runtime.BattleInventoryState.TrackedDiceIds;
            var spawnPlan = new List<(DiceInstanceId DiceId, DiceView Prefab)>();

            // Validate the complete presentation plan before creating scene objects. A bad definition
            // must fail before the coordinator leaves behind a partially spawned battle roster.
            for (int i = 0; i < trackedDiceIds.Count; i++)
            {
                DiceInstanceId diceId = trackedDiceIds[i];
                if (_viewsByDiceId.ContainsKey(diceId))
                {
                    continue;
                }

                DiceRuntimeState diceState = _runtime.BattleInventoryState.GetDice(diceId);
                DiceDefinition definition = _diceCatalog.GetRequired(diceState.DefinitionId);
                DiceView prefab = ResolveViewPrefab(definition);
                if (prefab.SpriteRenderer == null)
                {
                    throw new InvalidOperationException($"Dice definition {definition.Id} resolves to a DiceView prefab without a SpriteRenderer reference.");
                }

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

        public void ReconcileAll()
        {
            IReadOnlyList<DiceInstanceId> trackedDiceIds = _runtime.BattleInventoryState.TrackedDiceIds;
            for (int i = 0; i < trackedDiceIds.Count; i++)
            {
                DiceInstanceId diceId = trackedDiceIds[i];
                if (!_viewsByDiceId.TryGetValue(diceId, out DiceView view))
                {
                    throw new InvalidOperationException($"Tracked dice {diceId} does not have a spawned DiceView.");
                }

                ReconcileOne(diceId, view);
            }

            ReconcileBrokenSlotMarkers(Side.Enemy);
            ReconcileBrokenSlotMarkers(Side.Player);
        }

        public bool TryGetView(DiceInstanceId diceId, out DiceView view)
        {
            return _viewsByDiceId.TryGetValue(diceId, out view);
        }

        private DiceView ResolveViewPrefab(DiceDefinition definition)
        {
            if (definition.ViewPrefab == null)
            {
                return _defaultDiceViewPrefab;
            }

            DiceView diceView = definition.ViewPrefab.GetComponent<DiceView>();
            if (diceView == null)
            {
                throw new InvalidOperationException($"Dice definition {definition.Id} ViewPrefab does not contain a DiceView component.");
            }

            return diceView;
        }

        private void ReconcileOne(DiceInstanceId diceId, DiceView view)
        {
            DiceRuntimeState diceState = _runtime.BattleInventoryState.GetDice(diceId);
            DiceDefinition definition = _diceCatalog.GetRequired(diceState.DefinitionId);

            if (diceState.IsDecayedForCurrentGame)
            {
                view.SetPresentation(ResolveSprite(definition, diceState, false), view.transform.position, false);
                return;
            }

            if (_runtime.BoardState.TryGetSlotOfDice(diceId, out SlotId slotId))
            {
                view.SetPresentation(
                    ResolveSprite(definition, diceState, true),
                    _layout.GetBoardDicePosition(slotId),
                    true);
                return;
            }

            if (_runtime.BattleInventoryState.IsInInventory(diceId))
            {
                if (diceState.Owner == Side.Enemy)
                {
                    // Enemy Battle Inventory is authoritative gameplay state but intentionally has no visible row.
                    // Enemy dice become visible only when an approved movement places them on the Board.
                    view.SetPresentation(ResolveSprite(definition, diceState, false), view.transform.position, false);
                    return;
                }

                IReadOnlyList<DiceInstanceId> inventoryDiceIds = _runtime.BattleInventoryState.InventoryDiceIds(Side.Player);
                int displayIndex = IndexOf(inventoryDiceIds, diceId);
                view.SetPresentation(
                    ResolveSprite(definition, diceState, false),
                    _layout.GetPlayerInventoryDicePosition(displayIndex, inventoryDiceIds.Count),
                    true);
                return;
            }

            // A tracked dice can intentionally be outside Board/Inventory while DECAY or another future
            // process owns its transition. Until presentation for that process is implemented, hide it.
            view.SetPresentation(ResolveSprite(definition, diceState, false), view.transform.position, false);
        }

        private void ReconcileBrokenSlotMarkers(Side side)
        {
            for (int slotNumber = BattleRules.FirstSlotNumber; slotNumber <= BattleRules.LastSlotNumber; slotNumber++)
            {
                var slotId = new SlotId(side, slotNumber);
                bool shouldShow = _runtime.BoardState.GetSlot(slotId).Condition != SlotCondition.Unbroken;

                if (!_brokenSlotMarkers.TryGetValue(slotId, out GameObject marker))
                {
                    if (!shouldShow)
                    {
                        continue;
                    }

                    marker = CreateBareBrokenSlotMarker(slotId);
                    _brokenSlotMarkers.Add(slotId, marker);
                }

                marker.SetActive(shouldShow);
            }
        }

        private GameObject CreateBareBrokenSlotMarker(SlotId slotId)
        {
            // This intentionally uses primitive geometry rather than a new authored asset or prefab so the
            // current migration build visibly communicates an unusable slot with zero scene wiring. It is a
            // presentation-only fallback and can later be replaced by the final broken-slot sprite/animation.
            var marker = new GameObject($"BareBrokenSlot_{slotId}");
            marker.transform.position = _layout.GetBrokenSlotMarkerPosition(slotId);
            marker.transform.SetParent(_diceViewRoot, true);

            CreateMarkerBar(marker.transform, 45f);
            CreateMarkerBar(marker.transform, -45f);
            return marker;
        }

        private void CreateMarkerBar(Transform markerRoot, float angleDegrees)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            bar.transform.SetParent(markerRoot, false);
            bar.transform.localPosition = Vector3.zero;
            bar.transform.localRotation = Quaternion.Euler(0f, angleDegrees, 0f);
            bar.transform.localScale = new Vector3(
                _layout.BrokenSlotMarkerLength,
                _layout.BrokenSlotMarkerThickness,
                _layout.BrokenSlotMarkerWidth);

            Collider collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                // The marker is visual only and must never intercept dice or hourglass raycasts.
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }
        }

        private static Sprite ResolveSprite(DiceDefinition definition, DiceRuntimeState diceState, bool isOnBoard)
        {
            if (diceState.HasCurrentFace
                && definition.TryGetFace(diceState.CurrentFaceIndex, out DiceFaceDefinition face)
                && face.Sprite != null)
            {
                return face.Sprite;
            }

            Sprite preferred = isOnBoard ? definition.BoardSprite : definition.InventorySprite;
            if (preferred != null)
            {
                return preferred;
            }

            return isOnBoard ? definition.InventorySprite : definition.BoardSprite;
        }

        private static int IndexOf(IReadOnlyList<DiceInstanceId> diceIds, DiceInstanceId diceId)
        {
            for (int i = 0; i < diceIds.Count; i++)
            {
                if (diceIds[i] == diceId)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Dice {diceId} is reported in Battle Inventory but has no presentation index.");
        }
    }
}
