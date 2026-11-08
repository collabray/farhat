using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;
using Farhat.Story;
using Farhat.Systems;

namespace Farhat.Tests
{
    public class CoreTests
    {
        [SetUp]
        public void SetUp() { EventBusRegistry.ClearAll(); ServiceLocator.Clear(); }

        [TearDown]
        public void TearDown() { EventBusRegistry.ClearAll(); ServiceLocator.Clear(); }

        [Test]
        public void EventBus_DeliversToSubscribersAndStopsAfterUnsubscribe()
        {
            int received = 0;
            Action<FragmentCollected> handler = e => received += e.Index;
            EventBus<FragmentCollected>.Subscribe(handler);
            EventBus<FragmentCollected>.Publish(new FragmentCollected(3));
            EventBus<FragmentCollected>.Unsubscribe(handler);
            EventBus<FragmentCollected>.Publish(new FragmentCollected(4));
            Assert.AreEqual(3, received);
        }

        [Test]
        public void EventBusRegistry_ClearAllRemovesEverySubscriber()
        {
            EventBus<ChapterStarted>.Subscribe(e => { });
            EventBus<ChapterCompleted>.Subscribe(e => { });
            EventBusRegistry.ClearAll();
            Assert.AreEqual(0, EventBus<ChapterStarted>.SubscriberCount);
            Assert.AreEqual(0, EventBus<ChapterCompleted>.SubscriberCount);
        }

        [Test]
        public void ServiceLocator_ReturnsRegisteredServiceAndThrowsWhenMissing()
        {
            UnlockManager unlocks = new UnlockManager();
            ServiceLocator.Register(unlocks);
            Assert.AreSame(unlocks, ServiceLocator.Get<UnlockManager>());
            ServiceLocator.Unregister<UnlockManager>();
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<UnlockManager>());
        }

        [Test]
        public void StateMachine_CallsEnterTickAndExitInOrder()
        {
            string log = string.Empty;
            StateMachine<ShadowState> machine = new StateMachine<ShadowState>();
            machine.Configure(ShadowState.Available, () => log += "A+", dt => log += "At", () => log += "A-")
                   .Configure(ShadowState.Captured, () => log += "C+", null, null);
            machine.Set(ShadowState.Available);
            machine.Tick(0.1f);
            machine.Set(ShadowState.Captured);
            machine.Set(ShadowState.Captured);
            Assert.AreEqual("A+AtA-C+", log);
        }

        [Test]
        public void SaveMigrator_UpgradesVersionOneSaves()
        {
            SaveData old = new SaveData();
            old.version = 1;
            old.hearts = 2;
            old.completedObjectives = null;
            old.chapterStars = null;
            SaveData migrated = SaveMigrator.Migrate(old);
            Assert.AreEqual(SaveMigrator.Current, migrated.version);
            Assert.GreaterOrEqual(migrated.hearts, 5);
            Assert.IsNotNull(migrated.completedObjectives);
            Assert.IsNotNull(migrated.chapterStars);
        }

        [Test]
        public void SaveSystem_RoundTripsAndFallsBackToBackupWhenTheMainFileIsCorrupt()
        {
            string dir = Path.Combine(Path.GetTempPath(), "farhat_save_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                SaveSystem saves = new SaveSystem(dir);
                SaveData first = new SaveData();
                first.chapter = 7;
                first.SetFragment(2);
                saves.Write(0, first);
                SaveData second = new SaveData();
                second.chapter = 8;
                saves.Write(0, second);
                Assert.AreEqual(8, new SaveSystem(dir).Load(0).chapter);

                File.WriteAllText(saves.PathFor(0), "{ not json");
                SaveData recovered = new SaveSystem(dir).Load(0);
                Assert.AreEqual(7, recovered.chapter, "The backup holds the previous good save.");
                Assert.IsTrue(recovered.HasFragment(2));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void SaveData_FragmentMaskAndStars()
        {
            SaveData data = new SaveData();
            data.SetFragment(1);
            data.SetFragment(7);
            data.SetFragment(9);
            Assert.AreEqual(2, data.FragmentCount);
            data.SetStars(3, 2);
            data.SetStars(3, 1);
            Assert.AreEqual(2, data.chapterStars[2]);
            Assert.AreEqual(3, data.chapterStars.Count);
        }

        [Test]
        public void UnlockManager_PublishesFragmentEventsAndCountsBonusHearts()
        {
            UnlockManager unlocks = new UnlockManager();
            int fragment = 0;
            EventBus<FragmentCollected>.Subscribe(e => fragment = e.Index);
            Assert.IsTrue(unlocks.Grant("fragment.4"));
            Assert.IsFalse(unlocks.Grant("fragment.4"));
            unlocks.Grant("hearts.plus1.ch04");
            unlocks.Grant("hearts.plus1.ch10");
            Assert.AreEqual(4, fragment);
            Assert.AreEqual(1, unlocks.FragmentCount);
            Assert.AreEqual(2, unlocks.BonusHearts);

            SaveData data = new SaveData();
            unlocks.WriteTo(data);
            UnlockManager loaded = new UnlockManager();
            loaded.LoadFrom(data);
            Assert.IsTrue(loaded.Has("fragment.4"));
            Assert.IsTrue(loaded.Has("hearts.plus1.ch10"));
        }

        [Test]
        public void DifficultyModifiers_ScaleAsDocumented()
        {
            DifficultyModifiers story = DifficultyModifiers.For(Difficulty.Story);
            DifficultyModifiers standard = DifficultyModifiers.For(Difficulty.Standard);
            DifficultyModifiers challenge = DifficultyModifiers.For(Difficulty.Challenge);
            Assert.Less(story.EnemyHealthMultiplier, standard.EnemyHealthMultiplier);
            Assert.Greater(challenge.EnemyHealthMultiplier, standard.EnemyHealthMultiplier);
            Assert.IsTrue(story.AutoParry);
            Assert.IsFalse(standard.AutoParry);
            Assert.Greater(story.ParryWindowSeconds, challenge.ParryWindowSeconds);
            Assert.IsTrue(challenge.ReducedCheckpointsInSetPieces);
        }

        [Test]
        public void ChapterScoring_GivesOneToThreeStars()
        {
            Assert.AreEqual(3, ChapterScoring.Stars(1f, 600f, 3, 4, CostTier.B));
            Assert.AreEqual(2, ChapterScoring.Stars(1f, 99999f, 0, 4, CostTier.B));
            Assert.AreEqual(1, ChapterScoring.Stars(8f, 99999f, 0, 4, CostTier.A));
        }

        class FakeParticipant : ISnapshotParticipant
        {
            public int Value;
            public string SnapshotKey { get { return "fake"; } }
            public string CaptureSnapshot() { return Value.ToString(); }
            public void RestoreSnapshot(string json) { Value = int.Parse(json); }
        }

        [Test]
        public void CheckpointStore_RestoreEqualsTheCapturedSnapshot()
        {
            CheckpointStore store = new CheckpointStore();
            FakeParticipant fake = new FakeParticipant();
            store.Register(fake);
            fake.Value = 5;
            store.Record("start");
            fake.Value = 9;
            store.Record("mid");
            fake.Value = 1;
            Assert.IsTrue(store.Restore("start"));
            Assert.AreEqual(5, fake.Value);
            Assert.AreEqual("start", store.LastId);
            Assert.IsFalse(store.Restore("missing"));
            store.Clear();
            Assert.IsFalse(store.Has("start"));
        }

        [Test]
        public void ObjectiveTracker_PublishesGoalsMetOnceAndSnapshotsRestore()
        {
            ChapterDefinition def = ScriptableObject.CreateInstance<ChapterDefinition>();
            def.number = 99;
            def.objectives.Add(new ObjectiveDef { id = "ch99.a", tier = 1, text = "A" });
            def.objectives.Add(new ObjectiveDef { id = "ch99.b", tier = 1, text = "B" });
            def.objectives.Add(new ObjectiveDef { id = "ch99.c", tier = 2, text = "C" });
            ObjectiveTracker tracker = new ObjectiveTracker();
            tracker.Initialize(def, null);
            int goalsMet = 0;
            EventBus<ChapterGoalsMet>.Subscribe(e => goalsMet++);

            Assert.IsTrue(tracker.Complete("ch99.a"));
            Assert.IsFalse(tracker.Complete("ch99.a"));
            Assert.AreEqual("ch99.b", tracker.NextIncompleteGoal());
            string snapshot = tracker.CaptureSnapshot();
            tracker.Complete("ch99.b");
            Assert.IsTrue(tracker.AllGoalsMet);
            Assert.AreEqual(1, goalsMet);
            tracker.Complete("ch99.c");
            Assert.AreEqual(1, goalsMet);
            tracker.RestoreSnapshot(snapshot);
            Assert.IsFalse(tracker.AllGoalsMet);
            Assert.IsFalse(tracker.IsComplete("ch99.b"));
            Assert.IsFalse(tracker.Complete("unknown"));
            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void DecodePuzzle_WrongSymbolResetsProgressAndHintAppearsAfterFailures()
        {
            GameObject go = new GameObject("puzzle");
            DecodePuzzle puzzle = go.AddComponent<DecodePuzzle>();
            puzzle.Configure(new[] { "sun", "scorpion", "star" }, string.Empty);
            bool solved = false;
            puzzle.Solved += () => solved = true;
            Assert.IsTrue(puzzle.TryInput("sun"));
            Assert.IsFalse(puzzle.TryInput("star"));
            Assert.AreEqual(0, puzzle.Progress);
            Assert.IsFalse(puzzle.TryInput("star"));
            Assert.IsFalse(puzzle.TryInput("star"));
            Assert.IsTrue(puzzle.HintAvailable);
            Assert.AreEqual("sun", puzzle.Hint());
            Assert.IsTrue(puzzle.TryInput("sun"));
            Assert.IsTrue(puzzle.TryInput("scorpion"));
            Assert.IsTrue(puzzle.TryInput("star"));
            Assert.IsTrue(solved);
            Assert.IsTrue(puzzle.IsSolved);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
