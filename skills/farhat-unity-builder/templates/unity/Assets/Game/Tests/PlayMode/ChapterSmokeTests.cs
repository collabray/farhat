using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;
using Farhat.Story;
using Farhat.Systems;

namespace Farhat.Tests
{
    /// <summary>
    /// The chapter smoke-test bot and the fail simulator (project document 9.2). They load the generated Bootstrap scene, start one chapter and
    /// walk its critical path by moving the player into each objective in order. They prove that a path exists, not that it is fun.
    /// Run after "Generate Greybox Scenes". Limit the run with the environment variable FARHAT_SMOKE_CHAPTERS, for example "1,5,7".
    /// </summary>
    public class ChapterSmokeTests
    {
        string saveDir;

        public static IEnumerable<int> Chapters()
        {
            string filter = Environment.GetEnvironmentVariable("FARHAT_SMOKE_CHAPTERS");
            if (!string.IsNullOrEmpty(filter))
            {
                string[] parts = filter.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    int n;
                    if (int.TryParse(parts[i].Trim(), out n)) yield return n;
                }
                yield break;
            }
            for (int n = 1; n <= ChapterCatalog.ChapterCount; n++) yield return n;
        }

        [SetUp]
        public void SetUp()
        {
            EventBusRegistry.ClearAll();
            ServiceLocator.Clear();
            saveDir = Path.Combine(Path.GetTempPath(), "farhat_smoke_" + Guid.NewGuid().ToString("N"));
            GameBootstrap.SuppressAutoStart = true;
            GameBootstrap.SaveDirectoryOverride = saveDir;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SuppressAutoStart = false;
            GameBootstrap.SaveDirectoryOverride = null;
            EventBusRegistry.ClearAll();
            ServiceLocator.Clear();
            if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
        }

        IEnumerator LoadBootstrapAndStart(int number)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            if (load == null) { Assert.Ignore("The Bootstrap scene is not in the build settings. Run Farhat > Content > Generate Greybox Scenes."); yield break; }
            while (!load.isDone) yield return null;
            yield return null;
            GameBootstrap bootstrap = ServiceLocator.Get<GameBootstrap>();
            bootstrap.StartCoroutine(bootstrap.RunFrom(number, "start", true));
            float deadline = Time.realtimeSinceStartup + 30f;
            ChapterRunner runner = ServiceLocator.Get<ChapterRunner>();
            while (!runner.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(runner.IsRunning, "chapter " + number + " did not start");
            yield return null;
        }

        static IEnumerator Settle()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CriticalPath_CompletesTheChapter([ValueSource("Chapters")] int number)
        {
            yield return LoadBootstrapAndStart(number);
            ChapterRunner runner = ServiceLocator.Get<ChapterRunner>();
            PlayerController player = ServiceLocator.Get<PlayerController>();
            bool completed = false;
            EventBus<ChapterCompleted>.Subscribe(e => completed = true);
            runner.Unlocks.Grant("meter.tier1");

            // Wait for the runtime NavMesh bake to enable the enemies.
            yield return new WaitForSeconds(1f);

            ChapterDefinition chapter = runner.Chapter;
            List<Component> stops = new List<Component>();
            stops.AddRange(UnityEngine.Object.FindObjectsByType<ObjectiveTrigger>(FindObjectsSortMode.None));
            stops.AddRange(UnityEngine.Object.FindObjectsByType<FragmentPickup>(FindObjectsSortMode.None));
            stops.AddRange(UnityEngine.Object.FindObjectsByType<ImpulseChoiceTrigger>(FindObjectsSortMode.None));
            stops.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

            for (int i = 0; i < stops.Count; i++)
            {
                ImpulseChoiceTrigger choice = stops[i] as ImpulseChoiceTrigger;
                if (choice != null && choice.Impulsive) continue; // the bot takes the cautious option
                player.Teleport(stops[i].transform.position + Vector3.up * 0.1f, Quaternion.identity);
                yield return Settle();
            }

            EnemyBrain[] enemies = UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                DamageInfo kill = new DamageInfo();
                kill.Amount = 100000f;
                enemies[i].TakeDamage(kill);
            }
            ObjectiveOnLightUsed[] freeables = UnityEngine.Object.FindObjectsByType<ObjectiveOnLightUsed>(FindObjectsSortMode.None);
            for (int i = 0; i < freeables.Length; i++)
            {
                player.Teleport(freeables[i].transform.position + Vector3.back * 2f, Quaternion.identity);
                LightMeter meter = player.GetComponent<LightMeter>();
                meter.Gain(500f);
                meter.TrySpend(LightAbility.Flash);
                yield return Settle();
            }
            yield return new WaitForSeconds(1.5f); // dissolve animations finish and kill objectives complete

            ChapterExitTrigger exit = UnityEngine.Object.FindFirstObjectByType<ChapterExitTrigger>();
            Assert.IsNotNull(exit, "chapter " + number + " has no exit");
            player.Teleport(exit.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!completed && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.IsTrue(completed, "chapter " + number + " (" + chapter.episodeTitle + ") could not be completed. Missing tier-1 goal: " + runner.Objectives.NextIncompleteGoal());
        }

        [UnityTest]
        public IEnumerator FailSimulator_EveryDeclaredFailureReturnsToACheckpoint([ValueSource("Chapters")] int number)
        {
            yield return LoadBootstrapAndStart(number);
            ChapterRunner runner = ServiceLocator.Get<ChapterRunner>();
            PlayerController player = ServiceLocator.Get<PlayerController>();
            HealthComponent health = player.GetComponent<HealthComponent>();
            ChapterDefinition chapter = runner.Chapter;
            yield return new WaitForSeconds(0.5f);

            for (int r = 0; r < chapter.failureRules.Count; r++)
            {
                FailureType type = chapter.failureRules[r].type;
                player.Teleport(player.transform.position + new Vector3(0f, 0f, 3f), Quaternion.identity);
                bool restored = false;
                EventBus<PlayerRestored>.Subscribe(e => restored = true);
                EventBus<PlayerFailed>.Publish(new PlayerFailed(type));
                float deadline = Time.realtimeSinceStartup + 5f;
                while (!restored && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(restored, "chapter " + number + ": failure " + type + " did not restore a checkpoint");
                yield return null;
                Assert.AreEqual(health.MaxHearts, health.Hearts, "chapter " + number + ": player must return at full health after " + type);
                EventBus<PlayerRestored>.Clear();
            }
        }
    }
}
