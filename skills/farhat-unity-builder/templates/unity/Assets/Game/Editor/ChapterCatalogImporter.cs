using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Farhat.Data;

namespace Farhat.EditorTools
{
    /// <summary>
    /// Bible-to-data importer. Reads Assets/Game/Content/Data/chapters.json and creates or updates one ChapterDefinition per chapter plus the
    /// ChapterCatalog in Assets/Resources. Authored fields (includedInBuild) are never overwritten.
    /// </summary>
    public static class ChapterCatalogImporter
    {
        public const string JsonPath = "Assets/Game/Content/Data/chapters.json";
        public const string ChapterFolder = "Assets/Game/Content/Chapters";
        public const string CatalogPath = "Assets/Resources/ChapterCatalog.asset";

        [MenuItem("Farhat/Content/Import Chapters From JSON")]
        public static void ImportAll()
        {
            ChapterFile file = ReadFile();
            EnsureFolder("Assets/Game/Content");
            EnsureFolder(ChapterFolder);
            EnsureFolder("Assets/Resources");

            ChapterCatalog catalog = AssetDatabase.LoadAssetAtPath<ChapterCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ChapterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.chapters.Clear();

            for (int i = 0; i < file.chapters.Count; i++)
            {
                ChapterRecord record = file.chapters[i];
                string path = ChapterFolder + "/" + record.id + ".asset";
                ChapterDefinition def = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<ChapterDefinition>();
                    AssetDatabase.CreateAsset(def, path);
                }
                Apply(record, def);
                EditorUtility.SetDirty(def);
                catalog.chapters.Add(def);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Farhat] Imported " + file.chapters.Count + " chapters.");
        }

        public static ChapterFile ReadFile()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            string json = asset != null ? asset.text : File.ReadAllText(JsonPath);
            ChapterFile file = JsonUtility.FromJson<ChapterFile>(json);
            if (file == null || file.chapters == null || file.chapters.Count != ChapterCatalog.ChapterCount)
            {
                throw new InvalidOperationException("chapters.json must contain exactly " + ChapterCatalog.ChapterCount + " chapters.");
            }
            return file;
        }

        public static void Apply(ChapterRecord r, ChapterDefinition def)
        {
            def.number = r.number;
            def.id = r.id;
            def.episodeTitle = r.episodeTitle;
            def.season = r.season;
            def.part = r.part;
            def.sourceTag = (SourceTag)Enum.Parse(typeof(SourceTag), r.sourceTag);
            def.bibleTag = r.bibleTag;
            def.costTier = (CostTier)Enum.Parse(typeof(CostTier), r.costTier);
            def.chapterType = (ChapterType)Enum.Parse(typeof(ChapterType), r.chapterType);
            def.location = r.location;
            def.coreMechanic = r.coreMechanic;
            def.setPiece = r.setPiece;
            def.storyBeats = r.storyBeats;
            def.sceneName = r.sceneName;
            def.locPrefix = r.locPrefix;
            def.objectives = new List<ObjectiveDef>();
            for (int i = 0; i < r.objectives.Count; i++)
            {
                ObjectiveDef o = new ObjectiveDef();
                o.id = r.objectives[i].id;
                o.text = r.objectives[i].text;
                o.tier = r.objectives[i].tier;
                def.objectives.Add(o);
            }
            def.checkpointIds = new List<string>(r.checkpointIds);
            def.failureRules = new List<FailureRule>();
            for (int i = 0; i < r.failureRules.Count; i++)
            {
                FailureRule f = new FailureRule();
                f.type = (FailureType)Enum.Parse(typeof(FailureType), r.failureRules[i].type);
                f.checkpointId = r.failureRules[i].checkpointId;
                f.lockShadowNextArea = r.failureRules[i].lockShadowNextArea;
                f.resourceLoss = r.failureRules[i].resourceLoss;
                f.hintAfterRetries = r.failureRules[i].hintAfterRetries;
                def.failureRules.Add(f);
            }
            def.unlocks = new List<string>(r.unlocks);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
