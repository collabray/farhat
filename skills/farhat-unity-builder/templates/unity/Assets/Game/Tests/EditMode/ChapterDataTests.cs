using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Farhat.Data;
using Farhat.EditorTools;

namespace Farhat.Tests
{
    /// <summary>Checks chapters.json against the facts in the bible. These tests protect the data contract between the design documents and the build.</summary>
    public class ChapterDataTests
    {
        static readonly int[] VerifiedChapters = { 1, 2, 3, 5, 7, 8, 11, 13, 14, 15, 17, 18, 19, 21, 22, 23, 25, 26, 33, 35, 39 };

        static ChapterFile Load()
        {
            return ChapterCatalogImporter.ReadFile();
        }

        [Test]
        public void ThereAreFiftyTwoChaptersInOrder()
        {
            ChapterFile file = Load();
            Assert.AreEqual(52, file.chapters.Count);
            for (int i = 0; i < 52; i++) Assert.AreEqual(i + 1, file.chapters[i].number);
        }

        [Test]
        public void OnlyTheBibleVerifiedChaptersAreTaggedVerified()
        {
            ChapterFile file = Load();
            List<int> verified = new List<int>();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                if (file.chapters[i].sourceTag == "Verified") verified.Add(file.chapters[i].number);
            }
            CollectionAssert.AreEqual(VerifiedChapters, verified);
        }

        [Test]
        public void SeasonSplitIsTwentySixAndTwentySix()
        {
            ChapterFile file = Load();
            int s1 = 0;
            int s2 = 0;
            for (int i = 0; i < file.chapters.Count; i++)
            {
                if (file.chapters[i].season == 1) s1++; else s2++;
            }
            Assert.AreEqual(26, s1);
            Assert.AreEqual(26, s2);
        }

        [Test]
        public void EachFragmentOneToSevenIsGrantedByExactlyOneChapter()
        {
            ChapterFile file = Load();
            Dictionary<string, int> owners = new Dictionary<string, int>();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                for (int u = 0; u < file.chapters[i].unlocks.Count; u++)
                {
                    string id = file.chapters[i].unlocks[u];
                    if (!id.StartsWith("fragment.")) continue;
                    Assert.IsFalse(owners.ContainsKey(id), id + " granted twice");
                    owners[id] = file.chapters[i].number;
                }
            }
            for (int f = 1; f <= 7; f++) Assert.IsTrue(owners.ContainsKey("fragment." + f), "fragment " + f);
            Assert.AreEqual(18, owners["fragment.5"]);
            Assert.AreEqual(21, owners["fragment.6"]);
        }

        [Test]
        public void HeartUpgradesAreInChaptersFourTenFifteenTwentyOneAndTwentyFive()
        {
            ChapterFile file = Load();
            List<int> chapters = new List<int>();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                for (int u = 0; u < file.chapters[i].unlocks.Count; u++)
                {
                    if (file.chapters[i].unlocks[u].StartsWith("hearts.plus1.")) chapters.Add(file.chapters[i].number);
                }
            }
            CollectionAssert.AreEqual(new[] { 4, 10, 15, 21, 25 }, chapters);
        }

        [Test]
        public void KeyUnlocksLandInTheChaptersTheBibleNames()
        {
            ChapterFile file = Load();
            Dictionary<string, int> where = new Dictionary<string, int>();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                for (int u = 0; u < file.chapters[i].unlocks.Count; u++) where[file.chapters[i].unlocks[u]] = file.chapters[i].number;
            }
            Assert.AreEqual(1, where["shadow.scout"]);
            Assert.AreEqual(5, where["shadow.swap"]);
            Assert.AreEqual(7, where["shadow.shape"]);
            Assert.AreEqual(2, where["potion.heal"]);
            Assert.AreEqual(6, where["potion.boost"]);
            Assert.AreEqual(11, where["potion.transform"]);
            Assert.AreEqual(14, where["potion.cure"]);
            Assert.AreEqual(22, where["potion.weather"]);
            Assert.AreEqual(4, where["meter.tier1"]);
            Assert.AreEqual(15, where["meter.tier2"]);
            Assert.AreEqual(18, where["meter.tier3"]);
            Assert.AreEqual(25, where["meter.max"]);
            Assert.AreEqual(31, where["item.anti-reversal"]);
            Assert.AreEqual(33, where["system.black-hole-closing"]);
            Assert.AreEqual(48, where["shadow.evolved"]);
        }

        [Test]
        public void EveryChapterHasConsistentObjectivesCheckpointsAndRules()
        {
            ChapterFile file = Load();
            HashSet<string> scenes = new HashSet<string>();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                ChapterRecord c = file.chapters[i];
                Assert.IsTrue(scenes.Add(c.sceneName), "scene " + c.sceneName);
                Assert.Greater(c.objectives.Count, 0, c.id);
                Assert.AreEqual("start", c.checkpointIds[0], c.id);
                bool tier1 = false;
                for (int o = 0; o < c.objectives.Count; o++)
                {
                    Assert.IsTrue(c.objectives[o].id.StartsWith(c.locPrefix), c.objectives[o].id);
                    if (c.objectives[o].tier == 1) tier1 = true;
                }
                Assert.IsTrue(tier1, c.id + " needs a tier-1 objective");
                for (int r = 0; r < c.failureRules.Count; r++)
                {
                    string target = c.failureRules[r].checkpointId;
                    Assert.IsTrue(string.IsNullOrEmpty(target) || c.checkpointIds.Contains(target), c.id + " rule " + c.failureRules[r].type);
                }
            }
        }

        [Test]
        public void ImpulseTrapIsOnlyInChapterSeventeenAndLocksTheShadow()
        {
            ChapterFile file = Load();
            for (int i = 0; i < file.chapters.Count; i++)
            {
                for (int r = 0; r < file.chapters[i].failureRules.Count; r++)
                {
                    FailureRuleRecord rule = file.chapters[i].failureRules[r];
                    if (rule.type == "ImpulseTrap")
                    {
                        Assert.AreEqual(17, file.chapters[i].number);
                        Assert.IsTrue(rule.lockShadowNextArea);
                        Assert.Greater(rule.resourceLoss, 0);
                    }
                }
            }
        }

        [Test]
        public void ImportedCatalogPassesContentValidationAndTheStoryGateBlocksInferredChapters()
        {
            ChapterCatalogImporter.ImportAll();
            ChapterCatalog catalog = AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath);
            List<string> errors = ContentValidator.Validate(catalog, false);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors.ToArray()));
            Assert.AreEqual(31, StoryGate.FindUnverified(catalog).Count);
        }
    }
}
