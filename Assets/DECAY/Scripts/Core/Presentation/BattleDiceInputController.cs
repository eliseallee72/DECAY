using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// World-space pointer adapter for Player dice drag/drop. Press/release are event-driven Input System callbacks;
    /// Update is reserved only for the genuinely continuous pointer-following portion of an active drag.
    /// Releasing creates a normal MoveDiceRequest and authoritative Gates decide the result.
    /// </summary>
    public sealed class BattleDiceInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _raycastMask = ~0;
        [Tooltip("Input System binding used to begin/end a drag. Stored as editor data rather than polled every frame.")]
        [SerializeField] private string _dragPressBindingPath = "<Mouse>/leftButton";
        [Tooltip("Editor-authored surface whose local Up defines drag-plane normal and lift direction in the skewed 2.5D scene.")]
        [SerializeField] private Transform _dragSurface;
        [SerializeField] private float _dragLift = 0.08f;

        private BattleCompositionRoot _compositionRoot;
        private BattleDiceViewCoordinator _viewCoordinator;
        private BattleSceneDiceLayout _layout;
        private DiceView _draggedView;
        private Plane _dragPlane;
        private Vector3 _dragOffset;
        private InputAction _dragPressAction;

        public bool TryValidate(out string error)
        {
            if (_camera == null)
            {
                error = $"{name}: BattleDiceInputController requires a Camera reference.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(_dragPressBindingPath))
            {
                error = $"{name}: BattleDiceInputController requires an Input System drag press binding path.";
                return false;
            }
            if (_dragSurface == null)
            {
                error = $"{name}: BattleDiceInputController requires an editor-authored Drag Surface reference.";
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
                throw new InvalidOperationException(error);
        }

        internal void ConfigureForTests(Camera camera, Transform dragSurface)
        {
            _camera = camera;
            _dragSurface = dragSurface;
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(_dragPressBindingPath))
                return;

            _dragPressAction = new InputAction($"{name}_DragPress", InputActionType.Button, _dragPressBindingPath);
            _dragPressAction.started += OnDragPressStarted;
            _dragPressAction.canceled += OnDragPressCanceled;
            _dragPressAction.Enable();
        }

        private void OnDisable()
        {
            if (_dragPressAction != null)
            {
                _dragPressAction.started -= OnDragPressStarted;
                _dragPressAction.canceled -= OnDragPressCanceled;
                _dragPressAction.Disable();
                _dragPressAction.Dispose();
                _dragPressAction = null;
            }

            if (_draggedView != null)
            {
                _draggedView = null;
                _viewCoordinator?.ReconcileAll(true);
            }
        }

        private void Update()
        {
            if (_draggedView == null || Pointer.current == null)
                return;

            UpdateDrag(Pointer.current.position.ReadValue());
        }

        private void OnDragPressStarted(InputAction.CallbackContext context)
        {
            if (_compositionRoot == null || Pointer.current == null)
                return;
            TryBeginDrag(Pointer.current.position.ReadValue());
        }

        private void OnDragPressCanceled(InputAction.CallbackContext context)
        {
            if (_draggedView == null || Pointer.current == null)
                return;
            CompleteDrag(Pointer.current.position.ReadValue());
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
                    continue;

                DiceRuntimeState diceState = _compositionRoot.Runtime.BattleInventoryState.GetDice(view.DiceId);
                if (diceState.Owner != Side.Player)
                    continue;

                _draggedView = view;
                _dragPlane = new Plane(_dragSurface.up, view.transform.position);
                _dragOffset = _dragPlane.Raycast(ray, out float enter)
                    ? view.transform.position - ray.GetPoint(enter)
                    : Vector3.zero;
                return;
            }
        }

        private void UpdateDrag(Vector2 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter) + _dragOffset + (_dragSurface.up * _dragLift);
                _draggedView.SetPreviewWorldPosition(point);
            }
        }

        private void CompleteDrag(Vector2 screenPosition)
        {
            DiceView releasedView = _draggedView;
            _draggedView = null;

            if (!TryResolveDropTarget(screenPosition, out MoveDiceTarget target))
            {
                _viewCoordinator.ReconcileAll(true);
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
