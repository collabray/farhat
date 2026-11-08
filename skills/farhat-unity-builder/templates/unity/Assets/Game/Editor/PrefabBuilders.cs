using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Farhat.Data;
using Farhat.Systems;

namespace Farhat.EditorTools
{
    /// <summary>Creates the Player prefab, enemy definitions and potion recipes that the generated greybox scenes use.</summary>
    public static class PrefabBuilders
    {
        public const string PrefabFolder = "Assets/Game/Content/Prefabs";
        public const string EnemyFolder = "Assets/Game/Content/Enemies";
        public const string RecipeFolder = "Assets/Game/Content/Recipes";
        public const string PlayerPath = PrefabFolder + "/Player.prefab";

        public static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) throw new System.InvalidOperationException("Field '" + field + "' not found on " + target.GetType().Name);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) throw new System.InvalidOperationException("Field '" + field + "' not found on " + target.GetType().Name);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static List<PotionRecipe> BuildRecipes()
        {
            ChapterCatalogImporter.EnsureFolder(RecipeFolder);
            List<PotionRecipe> list = new List<PotionRecipe>();
            list.Add(Recipe("potion.heal", "Heal", PotionFamily.Heal, 1f, 0f, "potion.heal", "herb.desert", 2));
            list.Add(Recipe("potion.boost", "Boost", PotionFamily.Boost, 1f, 20f, "potion.boost", "herb.desert", 1, "herb.jungle", 1));
            list.Add(Recipe("potion.cure", "Cure", PotionFamily.Cure, 1f, 0f, "potion.cure", "herb.jungle", 2));
            list.Add(Recipe("potion.transform", "Transform", PotionFamily.Transform, 1f, 120f, "potion.transform", "plant.base", 2, "herb.desert", 1));
            list.Add(Recipe("potion.weather", "Weather", PotionFamily.Weather, 1f, 60f, "potion.weather", "plant.base", 3));
            return list;
        }

        static PotionRecipe Recipe(string id, string name, PotionFamily family, float magnitude, float duration, string unlock, string itemA, int countA, string itemB = null, int countB = 0)
        {
            string path = RecipeFolder + "/" + id + ".asset";
            PotionRecipe recipe = AssetDatabase.LoadAssetAtPath<PotionRecipe>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<PotionRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }
            recipe.id = id;
            recipe.displayName = name;
            recipe.family = family;
            recipe.magnitude = magnitude;
            recipe.durationSeconds = duration;
            recipe.requiresUnlock = unlock;
            recipe.ingredients = new List<IngredientAmount>();
            IngredientAmount a = new IngredientAmount();
            a.itemId = itemA;
            a.count = countA;
            recipe.ingredients.Add(a);
            if (itemB != null)
            {
                IngredientAmount b = new IngredientAmount();
                b.itemId = itemB;
                b.count = countB;
                recipe.ingredients.Add(b);
            }
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        public static EnemyDefinition BuildEnemy(string id, EnemyFamily family, float health, float damage, float speed, bool nonLethal)
        {
            ChapterCatalogImporter.EnsureFolder(EnemyFolder);
            string path = EnemyFolder + "/" + id + ".asset";
            EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = id;
            def.family = family;
            def.maxHealth = health;
            def.contactDamage = damage;
            def.moveSpeed = speed;
            def.nonLethal = nonLethal;
            EditorUtility.SetDirty(def);
            return def;
        }

        public static void BuildEnemies()
        {
            BuildEnemy("construct", EnemyFamily.Construct, 30f, 0.5f, 3.2f, false);
            BuildEnemy("machine", EnemyFamily.Machine, 45f, 0.5f, 3.6f, false);
            BuildEnemy("darkness", EnemyFamily.Darkness, 40f, 0.5f, 3.4f, false);
            BuildEnemy("enslaved", EnemyFamily.Enslaved, 60f, 0.5f, 3.8f, true);
            BuildEnemy("boss", EnemyFamily.Construct, 220f, 1f, 3.4f, false);
        }

        public static GameObject BuildPlayerPrefab()
        {
            ChapterCatalogImporter.EnsureFolder(PrefabFolder);
            List<PotionRecipe> recipes = BuildRecipes();

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);
            player.transform.position = Vector3.zero;

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.4f, 0.45f);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);

            GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proxy.name = "ShadowProxy";
            Object.DestroyImmediate(proxy.GetComponent<SphereCollider>());
            proxy.transform.SetParent(player.transform, false);
            proxy.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
            GameObject shapeBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shapeBox.name = "ShapeCrate";
            Object.DestroyImmediate(shapeBox.GetComponent<BoxCollider>());
            shapeBox.transform.SetParent(proxy.transform, false);
            shapeBox.transform.localPosition = new Vector3(0f, 1f, 0f);
            shapeBox.SetActive(false);
            GameObject shapeBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shapeBall.name = "ShapeBarrel";
            Object.DestroyImmediate(shapeBall.GetComponent<SphereCollider>());
            shapeBall.transform.SetParent(proxy.transform, false);
            shapeBall.transform.localPosition = new Vector3(0f, 1f, 0f);
            shapeBall.SetActive(false);
            proxy.SetActive(false);

            player.AddComponent<PlayerController>();
            player.AddComponent<HealthComponent>();
            player.AddComponent<LightMeter>();
            player.AddComponent<CombatController>();
            ShadowController shadow = player.AddComponent<ShadowController>();
            AlchemyInventory alchemy = player.AddComponent<AlchemyInventory>();
            player.AddComponent<SafePointRecorder>();

            SetRef(shadow, "proxy", proxy.transform);
            SetArray(shadow, "shapeObjects", new Object[] { shapeBox, shapeBall });
            SetArray(alchemy, "recipes", recipes.ToArray());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            Object.DestroyImmediate(player);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        public static GameObject BuildEnemyObject(string name, EnemyDefinition def, Vector3 position, float scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f * scale;
            agent.height = 2f * scale;
            EnemyBrain brain = go.AddComponent<EnemyBrain>();
            SetRef(brain, "definition", def);
            SetRef(brain, "telegraphRenderer", go.GetComponent<MeshRenderer>());
            return go;
        }
    }
}
