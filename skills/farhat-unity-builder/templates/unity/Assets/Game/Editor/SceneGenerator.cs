using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Farhat.Data;
using Farhat.Story;
using Farhat.Systems;
using Farhat.UI;

namespace Farhat.EditorTools
{
    /// <summary>
    /// Generates a complete, playable greybox build of all 52 chapters from the ChapterDefinitions: one scene per chapter with a path of
    /// objective triggers, checkpoints, a fragment pickup, enemies, timers, stealth or corruption hazards where the chapter's failure rules ask
    /// for them, and a chapter exit. Final art, animation and bespoke level design replace the greybox chapter by chapter.
    /// </summary>
    public static class SceneGenerator
    {
        public const string SceneFolder = "Assets/Game/Content/Scenes";
        public const string BootstrapPath = SceneFolder + "/Bootstrap.unity";
        const float Spacing = 24f;
        static readonly string[] KillWords = { "defeat", "beat-", "survive-ambush", "hold-position", "hold-the-line", "win-duel", "break-the-spell", "face-", "free-" };

        [MenuItem("Farhat/Content/Generate Greybox Scenes")]
        public static void GenerateAll()
        {
            ChapterCatalogImporter.ImportAll();
            ProjectSetup.Apply();
            ChapterCatalogImporter.EnsureFolder(SceneFolder);
            PrefabBuilders.BuildEnemies();
            GameObject playerPrefab = PrefabBuilders.BuildPlayerPrefab();
            ChapterCatalog catalog = AssetDatabase.LoadAssetAtPath<ChapterCatalog>(ChapterCatalogImporter.CatalogPath);

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            BuildBootstrap(catalog, playerPrefab);
            buildScenes.Add(new EditorBuildSettingsScene(BootstrapPath, true));

            for (int i = 0; i < catalog.chapters.Count; i++)
            {
                ChapterDefinition def = catalog.chapters[i];
                string path = SceneFolder + "/" + def.sceneName + ".unity";
                BuildChapterScene(def, path);
                buildScenes.Add(new EditorBuildSettingsScene(path, def.includedInBuild));
                EditorUtility.DisplayProgressBar("Generating greybox scenes", def.sceneName, (i + 1) / (float)catalog.chapters.Count);
            }
            EditorUtility.ClearProgressBar();
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Farhat] Generated Bootstrap and " + catalog.chapters.Count + " greybox chapter scenes.");
        }

        /// <summary>Batch entry point: -executeMethod Farhat.EditorTools.SceneGenerator.GenerateAllCli</summary>
        public static void GenerateAllCli()
        {
            try
            {
                GenerateAll();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Farhat] Scene generation failed: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void BuildBootstrap(ChapterCatalog catalog, GameObject playerPrefab)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject services = new GameObject("Services");
            ChapterRunner runner = services.AddComponent<ChapterRunner>();
            GameBootstrap bootstrap = services.AddComponent<GameBootstrap>();
            FailForwardManager fail = services.AddComponent<FailForwardManager>();
            services.AddComponent<DialogueRunner>();

            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            ThirdPersonCamera rig = cam.AddComponent<ThirdPersonCamera>();
            cam.transform.position = new Vector3(0f, 4f, -6f);

            GameObject canvasGo = new GameObject("HUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Image[] hearts = new Image[12];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = MakeBar("Heart" + i, canvasGo.transform, sprite, new Vector2(40f + i * 44f, -40f), new Vector2(36f, 36f), new Color(1f, 0.85f, 0.35f));
            }
            Image lightFill = MakeBar("LightMeter", canvasGo.transform, sprite, new Vector2(40f, -92f), new Vector2(360f, 18f), new Color(0.45f, 0.8f, 1f));
            Image ring = MakeBar("ShadowCooldown", canvasGo.transform, sprite, new Vector2(40f, -130f), new Vector2(120f, 14f), new Color(0.6f, 0.45f, 0.9f));
            Image shield = MakeBar("Shield", canvasGo.transform, sprite, new Vector2(410f, -92f), new Vector2(18f, 18f), new Color(1f, 1f, 1f));
            HudController hud = canvasGo.AddComponent<HudController>();
            PrefabBuilders.SetArray(hud, "heartIcons", hearts);
            PrefabBuilders.SetRef(hud, "lightFill", lightFill);
            PrefabBuilders.SetRef(hud, "shadowCooldownRing", ring);
            PrefabBuilders.SetRef(hud, "shieldIndicator", shield);

            Text subtitle = MakeText("Subtitle", canvasGo.transform, font, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1400f, 120f), 28, TextAnchor.LowerCenter);
            SubtitleView subtitleView = canvasGo.AddComponent<SubtitleView>();
            PrefabBuilders.SetRef(subtitleView, "label", subtitle);

            Text quest = MakeText("QuestLog", canvasGo.transform, font, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(520f, 300f), 22, TextAnchor.UpperRight);
            QuestLogView questView = canvasGo.AddComponent<QuestLogView>();
            PrefabBuilders.SetRef(questView, "label", quest);

            GameObject cardGo = new GameObject("TitleCard");
            cardGo.transform.SetParent(canvasGo.transform, false);
            RectTransform cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1000f, 200f);
            CanvasGroup cardGroup = cardGo.AddComponent<CanvasGroup>();
            Text number = MakeText("Number", cardGo.transform, font, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 50f), 30, TextAnchor.MiddleCenter);
            Text title = MakeText("Title", cardGo.transform, font, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1000f, 90f), 54, TextAnchor.MiddleCenter);
            ChapterTitleCard card = cardGo.AddComponent<ChapterTitleCard>();
            PrefabBuilders.SetRef(card, "numberLabel", number);
            PrefabBuilders.SetRef(card, "titleLabel", title);
            PrefabBuilders.SetRef(card, "group", cardGroup);

            GameObject faderGo = new GameObject("Fader");
            faderGo.transform.SetParent(canvasGo.transform, false);
            RectTransform faderRect = faderGo.AddComponent<RectTransform>();
            faderRect.anchorMin = Vector2.zero;
            faderRect.anchorMax = Vector2.one;
            faderRect.offsetMin = Vector2.zero;
            faderRect.offsetMax = Vector2.zero;
            Image black = faderGo.AddComponent<Image>();
            black.color = Color.black;
            faderGo.AddComponent<CanvasGroup>();
            ScreenFader fader = faderGo.AddComponent<ScreenFader>();

            PrefabBuilders.SetRef(runner, "catalog", catalog);
            PrefabBuilders.SetRef(runner, "playerPrefab", playerPrefab);
            PrefabBuilders.SetRef(runner, "cameraRig", rig);
            PrefabBuilders.SetRef(bootstrap, "runner", runner);
            PrefabBuilders.SetRef(fail, "runner", runner);
            PrefabBuilders.SetRef(fail, "fader", fader);

            EditorSceneManager.SaveScene(scene, BootstrapPath);
        }

        static Image MakeBar(string name, Transform parent, Sprite sprite, Vector2 anchoredTopLeft, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredTopLeft;
            rect.sizeDelta = size;
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1f;
            return image;
        }

        static Text MakeText(string name, Transform parent, Font font, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        static EnemyDefinition PickEnemy(ChapterDefinition def)
        {
            string hay = (def.location + " " + def.setPiece + " " + def.coreMechanic + " " + def.episodeTitle).ToLowerInvariant();
            string id = "construct";
            if (hay.Contains("enslaved")) id = "enslaved";
            else if (hay.Contains("robot") || hay.Contains("machine") || hay.Contains("bulldog") || hay.Contains("horseman") || hay.Contains("rashid")) id = "machine";
            else if (hay.Contains("black hole") || hay.Contains("namirash") || hay.Contains("darkness")) id = "darkness";
            return AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PrefabBuilders.EnemyFolder + "/" + id + ".asset");
        }

        static int EnemyCount(ChapterType type)
        {
            switch (type)
            {
                case ChapterType.Boss: return 3;
                case ChapterType.Combat:
                case ChapterType.Defence:
                case ChapterType.Duel:
                case ChapterType.Dungeon:
                case ChapterType.Training:
                case ChapterType.SetPiece:
                case ChapterType.Challenge:
                    return 3;
                default: return 0;
            }
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, bool trigger)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (trigger)
            {
                go.GetComponent<BoxCollider>().isTrigger = true;
                go.GetComponent<MeshRenderer>().enabled = false;
            }
            return go;
        }

        static void Label(Transform parent, string text, Vector3 position)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.35f;
            mesh.anchor = TextAnchor.MiddleCenter;
        }

        static bool IsKillObjective(string id)
        {
            for (int i = 0; i < KillWords.Length; i++)
            {
                if (id.Contains(KillWords[i])) return true;
            }
            return false;
        }

        public static void BuildChapterScene(ChapterDefinition def, string path)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Chapter_" + def.id);

            GameObject lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(root.transform, false);
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            int count = def.objectives.Count;
            float zEnd = Spacing * (count + 1);
            GameObject ground = Box("Ground", root.transform, new Vector3(0f, -0.5f, (zEnd + 10f) / 2f), new Vector3(30f, 1f, zEnd + 60f), false);
            NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();

            GameObject killer = Box("KillVolume", root.transform, new Vector3(0f, -25f, zEnd / 2f), new Vector3(500f, 2f, 500f), true);
            killer.AddComponent<KillVolume>();

            GameObject start = new GameObject("PlayerStart");
            start.transform.SetParent(root.transform, false);
            start.transform.position = new Vector3(0f, 1.1f, 0f);
            start.AddComponent<PlayerStart>();

            // Checkpoints: start is the player start; others are spread along the path.
            for (int i = 0; i < def.checkpointIds.Count; i++)
            {
                string id = def.checkpointIds[i];
                float z = i == 0 ? 0f : (id == "pre-boss" ? Spacing * (count - 1) - 8f : Spacing * Mathf.Max(1, count / 2) - 8f);
                GameObject anchorGo = new GameObject("Anchor_" + id);
                anchorGo.transform.SetParent(root.transform, false);
                anchorGo.transform.position = new Vector3(0f, 1.1f, z);
                anchorGo.AddComponent<CheckpointAnchor>().CheckpointId = id;
                if (i > 0)
                {
                    GameObject trigger = Box("Checkpoint_" + id, root.transform, new Vector3(0f, 2f, z), new Vector3(28f, 4f, 2f), true);
                    trigger.AddComponent<CheckpointTrigger>().CheckpointId = id;
                    Label(root.transform, "checkpoint " + id, new Vector3(0f, 4f, z));
                }
            }

            // Objectives in order along the path.
            List<string> done = new List<string>();
            bool killAssigned = false;
            int enemyCount = EnemyCount(def.chapterType);
            string lastTier1 = null;
            for (int i = 0; i < count; i++)
            {
                ObjectiveDef o = def.objectives[i];
                float z = Spacing * (i + 1);
                string[] prereq = done.ToArray();
                if (o.tier == 1) lastTier1 = o.id;

                if (o.id.Contains("collect-fragment-"))
                {
                    int index = int.Parse(o.id.Substring(o.id.LastIndexOf('-') + 1));
                    GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pickup.name = "Fragment" + index;
                    pickup.transform.SetParent(root.transform, false);
                    pickup.transform.position = new Vector3(0f, 1.5f, z);
                    pickup.GetComponent<SphereCollider>().isTrigger = true;
                    FragmentPickup fp = pickup.AddComponent<FragmentPickup>();
                    fp.FragmentIndex = index;
                    fp.ObjectiveId = o.id;
                    Label(root.transform, "fragment " + index, new Vector3(0f, 3.2f, z));
                }
                else if (o.id == "ch17.receive-call")
                {
                    GameObject choice = new GameObject("ImpulseChoice");
                    choice.transform.SetParent(root.transform, false);
                    ImpulseChoiceTrigger cautious = Box("Cautious_keep_going", choice.transform, new Vector3(-8f, 2f, z), new Vector3(10f, 4f, 3f), true).AddComponent<ImpulseChoiceTrigger>();
                    cautious.ObjectiveId = o.id;
                    cautious.Impulsive = false;
                    ImpulseChoiceTrigger impulsive = Box("Impulsive_turn_back", choice.transform, new Vector3(8f, 2f, z), new Vector3(10f, 4f, 3f), true).AddComponent<ImpulseChoiceTrigger>();
                    impulsive.ObjectiveId = o.id;
                    impulsive.Impulsive = true;
                    Label(root.transform, "Keep going (left) or turn back for the call (right)", new Vector3(0f, 4f, z));
                }
                else if (o.id == "ch26.free-parents")
                {
                    GameObject parents = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    parents.name = "CharmedParents";
                    parents.transform.SetParent(root.transform, false);
                    parents.transform.position = new Vector3(0f, 1f, z + 6f);
                    parents.AddComponent<ProtectedAlly>();
                    ObjectiveOnLightUsed free = parents.AddComponent<ObjectiveOnLightUsed>();
                    free.ObjectiveId = o.id;
                    Label(root.transform, "Free the parents with light (Flash). Do not strike them.", new Vector3(0f, 4f, z));
                }
                else if (!killAssigned && enemyCount > 0 && IsKillObjective(o.id))
                {
                    killAssigned = true;
                    GameObject marker = new GameObject("Kill_" + o.id);
                    marker.transform.SetParent(root.transform, false);
                    ObjectiveOnEnemiesDefeated kill = marker.AddComponent<ObjectiveOnEnemiesDefeated>();
                    kill.ObjectiveId = o.id;
                    kill.RequiredCount = enemyCount;
                    Label(root.transform, o.text + " (defeat all enemies)", new Vector3(0f, 4f, z));
                }
                else
                {
                    GameObject trigger = Box("Objective_" + o.id, root.transform, new Vector3(0f, 2f, z), new Vector3(28f, 4f, 3f), true);
                    ObjectiveTrigger ot = trigger.AddComponent<ObjectiveTrigger>();
                    ot.ObjectiveId = o.id;
                    ot.PrerequisiteIds = prereq;
                    Label(root.transform, o.text, new Vector3(0f, 4f, z));
                }
                done.Add(o.id);
            }

            GameObject exit = Box("Exit", root.transform, new Vector3(0f, 2f, zEnd), new Vector3(28f, 4f, 3f), true);
            exit.AddComponent<ChapterExitTrigger>();
            Label(root.transform, "Chapter exit", new Vector3(0f, 4f, zEnd));

            // Enemies, enabled after the NavMesh is baked at runtime.
            List<GameObject> toEnable = new List<GameObject>();
            if (enemyCount > 0)
            {
                EnemyDefinition enemyDef = PickEnemy(def);
                EnemyDefinition bossDef = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PrefabBuilders.EnemyFolder + "/boss.asset");
                float zMid = Spacing * Mathf.Max(1, count / 2) + 6f;
                for (int e = 0; e < enemyCount; e++)
                {
                    bool boss = def.chapterType == ChapterType.Boss && e == 0;
                    GameObject enemy = PrefabBuilders.BuildEnemyObject((boss ? "Boss" : "Enemy") + e, boss ? bossDef : enemyDef, new Vector3(-6f + e * 6f, 1f, zMid + (boss ? 6f : 0f)), boss ? 1.8f : 1f);
                    enemy.transform.SetParent(root.transform, true);
                    enemy.SetActive(false);
                    toEnable.Add(enemy);
                }
            }

            // Timers, stealth and corruption hazards from the chapter's failure rules.
            if (def.GetRule(FailureType.Objective) != null && lastTier1 != null && IsTimedType(def.chapterType))
            {
                GameObject timerGo = new GameObject("ObjectiveTimer");
                timerGo.transform.SetParent(root.transform, false);
                ObjectiveTimer timer = timerGo.AddComponent<ObjectiveTimer>();
                SerializedObject so = new SerializedObject(timer);
                so.FindProperty("objectiveId").stringValue = lastTier1;
                so.FindProperty("seconds").floatValue = 480f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (def.GetRule(FailureType.Stealth) != null)
            {
                GameObject watcher = Box("Watcher", root.transform, new Vector3(8f, 1f, Spacing * Mathf.Max(1, count / 2)), new Vector3(1f, 2f, 1f), false);
                watcher.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
                AwarenessMeter meter = watcher.AddComponent<AwarenessMeter>();
                AwarenessSensor sensor = watcher.AddComponent<AwarenessSensor>();
                PrefabBuilders.SetRef(sensor, "meter", meter);
            }
            if (def.GetRule(FailureType.Corruption) != null)
            {
                GameObject zone = Box("NamirashZone", root.transform, new Vector3(0f, 2f, Spacing * Mathf.Max(1, count / 2) + 12f), new Vector3(28f, 4f, 14f), true);
                zone.AddComponent<HazardZone>();
                SerializedObject zso = new SerializedObject(zone.GetComponent<HazardZone>());
                zso.FindProperty("kind").enumValueIndex = (int)HazardKind.Namirash;
                zso.FindProperty("corruptionSecondsToFail").floatValue = 12f;
                zso.ApplyModifiedPropertiesWithoutUndo();
            }

            RuntimeNavMeshBaker baker = root.AddComponent<RuntimeNavMeshBaker>();
            baker.Surface = surface;
            baker.EnableAfterBake = toEnable.ToArray();

            EditorSceneManager.SaveScene(scene, path);
        }

        static bool IsTimedType(ChapterType type)
        {
            return type == ChapterType.SetPiece || type == ChapterType.Escape || type == ChapterType.Rescue || type == ChapterType.Trek || type == ChapterType.Boss || type == ChapterType.Hub;
        }
    }
}
