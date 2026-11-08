using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Farhat.Data;

namespace Farhat.EditorTools
{
    /// <summary>
    /// Content validation (project document section 9.1): all 52 chapters present, unique ids and scene names, objectives and checkpoints
    /// consistent, failure rules pointing at declared checkpoints, chapter scenes present for included chapters, fragments 1 to 7 each granted once.
    /// </summary>
    public static class ContentValidator
    {
        public static List<string> Validate(ChapterCatalog catalog, bool requireScenes)
        {
            List<string> errors = new List<string>();
            if (catalog == null) { errors.Add("ChapterCatalog asset not found. Run Farhat > Content > Import Chapters From JSON."); return errors; }
            if (catalog.Count != ChapterCatalog.ChapterCount) errors.Add("Catalog has " + catalog.Count + " chapters, expected " + ChapterCatalog.ChapterCount + ".");

            HashSet<int> numbers = new HashSet<int>();
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> scenes = new HashSet<string>();
            Dictionary<string, int> fragmentOwners = new Dictionary<string, int>();
            for (int i = 0; i < catalog.chapters.Count; i++)
            {
                ChapterDefinition c = catalog.chapters[i];
                if (c == null) { errors.Add("Catalog slot " + i + " is empty."); continue; }
                string tag = "Chapter " + c.number;
                if (c.number < 1 || c.number > ChapterCatalog.ChapterCount) errors.Add(tag + ": number out of range.");
                if (!numbers.Add(c.number)) errors.Add(tag + ": duplicate number.");
                if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) errors.Add(tag + ": missing or duplicate id.");
                if (string.IsNullOrEmpty(c.sceneName) || !scenes.Add(c.sceneName)) errors.Add(tag + ": missing or duplicate scene name.");
                if (string.IsNullOrEmpty(c.episodeTitle)) errors.Add(tag + ": missing episode title.");
                if (c.objectives.Count == 0) errors.Add(tag + ": no objectives.");
                if (c.CountObjectives(1) == 0) errors.Add(tag + ": needs at least one tier-1 objective.");
                if (c.checkpointIds.Count == 0 || c.checkpointIds[0] != "start") errors.Add(tag + ": the first checkpoint must be 'start'.");
                HashSet<string> objectiveIds = new HashSet<string>();
                for (int o = 0; o < c.objectives.Count; o++)
                {
                    if (string.IsNullOrEmpty(c.objectives[o].id) || !c.objectives[o].id.StartsWith(c.locPrefix)) errors.Add(tag + ": objective id must start with " + c.locPrefix);
                    if (!objectiveIds.Add(c.objectives[o].id)) errors.Add(tag + ": duplicate objective id " + c.objectives[o].id);
                }
                for (int f = 0; f < c.failureRules.Count; f++)
                {
                    string target = c.failureRules[f].checkpointId;
                    if (!string.IsNullOrEmpty(target) && !c.HasCheckpoint(target)) errors.Add(tag + ": failure rule " + c.failureRules[f].type + " points at undeclared checkpoint '" + target + "'.");
                }
                if (c.GetRule(FailureType.Health) == null && c.failureRules.Count > 0) errors.Add(tag + ": every chapter that can fail needs a Health rule.");
                for (int u = 0; u < c.unlocks.Count; u++)
                {
                    if (c.unlocks[u].StartsWith("fragment."))
                    {
                        int owner;
                        if (fragmentOwners.TryGetValue(c.unlocks[u], out owner)) errors.Add(tag + ": " + c.unlocks[u] + " is already granted by chapter " + owner + ".");
                        else fragmentOwners[c.unlocks[u]] = c.number;
                    }
                }
                if (requireScenes && c.includedInBuild && AssetDatabase.FindAssets(c.sceneName + " t:Scene").Length == 0)
                {
                    errors.Add(tag + ": scene '" + c.sceneName + "' not found.");
                }
            }
            for (int i = 1; i <= 7; i++)
            {
                if (!fragmentOwners.ContainsKey("fragment." + i)) errors.Add("Fragment " + i + " is not granted by any chapter.");
            }
            return errors;
        }

        [MenuItem("Farhat/Content/Validate")]
        public static void ValidateMenu()
        {
            List<string> errors = Validate(AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath), false);
            Report(errors);
        }

        /// <summary>Batch entry point: -executeMethod Farhat.EditorTools.ContentValidator.RunCli [-requireScenes]</summary>
        public static void RunCli()
        {
            bool requireScenes = Array.IndexOf(Environment.GetCommandLineArgs(), "-requireScenes") >= 0;
            List<string> errors = Validate(AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath), requireScenes);
            Report(errors);
            if (Application.isBatchMode) EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        static void Report(List<string> errors)
        {
            if (errors.Count == 0) { Debug.Log("[Farhat] Content validation passed."); return; }
            StringBuilder sb = new StringBuilder("[Farhat] Content validation failed:\n");
            for (int i = 0; i < errors.Count; i++) sb.AppendLine(" - " + errors[i]);
            Debug.LogError(sb.ToString());
        }
    }
}
