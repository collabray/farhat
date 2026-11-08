using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Farhat.Data;

namespace Farhat.EditorTools
{
    /// <summary>Chapter Board (project document 8.6): all 52 chapters with tag, tier and type, and a button that opens each chapter scene.</summary>
    public class ChapterBoardWindow : EditorWindow
    {
        Vector2 scroll;
        bool onlyUnverified;

        [MenuItem("Farhat/Chapter Board")]
        public static void Open() { GetWindow<ChapterBoardWindow>("Chapter Board"); }

        void OnGUI()
        {
            ChapterCatalog catalog = AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath);
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("No ChapterCatalog yet. Use Farhat > Content > Import Chapters From JSON.", MessageType.Info);
                if (GUILayout.Button("Import chapters")) ChapterCatalogImporter.ImportAll();
                return;
            }
            onlyUnverified = EditorGUILayout.ToggleLeft("Show only chapters that are not Verified", onlyUnverified);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < catalog.chapters.Count; i++)
            {
                ChapterDefinition c = catalog.chapters[i];
                if (c == null || (onlyUnverified && c.sourceTag == SourceTag.Verified)) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(c.number.ToString("00"), GUILayout.Width(24));
                GUILayout.Label(c.episodeTitle, GUILayout.Width(250));
                GUILayout.Label(c.sourceTag + " (" + c.bibleTag + ")", GUILayout.Width(110));
                GUILayout.Label("Tier " + c.costTier, GUILayout.Width(50));
                GUILayout.Label(c.chapterType.ToString(), GUILayout.Width(80));
                c.includedInBuild = EditorGUILayout.Toggle(c.includedInBuild, GUILayout.Width(20));
                if (GUILayout.Button("Open scene", GUILayout.Width(90)))
                {
                    string path = SceneGenerator.SceneFolder + "/" + c.sceneName + ".unity";
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) EditorSceneManager.OpenScene(path);
                    else Debug.LogWarning("[Farhat] Scene not found: " + path);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
