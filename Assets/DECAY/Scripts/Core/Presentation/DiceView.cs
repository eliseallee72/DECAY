using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation for one battle dice. The bound DiceInstanceId identifies the authoritative runtime
    /// state elsewhere; this component never owns board location, inventory membership, or rule results.
    /// </summary>
    public sealed class DiceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider _interactionCollider;

        private DiceInstanceId _diceId;

        public DiceInstanceId DiceId => _diceId;
        public bool IsBound => _diceId.IsValid;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public void Bind(DiceInstanceId diceId)
        {
            if (!diceId.IsValid)
            {
                throw new ArgumentException("DiceView requires a valid DiceInstanceId.", nameof(diceId));
            }

            if (IsBound && _diceId != diceId)
            {
                throw new InvalidOperationException($"DiceView is already bound to dice {_diceId}.");
            }

            _diceId = diceId;
        }

        internal void SetPresentation(Sprite sprite, Vector3 worldPosition, bool isVisible)
        {
            RequireConfigured();
            _spriteRenderer.sprite = sprite;
            transform.position = worldPosition;
            _spriteRenderer.enabled = isVisible;
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = isVisible;
            }
        }

        internal void SetPreviewWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        internal void ConfigureForTests(SpriteRenderer spriteRenderer, Collider interactionCollider)
        {
            _spriteRenderer = spriteRenderer;
            _interactionCollider = interactionCollider;
        }

        private void Awake()
        {
            RequireConfigured();
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_interactionCollider == null)
            {
                _interactionCollider = GetComponent<Collider>();
            }
        }

        private void RequireConfigured()
        {
            if (_spriteRenderer == null)
            {
                throw new InvalidOperationException($"{name}: DiceView requires a SpriteRenderer reference.");
            }
        }
    }
}
