using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Unity scene composition root for one battle. It converts inspector-authored startup data into the
    /// plain-C# battle runtime, then binds that runtime to presentation objects. It does not own rules.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class BattleCompositionRoot : MonoBehaviour
    {
        [Serializable]
        private sealed class PlayerRosterEntry
        {
            [SerializeField] private OwnedDiceId _ownedDiceId;
            [SerializeField] private DiceId _definitionId;

            public OwnedDiceId OwnedDiceId => _ownedDiceId;
            public DiceId DefinitionId => _definitionId;
        }

        [Header("Battle Data")]
        [SerializeField] private BattleConfig _battleConfig;
        [SerializeField] private DiceCatalog _diceCatalog;
        [SerializeField] private List<PlayerRosterEntry> _playerRoster = new List<PlayerRosterEntry>();
        [SerializeField] private List<DiceId> _enemyRoster = new List<DiceId>();

        [Header("Randomness")]
        [SerializeField] private int _normalRollSeed = 12345;
        [SerializeField] private int _fallbackRollSeed = 67890;

        [Header("Presentation")]
        [SerializeField] private DiceView _defaultDiceViewPrefab;
        [SerializeField] private Transform _diceViewRoot;
        [SerializeField] private BattleSceneDiceLayout _diceLayout;
        [SerializeField] private BattleDiceInputController _diceInputController;

        [Header("Startup")]
        [SerializeField] private bool _initializeOnAwake = true;

        private BattleRuntime _runtime;
        private BattleDiceViewCoordinator _viewCoordinator;

        public bool IsInitialized => _runtime != null;
        public BattleRuntime Runtime => _runtime ?? throw new InvalidOperationException("BattleCompositionRoot has not been initialized.");

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeFromInspector();
            }
        }

        public void Initialize(
            GlobalInventoryState globalInventory,
            IEnumerable<OwnedDiceId> selectedPlayerDiceIds,
            IEnumerable<DiceRuntimeSeed> enemyDiceSeeds,
            IRandomSource primaryRandomSource,
            IRandomSource fallbackRandomSource)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("BattleCompositionRoot can only initialize one battle runtime.");
            }

            RequireSceneConfiguration();
            var bootstrapper = new BattleBootstrapper();
            BattleRuntime runtime = bootstrapper.Create(
                _battleConfig,
                globalInventory,
                selectedPlayerDiceIds,
                enemyDiceSeeds,
                primaryRandomSource,
                fallbackRandomSource);

            var viewCoordinator = new BattleDiceViewCoordinator(
                runtime,
                _diceCatalog,
                _defaultDiceViewPrefab,
                _diceViewRoot,
                _diceLayout);
            viewCoordinator.SpawnTrackedDiceViews();

            // Publish the assembled runtime only after scene configuration and view creation succeed.
            // This avoids presenting a partially initialized BattleCompositionRoot as ready for play.
            _runtime = runtime;
            _viewCoordinator = viewCoordinator;

            if (_diceInputController != null)
            {
                _diceInputController.Bind(this, _viewCoordinator, _diceLayout);
            }
        }

        public MoveDiceResult RequestPlayerMove(DiceInstanceId diceId, MoveDiceTarget target)
        {
            MoveDiceResult result = Runtime.MoveDiceController.RequestMove(
                new MoveDiceRequest(Side.Player, diceId, target));
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteEnemySetup()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteEnemySetup();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult RequestRoll()
        {
            BattleFlowResult result = Runtime.BattleController.RequestRoll();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteRoll()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteRoll();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteEnemyReposition()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteEnemyReposition();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult RequestDecay()
        {
            BattleFlowResult result = Runtime.BattleController.RequestDecay();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteDecay()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteDecay();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteScore()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteScore();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteRoundEnd()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteRoundEnd();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public BattleFlowResult CompleteGameEnd()
        {
            BattleFlowResult result = Runtime.BattleController.CompleteGameEnd();
            _viewCoordinator.ReconcileAll();
            return result;
        }

        public void ReconcileViews()
        {
            _viewCoordinator.ReconcileAll();
        }

        internal bool TryGetDiceView(DiceInstanceId diceId, out DiceView view)
        {
            view = null;
            return _viewCoordinator != null && _viewCoordinator.TryGetView(diceId, out view);
        }

        internal void ConfigureForTests(
            BattleConfig battleConfig,
            DiceCatalog diceCatalog,
            DiceView defaultDiceViewPrefab,
            Transform diceViewRoot,
            BattleSceneDiceLayout diceLayout,
            BattleDiceInputController diceInputController = null)
        {
            _initializeOnAwake = false;
            _battleConfig = battleConfig;
            _diceCatalog = diceCatalog;
            _defaultDiceViewPrefab = defaultDiceViewPrefab;
            _diceViewRoot = diceViewRoot;
            _diceLayout = diceLayout;
            _diceInputController = diceInputController;
        }

        private void InitializeFromInspector()
        {
            RequireSceneConfiguration();

            var globalDice = new List<GlobalDiceState>(_playerRoster.Count);
            var selectedPlayerDiceIds = new List<OwnedDiceId>(_playerRoster.Count);
            var seenOwnedIds = new HashSet<OwnedDiceId>();

            for (int i = 0; i < _playerRoster.Count; i++)
            {
                PlayerRosterEntry entry = _playerRoster[i];
                if (entry == null || !entry.OwnedDiceId.IsValid || !entry.DefinitionId.IsValid)
                {
                    throw new InvalidOperationException($"Player roster entry {i + 1} is incomplete.");
                }

                if (!seenOwnedIds.Add(entry.OwnedDiceId))
                {
                    throw new InvalidOperationException($"Player roster repeats owned dice {entry.OwnedDiceId}.");
                }

                DiceDefinition definition = _diceCatalog.GetRequired(entry.DefinitionId);
                globalDice.Add(new GlobalDiceState(entry.OwnedDiceId, DiceRuntimeSeed.FromDefinition(definition)));
                selectedPlayerDiceIds.Add(entry.OwnedDiceId);
            }

            var enemySeeds = new List<DiceRuntimeSeed>(_enemyRoster.Count);
            for (int i = 0; i < _enemyRoster.Count; i++)
            {
                DiceId definitionId = _enemyRoster[i];
                if (!definitionId.IsValid)
                {
                    throw new InvalidOperationException($"Enemy roster entry {i + 1} has an invalid dice definition ID.");
                }

                enemySeeds.Add(DiceRuntimeSeed.FromDefinition(_diceCatalog.GetRequired(definitionId)));
            }

            Initialize(
                new GlobalInventoryState(globalDice),
                selectedPlayerDiceIds,
                enemySeeds,
                new SeededRandomSource(_normalRollSeed),
                new SeededRandomSource(_fallbackRollSeed));
        }

        private void RequireSceneConfiguration()
        {
            if (_battleConfig == null)
            {
                throw new InvalidOperationException($"{name}: Battle Config is missing.");
            }

            if (_diceCatalog == null)
            {
                throw new InvalidOperationException($"{name}: Dice Catalog is missing.");
            }

            if (_defaultDiceViewPrefab == null)
            {
                throw new InvalidOperationException($"{name}: default DiceView prefab is missing.");
            }

            if (_diceViewRoot == null)
            {
                throw new InvalidOperationException($"{name}: DiceViewRoot is missing.");
            }

            if (_diceLayout == null)
            {
                throw new InvalidOperationException($"{name}: BattleSceneDiceLayout is missing.");
            }

            if (!_battleConfig.TryValidate(out string configError))
            {
                throw new InvalidOperationException(configError);
            }

            if (!_diceCatalog.TryValidate(out string catalogError))
            {
                throw new InvalidOperationException(catalogError);
            }

            if (!_diceLayout.TryValidate(out string layoutError))
            {
                throw new InvalidOperationException(layoutError);
            }

            if (_diceInputController != null && !_diceInputController.TryValidate(out string inputError))
            {
                throw new InvalidOperationException(inputError);
            }
        }
    }
}
