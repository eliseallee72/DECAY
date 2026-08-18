
using UnityEngine;

namespace Decay
{
    [CreateAssetMenu(fileName = "conf_BATTLE_New", menuName = "DECAY/Battle/Battle Config")]
    public sealed class BattleConfig : ScriptableObject
    {
        [Header("Battle Structure")]
        [SerializeField, Min(1)] private int _gamesPerBattle = 2;
        [SerializeField, Min(1)] private int _roundsPerGame = 4;

        [Header("Inventory")]
        [SerializeField, Min(1)] private int _battleInventoryCapacity = 10;

        public int GamesPerBattle => _gamesPerBattle;
        public int RoundsPerGame => _roundsPerGame;
        public int BattleInventoryCapacity => _battleInventoryCapacity;

        public bool TryValidate(out string error)
        {
            if (_gamesPerBattle < 1)
            {
                error = "Games per battle must be at least 1.";
                return false;
            }

            if (_roundsPerGame < 1)
            {
                error = "Rounds per game must be at least 1.";
                return false;
            }

            if (_battleInventoryCapacity < 1)
            {
                error = "Battle inventory capacity must be at least 1.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            _gamesPerBattle = Mathf.Max(1, _gamesPerBattle);
            _roundsPerGame = Mathf.Max(1, _roundsPerGame);
            _battleInventoryCapacity = Mathf.Max(1, _battleInventoryCapacity);
        }
    }
}
