using UnityEditor;
using UnityEngine;

namespace Farhat.EditorTools
{
    /// <summary>Applies the project settings from the project document: linear colour space, product name, input handling and the folder layout.</summary>
    public static class ProjectSetup
    {
        [MenuItem("Farhat/Setup/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Farhat The Prince of the Desert";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Farhat Game Studio";
            EnableNewInputSystem();
            string[] folders =
            {
                "Assets/Game/Content/Chapters", "Assets/Game/Content/Scenes", "Assets/Game/Content/Enemies", "Assets/Game/Content/Dialogue",
                "Assets/Game/Content/Prefabs", "Assets/Game/Content/Recipes", "Assets/Game/Art", "Assets/Game/Audio", "Assets/Game/VFX",
                "Assets/Game/UI", "Assets/Resources", "Assets/ThirdParty"
            };
            for (int i = 0; i < folders.Length; i++) ChapterCatalogImporter.EnsureFolder(folders[i]);
            AssetDatabase.SaveAssets();
            Debug.Log("[Farhat] Project settings applied. The input backend change takes effect when the editor is restarted.");
        }

        /// <summary>
        /// The game reads input only through the Input System package. Set Active Input Handling to the new system (1). Takes effect after a restart,
        /// which happens naturally between batch commands.
        /// </summary>
        public static void EnableNewInputSystem()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty handler = settings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue == 1) return;
            handler.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
