using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored board-wide presentation surface. It provides the Enemy Reposition cue hook without
    /// deciding enemy movement, interaction availability, board state, colors, or animation timing in code.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] private AnimatorTriggerPresentationBinding _enemyRepositionPresentation = new AnimatorTriggerPresentationBinding();
        private Action _enemyRepositionCompletion;

        public bool TryValidate(out string error) =>
            _enemyRepositionPresentation.TryValidate($"{name} Enemy Reposition", out error);

        internal void PlayEnemyRepositionPresentation(Action onCompleted)
        {
            _enemyRepositionCompletion = null;
            if (!_enemyRepositionPresentation.Play())
            {
                onCompleted?.Invoke();
                return;
            }
            _enemyRepositionCompletion = onCompleted;
        }

        public void NotifyEnemyRepositionPresentationComplete()
        {
            Action callback = _enemyRepositionCompletion;
            _enemyRepositionCompletion = null;
            callback?.Invoke();
        }

        internal void CancelAllPresentation()
        {
            _enemyRepositionCompletion = null;
            _enemyRepositionPresentation.Cancel();
        }

        private void OnDisable() => CancelAllPresentation();
    }
}
