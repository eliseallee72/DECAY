using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation anchors for dice. These transforms describe where views are drawn;
    /// they never establish gameplay occupancy or Battle Inventory membership.
    /// Enemy Battle Inventory intentionally has no visible presentation anchor.
    /// </summary>
    public sealed class BattleSceneDiceLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class SlotAnchor
        {
            [SerializeField] private SlotId _slotId;
            [SerializeField] private Transform _anchor;

            public SlotId SlotId => _slotId;
            public Transform Anchor => _anchor;

            public SlotAnchor()
            {
            }

            public SlotAnchor(SlotId slotId, Transform anchor)
            {
                _slotId = slotId;
                _anchor = anchor;
            }
        }

        [Header("Board")]
        [SerializeField] private List<SlotAnchor> _slotAnchors = new List<SlotAnchor>();
        [SerializeField] private float _boardDiceHeight = 0.12f;

        [Header("Bare Broken Slot Presentation")]
        [SerializeField, Min(0f)] private float _brokenSlotMarkerHeight = 0.07f;
        [SerializeField, Min(0.01f)] private float _brokenSlotMarkerLength = 1.1f;
        [SerializeField, Min(0.01f)] private float _brokenSlotMarkerWidth = 0.12f;
        [SerializeField, Min(0.005f)] private float _brokenSlotMarkerThickness = 0.03f;

        [Header("Player Battle Inventory Presentation")]
        [SerializeField] private Transform _playerInventoryAnchor;
        [SerializeField] private Vector3 _playerInventorySpacing = new Vector3(1.7f, 0f, 0f);
        [SerializeField] private Collider _playerInventoryDropCollider;

        private readonly Dictionary<SlotId, Transform> _anchorsBySlot = new Dictionary<SlotId, Transform>();
        private readonly Dictionary<Transform, SlotId> _slotsByTransform = new Dictionary<Transform, SlotId>();
        private bool _isIndexed;

        public float BrokenSlotMarkerLength => _brokenSlotMarkerLength;
        public float BrokenSlotMarkerWidth => _brokenSlotMarkerWidth;
        public float BrokenSlotMarkerThickness => _brokenSlotMarkerThickness;

        public Vector3 GetBoardDicePosition(SlotId slotId)
        {
            return GetRequiredBoardAnchor(slotId).position + (Vector3.up * _boardDiceHeight);
        }

        public Vector3 GetBrokenSlotMarkerPosition(SlotId slotId)
        {
            return GetRequiredBoardAnchor(slotId).position + (Vector3.up * _brokenSlotMarkerHeight);
        }

        public Vector3 GetPlayerInventoryDicePosition(int displayIndex, int displayCount)
        {
            if (displayIndex < 0 || displayIndex >= displayCount)
            {
                throw new ArgumentOutOfRangeException(nameof(displayIndex), displayIndex, "Inventory display index must be within the displayed collection.");
            }

            if (displayCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(displayCount), displayCount, "Inventory display count must be at least one.");
            }

            if (_playerInventoryAnchor == null)
            {
                throw new InvalidOperationException("Player Battle Inventory presentation anchor is not configured.");
            }

            float centeredIndex = displayIndex - ((displayCount - 1) * 0.5f);
            return _playerInventoryAnchor.position + (_playerInventorySpacing * centeredIndex);
        }

        public bool TryGetBoardSlot(Collider collider, out SlotId slotId)
        {
            EnsureIndex();
            if (collider != null && _slotsByTransform.TryGetValue(collider.transform, out slotId))
            {
                return true;
            }

            slotId = default;
            return false;
        }

        public bool IsPlayerInventoryDrop(Collider collider)
        {
            return collider != null && _playerInventoryDropCollider != null && collider == _playerInventoryDropCollider;
        }

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
                {
                    _slotAnchors.Add(new SlotAnchor(item.SlotId, item.Anchor));
                }
            }

            _playerInventoryAnchor = playerInventoryAnchor;
            _playerInventoryDropCollider = playerInventoryDropCollider;
            _isIndexed = false;
        }

        private void OnValidate()
        {
            _isIndexed = false;
        }

        private Transform GetRequiredBoardAnchor(SlotId slotId)
        {
            EnsureIndex();
            if (!_anchorsBySlot.TryGetValue(slotId, out Transform anchor))
            {
                throw new KeyNotFoundException($"No presentation anchor is configured for slot {slotId}.");
            }

            return anchor;
        }

        private void EnsureIndex()
        {
            if (!_isIndexed)
            {
                RebuildIndex();
            }
        }

        private void RebuildIndex()
        {
            _anchorsBySlot.Clear();
            _slotsByTransform.Clear();

            if (_slotAnchors == null || _slotAnchors.Count != BattleRules.SlotsPerSide * 2)
            {
                throw new InvalidOperationException($"Battle scene layout requires exactly {BattleRules.SlotsPerSide * 2} slot anchors.");
            }

            for (int i = 0; i < _slotAnchors.Count; i++)
            {
                SlotAnchor entry = _slotAnchors[i];
                if (entry == null || !entry.SlotId.IsValid || entry.Anchor == null)
                {
                    throw new InvalidOperationException($"Slot presentation anchor {i + 1} is incomplete.");
                }

                if (_anchorsBySlot.ContainsKey(entry.SlotId))
                {
                    throw new InvalidOperationException($"Slot {entry.SlotId} has more than one presentation anchor.");
                }

                if (_slotsByTransform.ContainsKey(entry.Anchor))
                {
                    throw new InvalidOperationException("One Transform cannot present more than one board slot.");
                }

                _anchorsBySlot.Add(entry.SlotId, entry.Anchor);
                _slotsByTransform.Add(entry.Anchor, entry.SlotId);
            }

            _isIndexed = true;
        }
    }
}
