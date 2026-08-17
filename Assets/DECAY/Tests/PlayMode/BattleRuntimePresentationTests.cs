using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Decay.Tests
{
    public sealed class BattleRuntimePresentationTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.Destroy(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Initialize_SpawnsOneIdBoundViewPerTrackedDice()
        {
            PresentationFixture fixture = CreateFixture();

            Assert.That(fixture.Root.Runtime.BattleInventoryState.TotalTrackedCount, Is.EqualTo(2));
            Assert.That(fixture.Root.TryGetDiceView(new DiceInstanceId(1), out DiceView playerView), Is.True);
            Assert.That(fixture.Root.TryGetDiceView(new DiceInstanceId(2), out DiceView enemyView), Is.True);
            Assert.That(playerView.DiceId, Is.EqualTo(new DiceInstanceId(1)));
            Assert.That(enemyView.DiceId, Is.EqualTo(new DiceInstanceId(2)));
            Assert.That(playerView.transform.parent, Is.SameAs(fixture.DiceViewRoot));
            Assert.That(enemyView.transform.parent, Is.SameAs(fixture.DiceViewRoot));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Initialize_InventoryViewsUsePresentationLayoutWithoutOwningMembership()
        {
            PresentationFixture fixture = CreateFixture();
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);

            Assert.That(fixture.Root.Runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.True);
            Assert.That(fixture.Root.Runtime.BoardState.IsDiceOnBoard(playerDiceId), Is.False);
            Assert.That(playerView.transform.position, Is.EqualTo(fixture.Layout.GetInventoryDicePosition(Side.Player, 0, 1)));
            Assert.That(playerView.SpriteRenderer.sprite, Is.SameAs(fixture.NeutralSprite));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApprovedMovement_ReconcilesViewToAuthoritativeSlotAnchor()
        {
            PresentationFixture fixture = CreateFixture();
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            SlotId destination = new SlotId(Side.Player, 3);

            Assert.That(fixture.Root.CompleteEnemySetup().IsApproved, Is.True);
            MoveDiceResult move = fixture.Root.RequestPlayerMove(playerDiceId, MoveDiceTarget.Board(destination));
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);

            Assert.That(move.IsApproved, Is.True);
            Assert.That(fixture.Root.Runtime.BoardState.GetSlot(destination).OccupantDiceId, Is.EqualTo(playerDiceId));
            Assert.That(fixture.Root.Runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.False);
            Assert.That(playerView.transform.position, Is.EqualTo(fixture.Layout.GetBoardDicePosition(destination)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RejectedMovement_ReconcilesDraggedViewBackToAuthoritativeLocation()
        {
            PresentationFixture fixture = CreateFixture();
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);
            Vector3 authoritativePosition = playerView.transform.position;
            playerView.SetPreviewWorldPosition(new Vector3(50f, 50f, 50f));

            MoveDiceResult move = fixture.Root.RequestPlayerMove(
                playerDiceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 1)));

            Assert.That(move.IsRejected, Is.True, "Player movement must remain denied during EnemySetup.");
            Assert.That(fixture.Root.Runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.True);
            Assert.That(playerView.transform.position, Is.EqualTo(authoritativePosition));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RollResult_ReconcilesBoundViewToAuthoritativeFaceSprite()
        {
            PresentationFixture fixture = CreateFixture(primaryRolls: new[] { 4 });
            DiceInstanceId playerDiceId = new DiceInstanceId(1);

            Assert.That(fixture.Root.CompleteEnemySetup().IsApproved, Is.True);
            Assert.That(fixture.Root.RequestPlayerMove(
                playerDiceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))).IsApproved, Is.True);

            BattleFlowResult roll = fixture.Root.RequestRoll();
            DiceRuntimeState runtimeDice = fixture.Root.Runtime.BattleInventoryState.GetDice(playerDiceId);
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);

            Assert.That(roll.IsApproved, Is.True);
            Assert.That(runtimeDice.CurrentFaceIndex, Is.EqualTo(4));
            Assert.That(playerView.SpriteRenderer.sprite, Is.SameAs(fixture.FaceSprites[3]));
            Assert.That(fixture.Root.Runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReconcileViews_RepairsPresentationDriftWithoutChangingGameplayState()
        {
            PresentationFixture fixture = CreateFixture();
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);
            Vector3 expected = playerView.transform.position;

            playerView.transform.position = new Vector3(-100f, 10f, 20f);
            fixture.Root.ReconcileViews();

            Assert.That(playerView.transform.position, Is.EqualTo(expected));
            Assert.That(fixture.Root.Runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.True);
            Assert.That(fixture.Root.Runtime.BoardState.IsDiceOnBoard(playerDiceId), Is.False);
            yield return null;
        }


        [UnityTest]
        public IEnumerator DecayResult_ReconciliationHidesDecayedViewsFromAuthoritativeState()
        {
            PresentationFixture fixture = CreateFixture(primaryRolls: new[] { 6, 5 });
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            DiceInstanceId enemyDiceId = new DiceInstanceId(2);
            SlotId enemySlot = new SlotId(Side.Enemy, 2);
            SlotId playerSlot = new SlotId(Side.Player, 2);

            MoveDiceResult enemyMove = fixture.Root.Runtime.MoveDiceController.RequestMove(
                new MoveDiceRequest(Side.Enemy, enemyDiceId, MoveDiceTarget.Board(enemySlot)));
            Assert.That(enemyMove.IsApproved, Is.True);
            Assert.That(fixture.Root.CompleteEnemySetup().IsApproved, Is.True);
            Assert.That(fixture.Root.RequestPlayerMove(playerDiceId, MoveDiceTarget.Board(playerSlot)).IsApproved, Is.True);
            Assert.That(fixture.Root.RequestRoll().IsApproved, Is.True);
            Assert.That(fixture.Root.CompleteRoll().IsApproved, Is.True);
            Assert.That(fixture.Root.CompleteEnemyReposition().IsApproved, Is.True);
            Assert.That(fixture.Root.RequestDecay().IsApproved, Is.True);

            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);
            DiceView enemyView = GetRequiredView(fixture.Root, enemyDiceId);
            Assert.That(fixture.Root.Runtime.BattleInventoryState.GetDice(playerDiceId).IsDecayedForCurrentGame, Is.True);
            Assert.That(fixture.Root.Runtime.BattleInventoryState.GetDice(enemyDiceId).IsDecayedForCurrentGame, Is.True);
            Assert.That(playerView.SpriteRenderer.enabled, Is.False);
            Assert.That(enemyView.SpriteRenderer.enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameEndReset_RepopulatesDecayedPlayerViewForNextGame()
        {
            PresentationFixture fixture = CreateFixture(primaryRolls: new[] { 6 });
            DiceInstanceId playerDiceId = new DiceInstanceId(1);
            DiceView playerView = GetRequiredView(fixture.Root, playerDiceId);

            Assert.That(fixture.Root.CompleteEnemySetup().IsApproved, Is.True);
            Assert.That(fixture.Root.RequestPlayerMove(
                playerDiceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 3))).IsApproved, Is.True);

            for (int round = 1; round <= 4; round++)
            {
                if (round > 1)
                    Assert.That(fixture.Root.CompleteEnemySetup().IsApproved, Is.True);
                Assert.That(fixture.Root.RequestRoll().IsApproved, Is.True);
                Assert.That(fixture.Root.CompleteRoll().IsApproved, Is.True);
                Assert.That(fixture.Root.CompleteEnemyReposition().IsApproved, Is.True);
                Assert.That(fixture.Root.RequestDecay().IsApproved, Is.True);
                Assert.That(fixture.Root.CompleteDecay().IsApproved, Is.True);
                Assert.That(fixture.Root.CompleteScore().IsApproved, Is.True);
                Assert.That(fixture.Root.CompleteRoundEnd().IsApproved, Is.True);
            }

            DiceRuntimeState player = fixture.Root.Runtime.BattleInventoryState.GetDice(playerDiceId);
            Assert.That(fixture.Root.Runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.GameEnd));
            Assert.That(player.IsDecayedForCurrentGame, Is.False);
            Assert.That(fixture.Root.Runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.True);
            Assert.That(playerView.SpriteRenderer.enabled, Is.True);
            Assert.That(playerView.transform.position, Is.EqualTo(fixture.Layout.GetInventoryDicePosition(Side.Player, 0, 1)));

            Assert.That(fixture.Root.CompleteGameEnd().IsApproved, Is.True);
            Assert.That(fixture.Root.Runtime.BattleState.CurrentGameNumber, Is.EqualTo(2));
            Assert.That(fixture.Root.Runtime.BattleState.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(fixture.Root.Runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.EnemySetup));
            yield return null;
        }

        private PresentationFixture CreateFixture(int[] primaryRolls = null)
        {
            Sprite neutralSprite = CreateSprite();
            var faceSprites = new List<Sprite>();
            var faces = new List<DiceFaceDefinition>();
            for (int i = 1; i <= 6; i++)
            {
                Sprite faceSprite = CreateSprite();
                faceSprites.Add(faceSprite);
                faces.Add(new DiceFaceDefinition(i, i, i, faceSprite));
            }

            DiceView prefab = CreateDiceViewPrefab(neutralSprite);
            DiceDefinition definition = Track(ScriptableObject.CreateInstance<DiceDefinition>());
            definition.ConfigureForTests(new DiceId("dice.playmode_neutral"), "PlayMode Neutral", 0, faces);
            definition.ConfigurePresentationForTests(neutralSprite, neutralSprite, prefab.gameObject);

            DiceCatalog catalog = Track(ScriptableObject.CreateInstance<DiceCatalog>());
            catalog.ConfigureForTests(new[] { definition });
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());

            BattleSceneDiceLayout layout = CreateLayout();
            GameObject rootObject = Track(new GameObject("BattleCompositionRoot_Test"));
            rootObject.SetActive(false);
            BattleCompositionRoot root = rootObject.AddComponent<BattleCompositionRoot>();
            Transform diceViewRoot = Track(new GameObject("DiceViewRoot_Test")).transform;
            root.ConfigureForTests(config, catalog, prefab, diceViewRoot, layout);

            GlobalInventoryState globalInventory = new GlobalInventoryState(new[]
            {
                new GlobalDiceState(new OwnedDiceId(101), DiceRuntimeSeed.FromDefinition(definition))
            });

            IRandomSource primary = primaryRolls == null
                ? new SeededRandomSource(1)
                : new ScriptedRandomSource(primaryRolls);
            root.Initialize(
                globalInventory,
                new[] { new OwnedDiceId(101) },
                new[] { DiceRuntimeSeed.FromDefinition(definition) },
                primary,
                new ScriptedRandomSource(new[] { 1, 1, 1, 1, 1, 1 }));

            return new PresentationFixture(root, layout, diceViewRoot, neutralSprite, faceSprites);
        }

        private BattleSceneDiceLayout CreateLayout()
        {
            GameObject layoutObject = Track(new GameObject("BattleSceneDiceLayout_Test"));
            BattleSceneDiceLayout layout = layoutObject.AddComponent<BattleSceneDiceLayout>();
            var slotAnchors = new List<(SlotId SlotId, Transform Anchor)>();
            for (int number = 1; number <= 6; number++)
            {
                slotAnchors.Add((new SlotId(Side.Enemy, number), CreateAnchor($"Slot_{number}E", new Vector3(number, 0f, 2f))));
                slotAnchors.Add((new SlotId(Side.Player, number), CreateAnchor($"Slot_{number}P", new Vector3(number, 0f, 0f))));
            }

            Transform playerInventory = CreateAnchor("PlayerInventory", new Vector3(0f, 0f, -2f));
            Transform enemyInventory = CreateAnchor("EnemyInventory", new Vector3(0f, 0f, 4f));
            GameObject dropObject = Track(new GameObject("PlayerInventoryDrop"));
            BoxCollider dropCollider = dropObject.AddComponent<BoxCollider>();

            layout.ConfigureForTests(slotAnchors, playerInventory, dropCollider, enemyInventory);
            Assert.That(layout.TryValidate(out string error), Is.True, error);
            return layout;
        }

        private DiceView CreateDiceViewPrefab(Sprite sprite)
        {
            GameObject gameObject = Track(new GameObject("PF_DiceView_Test"));
            gameObject.SetActive(false);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            DiceView view = gameObject.AddComponent<DiceView>();
            view.ConfigureForTests(renderer, collider);
            gameObject.SetActive(true);
            return view;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Track(new Texture2D(1, 1));
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Track(Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1));
            return sprite;
        }

        private Transform CreateAnchor(string name, Vector3 position)
        {
            GameObject gameObject = Track(new GameObject(name));
            gameObject.transform.position = position;
            return gameObject.transform;
        }

        private T Track<T>(T value) where T : Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private static DiceView GetRequiredView(BattleCompositionRoot root, DiceInstanceId diceId)
        {
            Assert.That(root.TryGetDiceView(diceId, out DiceView view), Is.True);
            return view;
        }

        private sealed class PresentationFixture
        {
            public PresentationFixture(
                BattleCompositionRoot root,
                BattleSceneDiceLayout layout,
                Transform diceViewRoot,
                Sprite neutralSprite,
                IReadOnlyList<Sprite> faceSprites)
            {
                Root = root;
                Layout = layout;
                DiceViewRoot = diceViewRoot;
                NeutralSprite = neutralSprite;
                FaceSprites = faceSprites;
            }

            public BattleCompositionRoot Root { get; }
            public BattleSceneDiceLayout Layout { get; }
            public Transform DiceViewRoot { get; }
            public Sprite NeutralSprite { get; }
            public IReadOnlyList<Sprite> FaceSprites { get; }
        }
    }
}
