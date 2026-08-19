using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation anchors for semantic dice locations. Gameplay owns SlotId/Inventory membership;
    /// this component only resolves those semantic locations to scene-space destinations.
    /// </summary>
    public sealed class BattleSceneDiceLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class SlotAnchor
        {
            [SerializeField] private SlotId _slotId;
            [Tooltip("Scene transform used as the presentation origin for dice occupying this semantic SlotId.")]
            [SerializeField] private Transform _anchor;
            [Tooltip("Editor-authored local offset from the slot anchor. Use this for skewed/2.5D placement instead of coded world offsets.")]
            [SerializeField] private Vector3 _diceLocalOffset;
            [SerializeField] private SlotView _slotView;

            public SlotId SlotId => _slotId;
            public Transform Anchor => _anchor;
            public Vector3 DiceLocalOffset => _diceLocalOffset;
            public SlotView SlotView => _slotView;

            public SlotAnchor()
            {
            }

            public SlotAnchor(SlotId slotId, Transform anchor, Vector3 diceLocalOffset = default)
            {
                _slotId = slotId;
                _anchor = anchor;
                _diceLocalOffset = diceLocalOffset;
            }
        }

        [Header("Board Presentation Anchors")]
        [SerializeField] private List<SlotAnchor> _slotAnchors = new List<SlotAnchor>();

        [Header("Player Battle Inventory Presentation")]
        [Tooltip("Authored root anchor for the current temporary battle-inventory row. The later Inventory pass may replace this layout without changing gameplay state.")]
        [SerializeField] private Transform _playerInventoryAnchor;
        [Tooltip("Local-space spacing between temporary inventory dice positions. This is presentation data, not gameplay position.")]
        [SerializeField] private Vector3 _playerInventorySpacing = new Vector3(1.7f, 0f, 0f);
        [SerializeField] private Collider _playerInventoryDropCollider;

        private readonly Dictionary<SlotId, SlotAnchor> _anchorsBySlot = new Dictionary<SlotId, SlotAnchor>();
        private readonly Dictionary<Transform, SlotId> _slotsByTransform = new Dictionary<Transform, SlotId>();
        private bool _isIndexed;

        internal DicePresentationDestination GetBoardDiceDestination(SlotId slotId)
        {
            SlotAnchor entry = GetRequiredBoardAnchor(slotId);
            return new DicePresentationDestination(entry.Anchor, entry.DiceLocalOffset);
        }

        internal DicePresentationDestination GetPlayerInventoryDiceDestination(int displayIndex, int displayCount)
        {
            ValidateInventoryDisplayIndex(displayIndex, displayCount);
            if (_playerInventoryAnchor == null)
                throw new InvalidOperationException("Player Battle Inventory presentation anchor is not configured.");

            float centeredIndex = displayIndex - ((displayCount - 1) * 0.5f);
            return new DicePresentationDestination(_playerInventoryAnchor, _playerInventorySpacing * centeredIndex);
        }

        // Kept as read-only convenience APIs for tests/editor diagnostics. Runtime presentation should retain the
        // destination itself so the rendered transform can move independently from the authoritative location.
        public Vector3 GetBoardDicePosition(SlotId slotId) => GetBoardDiceDestination(slotId).WorldPosition;
        public Vector3 GetPlayerInventoryDicePosition(int displayIndex, int displayCount) =>
            GetPlayerInventoryDiceDestination(displayIndex, displayCount).WorldPosition;

        public bool TryGetSlotView(SlotId slotId, out SlotView slotView)
        {
            EnsureIndex();
            if (_anchorsBySlot.TryGetValue(slotId, out SlotAnchor entry))
            {
                slotView = entry.SlotView;
                return slotView != null;
            }

            slotView = null;
            return false;
        }

        public bool TryGetBoardSlot(Collider collider, out SlotId slotId)
        {
            EnsureIndex();
            if (collider != null && _slotsByTransform.TryGetValue(collider.transform, out slotId))
                return true;

            slotId = default;
            return false;
        }

        public bool IsPlayerInventoryDrop(Collider collider) =>
            collider != null && _playerInventoryDropCollider != null && collider == _playerInventoryDropCollider;

        public bool TryValidate(out string error)
        {
            try
            {
                RebuildIndex();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (_playerInventoryAnchor == null)
            {
                error = "Player Battle Inventory presentation anchor is missing.";
                return false;
            }

            if (_playerInventoryDropCollider == null)
            {
                error = "Player Battle Inventory drop collider is missing.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void ConfigureForTests(
            IEnumerable<(SlotId SlotId, Transform Anchor)> slotAnchors,
            Transform playerInventoryAnchor,
            Collider playerInventoryDropCollider)
        {
            _slotAnchors = new List<SlotAnchor>();
            if (slotAnchors != null)
            {
                foreach ((SlotId SlotId, Transform Anchor) item in slotAnchors)
                    _slotAnchors.Add(new SlotAnchor(item.SlotId, item.Anchor));
            }

            _playerInventoryAnchor = playerInventoryAnchor;
            _playerInventoryDropCollider = playerInventoryDropCollider;
            _isIndexed = false;
        }

        private void OnValidate()
        {
            _isIndexed = false;
        }

        private SlotAnchor GetRequiredBoardAnchor(SlotId slotId)
        {
            EnsureIndex();
            if (!_anchorsBySlot.TryGetValue(slotId, out SlotAnchor anchor))
                throw new KeyNotFoundException($"No presentation anchor is configured for slot {slotId}.");
            return anchor;
        }

        private void EnsureIndex()
        {
            if (!_isIndexed)
                RebuildIndex();
        }

        private void RebuildIndex()
        {
            _anchorsBySlot.Clear();
            _slotsByTransform.Clear();

            if (_slotAnchors == null || _slotAnchors.Count != BattleRules.SlotsPerSide * 2)
                throw new InvalidOperationException($"Battle scene layout requires exactly {BattleRules.SlotsPerSide * 2} slot anchors.");

            for (int i = 0; i < _slotAnchors.Count; i++)
            {
                SlotAnchor entry = _slotAnchors[i];
                if (entry == null || !entry.SlotId.IsValid || entry.Anchor == null)
                    throw new InvalidOperationException($"Slot presentation anchor {i + 1} is incomplete.");
                if (_anchorsBySlot.ContainsKey(entry.SlotId))
                    throw new InvalidOperationException($"Slot {entry.SlotId} has more than one presentation anchor.");
                if (_slotsByTransform.ContainsKey(entry.Anchor))
                    throw new InvalidOperationException("One Transform cannot present more than one board slot.");
                if (entry.SlotView != null && !entry.SlotView.TryValidate(out string slotError))
                    throw new InvalidOperationException(slotError);

                _anchorsBySlot.Add(entry.SlotId, entry);
                _slotsByTransform.Add(entry.Anchor, entry.SlotId);
            }

            _isIndexed = true;
        }

        private static void ValidateInventoryDisplayIndex(int displayIndex, int displayCount)
        {
            if (displayIndex < 0 || displayIndex >= displayCount)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), displayIndex, "Inventory display index must be within the displayed collection.");
            if (displayCount < 1)
                throw new ArgumentOutOfRangeException(nameof(displayCount), displayCount, "Inventory display count must be at least one.");
        }
    }
}
