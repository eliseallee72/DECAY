using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// World-space pointer adapter for Player dice drag/drop. Drag motion is presentation-only;
    /// releasing creates a normal MoveDiceRequest and authoritative state decides the final location.
    /// </summary>
    public sealed class BattleDiceInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _raycastMask = ~0;
        [SerializeField] private float _dragLift = 0.08f;

        private BattleCompositionRoot _compositionRoot;
        private BattleDiceViewCoordinator _viewCoordinator;
        private BattleSceneDiceLayout _layout;
        private DiceView _draggedView;
        private Plane _dragPlane;
        private Vector3 _dragOffset;

        public bool TryValidate(out string error)
        {
            if (_camera == null)
            {
                error = $"{name}: BattleDiceInputController requires a Camera reference.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void Bind(
            BattleCompositionRoot compositionRoot,
            BattleDiceViewCoordinator viewCoordinator,
            BattleSceneDiceLayout layout)
        {
            _compositionRoot = compositionRoot ?? throw new ArgumentNullException(nameof(compositionRoot));
            _viewCoordinator = viewCoordinator ?? throw new ArgumentNullException(nameof(viewCoordinator));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            if (!TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }
        }

        internal void ConfigureForTests(Camera camera)
        {
            _camera = camera;
        }

        private void Update()
        {
            if (_compositionRoot == null || Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryBeginDrag(Mouse.current.position.ReadValue());
            }

            if (_draggedView != null && Mouse.current.leftButton.isPressed)
            {
                UpdateDrag(Mouse.current.position.ReadValue());
            }

            if (_draggedView != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                CompleteDrag(Mouse.current.position.ReadValue());
            }
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, _camera.farClipPlane, _raycastMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                DiceView view = hits[i].collider.GetComponent<DiceView>();
                if (view == null || !view.IsBound)
                {
                    continue;
                }

                DiceRuntimeState diceState = _compositionRoot.Runtime.BattleInventoryState.GetDice(view.DiceId);
                if (diceState.Owner != Side.Player)
                {
                    // Enemy board dice may overlap Player dice in an angled 2.5D camera ray. They are not
                    // draggable by the Player, but they also must not prevent searching deeper hits for
                    // a valid Player DiceView.
                    continue;
                }

                _draggedView = view;
                _dragPlane = new Plane(Vector3.up, view.transform.position);
                if (_dragPlane.Raycast(ray, out float enter))
                {
                    _dragOffset = view.transform.position - ray.GetPoint(enter);
                }
                else
                {
                    _dragOffset = Vector3.zero;
                }

                return;
            }
        }

        private void UpdateDrag(Vector2 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter) + _dragOffset + (Vector3.up * _dragLift);
                _draggedView.SetPreviewWorldPosition(point);
            }
        }

        private void CompleteDrag(Vector2 screenPosition)
        {
            DiceView releasedView = _draggedView;
            _draggedView = null;

            if (!TryResolveDropTarget(screenPosition, out MoveDiceTarget target))
            {
                _viewCoordinator.ReconcileAll();
                return;
            }

            _compositionRoot.RequestPlayerMove(releasedView.DiceId, target);
        }

        private bool TryResolveDropTarget(Vector2 screenPosition, out MoveDiceTarget target)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, _camera.farClipPlane, _raycastMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (_layout.TryGetBoardSlot(collider, out SlotId slotId))
                {
                    target = MoveDiceTarget.Board(slotId);
                    return true;
                }

                if (_layout.IsPlayerInventoryDrop(collider))
                {
                    target = MoveDiceTarget.BattleInventory;
                    return true;
                }
            }

            target = default;
            return false;
        }
    }
}
