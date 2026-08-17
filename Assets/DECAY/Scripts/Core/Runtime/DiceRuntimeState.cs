
using System;
using System.Collections.Generic;

namespace Decay
{
    public sealed class DiceRuntimeState
    {
        private readonly List<DiceFaceRuntimeState> _faces = new List<DiceFaceRuntimeState>();
        private readonly Dictionary<int, DiceFaceRuntimeState> _facesByIndex = new Dictionary<int, DiceFaceRuntimeState>();
        private readonly HashSet<DiceTagId> _tags = new HashSet<DiceTagId>();
        private readonly List<EffectDefinition> _effects = new List<EffectDefinition>();
        private readonly EffectRuntimeStateCollection _effectRuntimeStates = new EffectRuntimeStateCollection();

        private bool _hasCurrentFace;
        private int _currentFaceIndex;
        private bool _isDecayedForCurrentGame;

        private DiceRuntimeState(
            DiceInstanceId instanceId,
            Side owner,
            DiceRuntimeSeed seed,
            bool hasSourceOwnedDice,
            OwnedDiceId sourceOwnedDiceId)
        {
            if (!instanceId.IsValid)
            {
                throw new ArgumentException("A valid battle dice instance ID is required.", nameof(instanceId));
            }

            if (!Enum.IsDefined(typeof(Side), owner))
            {
                throw new ArgumentOutOfRangeException(nameof(owner), owner, "Side must be Enemy or Player.");
            }

            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            if (!seed.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(seed));
            }

            if (hasSourceOwnedDice && !sourceOwnedDiceId.IsValid)
            {
                throw new ArgumentException("A source-owned dice ID must be valid when supplied.", nameof(sourceOwnedDiceId));
            }

            InstanceId = instanceId;
            Owner = owner;
            DefinitionId = seed.DefinitionId;
            HasSourceOwnedDice = hasSourceOwnedDice;
            SourceOwnedDiceId = sourceOwnedDiceId;
            CopyBattleValuesFrom(seed);
        }

        public DiceInstanceId InstanceId { get; }
        public DiceId DefinitionId { get; }
        public Side Owner { get; }
        public bool HasSourceOwnedDice { get; }
        public OwnedDiceId SourceOwnedDiceId { get; }
        public int GeneralScoreValue { get; private set; }
        public IReadOnlyList<DiceFaceRuntimeState> Faces => _faces;
        public IReadOnlyCollection<DiceTagId> Tags => _tags;
        public IReadOnlyList<EffectDefinition> Effects => _effects;
        public bool HasCurrentFace => _hasCurrentFace;
        public int CurrentFaceIndex => _hasCurrentFace ? _currentFaceIndex : 0;
        public bool IsDecayedForCurrentGame => _isDecayedForCurrentGame;
        public int EffectRuntimeStateCount => _effectRuntimeStates.Count;

        public DiceFaceRuntimeState CurrentFace
        {
            get
            {
                if (!_hasCurrentFace)
                {
                    throw new InvalidOperationException("This dice does not have a current rolled face.");
                }

                return _facesByIndex[_currentFaceIndex];
            }
        }

        public int ActiveRollValue => CurrentFace.RollValue;
        public int ActiveFaceScoreValue => CurrentFace.ScoreValue;
        public int ActiveScoreContribution => GeneralScoreValue + ActiveFaceScoreValue;

        public static DiceRuntimeState CreatePlayerDice(
            DiceInstanceId instanceId,
            OwnedDiceId sourceOwnedDiceId,
            DiceDefinition definition)
        {
            return CreatePlayerDice(instanceId, sourceOwnedDiceId, DiceRuntimeSeed.FromDefinition(definition));
        }

        public static DiceRuntimeState CreatePlayerDice(
            DiceInstanceId instanceId,
            OwnedDiceId sourceOwnedDiceId,
            DiceRuntimeSeed seed)
        {
            return new DiceRuntimeState(instanceId, Side.Player, seed, true, sourceOwnedDiceId);
        }

        public static DiceRuntimeState CreateEnemyDice(
            DiceInstanceId instanceId,
            DiceDefinition definition)
        {
            return CreateEnemyDice(instanceId, DiceRuntimeSeed.FromDefinition(definition));
        }

        public static DiceRuntimeState CreateEnemyDice(
            DiceInstanceId instanceId,
            DiceRuntimeSeed seed)
        {
            return new DiceRuntimeState(instanceId, Side.Enemy, seed, false, default);
        }

        public bool HasTag(DiceTagId tagId) => _tags.Contains(tagId);

        public bool TryGetFace(int faceIndex, out DiceFaceRuntimeState face)
        {
            return _facesByIndex.TryGetValue(faceIndex, out face);
        }

        public bool TryGetEffectRuntimeState(EffectInstanceId instanceId, out IEffectRuntimeState state)
        {
            return _effectRuntimeStates.TryGet(instanceId, out state);
        }

        internal void SetCurrentFace(int faceIndex)
        {
            if (!_facesByIndex.ContainsKey(faceIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(faceIndex), faceIndex, "The face index is not present on this dice.");
            }

            _currentFaceIndex = faceIndex;
            _hasCurrentFace = true;
        }

        internal void ClearCurrentFace()
        {
            _currentFaceIndex = 0;
            _hasCurrentFace = false;
        }

        internal void SetGeneralScoreValue(int value)
        {
            GeneralScoreValue = value;
        }

        internal void MarkDecayedForCurrentGame()
        {
            _isDecayedForCurrentGame = true;
            ClearCurrentFace();
        }

        internal void AddTag(DiceTagId tagId)
        {
            if (!tagId.IsValid)
            {
                throw new ArgumentException("A valid tag ID is required.", nameof(tagId));
            }

            _tags.Add(tagId);
        }

        internal void RemoveTag(DiceTagId tagId)
        {
            _tags.Remove(tagId);
        }

        internal void RegisterEffectRuntimeState(IEffectRuntimeState state)
        {
            _effectRuntimeStates.Register(state);
        }

        internal void ResetFromSeed(DiceRuntimeSeed seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            if (seed.DefinitionId != DefinitionId)
            {
                throw new InvalidOperationException("A dice can only reset from data for its own definition.");
            }

            if (!seed.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(seed));
            }

            CopyBattleValuesFrom(seed);
            _effectRuntimeStates.Clear();
            _isDecayedForCurrentGame = false;
            ClearCurrentFace();
        }

        private void CopyBattleValuesFrom(DiceRuntimeSeed seed)
        {
            GeneralScoreValue = seed.GeneralScoreValue;

            _faces.Clear();
            _facesByIndex.Clear();
            for (int i = 0; i < seed.Faces.Count; i++)
            {
                var face = new DiceFaceRuntimeState(seed.Faces[i]);
                _faces.Add(face);
                _facesByIndex.Add(face.FaceIndex, face);
            }

            _tags.Clear();
            for (int i = 0; i < seed.Tags.Count; i++)
            {
                _tags.Add(seed.Tags[i]);
            }

            _effects.Clear();
            for (int i = 0; i < seed.Effects.Count; i++)
            {
                _effects.Add(seed.Effects[i]);
            }
        }
    }
}
