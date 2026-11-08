// Minimal API stubs of the Unity types used by the templates. They exist only so the C# can be type-checked with
// Mono's mcs compiler outside Unity. They are NOT Unity, do not ship them in a project, and do not prove runtime behaviour.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DestroyImmediate(Object o) { }
        public static T Instantiate<T>(T original) where T : Object { return original; }
        public static T Instantiate<T>(T original, Transform parent) where T : Object { return original; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object { return original; }
        public static implicit operator bool(Object o) { return o != null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : Object { return null; }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
    }
    public class Component : Object
    {
        public GameObject gameObject; public Transform transform; public string tag;
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return null; }
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public void StopCoroutine(Coroutine c) { }
        public void StopAllCoroutines() { }
    }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject { return null; } }
    public class GameObject : Object
    {
        public Transform transform; public string tag; public bool activeSelf;
        public GameObject() { } public GameObject(string name) { }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public void SetActive(bool v) { }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public class Transform : Component
    {
        public Vector3 position, localPosition, localScale, eulerAngles, forward, right, up; public Quaternion rotation; public Transform parent;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, offsetMin, offsetMax;
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero, one, up, back, forward;
        public float magnitude { get { return 0f; } }
        public float sqrMagnitude { get { return 0f; } }
        public Vector3 normalized { get { return this; } }
        public void Normalize() { }
        public static float Distance(Vector3 a, Vector3 b) { return 0f; }
        public static float Angle(Vector3 a, Vector3 b) { return 0f; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float d) { return a; }
        public static Vector3 operator *(float d, Vector3 a) { return a; }
    }
    public struct Quaternion
    {
        public static Quaternion identity;
        public static Quaternion Euler(float x, float y, float z) { return identity; }
        public static Quaternion LookRotation(Vector3 f, Vector3 u) { return identity; }
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) { return a; }
        public static Vector3 operator *(Quaternion q, Vector3 v) { return v; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public static Color white, black;
    }
    public static class Mathf
    {
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
        public static int Clamp(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        public static float Abs(float v) { return v < 0 ? -v : v; }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static float Sin(float v) { return (float)Math.Sin(v); }
        public static float Cos(float v) { return (float)Math.Cos(v); }
        public static int RoundToInt(float v) { return (int)Math.Round(v); }
    }
    public static class Time
    {
        public static float time, deltaTime, unscaledDeltaTime, timeScale, realtimeSinceStartup;
    }
    public static class Debug
    {
        public static void Log(System.Object m) { } public static void LogWarning(System.Object m) { } public static void LogError(System.Object m) { }
    }
    public static class Application
    {
        public static string persistentDataPath; public static bool isBatchMode; public static bool isPlaying;
    }
    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }
    public static class JsonUtility
    {
        public static string ToJson(System.Object o) { return null; }
        public static string ToJson(System.Object o, bool pretty) { return null; }
        public static T FromJson<T>(string json) { return default(T); }
    }
    public static class PlayerPrefs
    {
        public static int GetInt(string k, int d) { return d; } public static void SetInt(string k, int v) { }
        public static float GetFloat(string k, float d) { return d; } public static void SetFloat(string k, float v) { }
        public static void Save() { }
    }
    public static class Physics
    {
        public static Collider[] OverlapSphere(Vector3 p, float r) { return null; }
        public static Collider[] OverlapSphere(Vector3 p, float r, int mask) { return null; }
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit hit, float max, int mask) { hit = default(RaycastHit); return false; }
        public static bool Linecast(Vector3 a, Vector3 b, int mask) { return false; }
    }
    public struct RaycastHit { public float distance; }
    public struct LayerMask { public static implicit operator int(LayerMask m) { return 0; } public static implicit operator LayerMask(int i) { return default(LayerMask); } }
    public class Collider : Component { public bool isTrigger; }
    public class BoxCollider : Collider { public Vector3 size; }
    public class SphereCollider : Collider { }
    public class CapsuleCollider : Collider { }
    public class CharacterController : Collider
    {
        public bool isGrounded; public float height, radius; public Vector3 center;
        public bool enabled;
        public void Move(Vector3 motion) { }
    }
    public class Renderer : Component { public Material material; public Material sharedMaterial; public bool enabled; }
    public class MeshRenderer : Renderer { }
    public class Material : Object { public Color color; }
    public class Camera : Behaviour { public static Camera main; }
    public class AudioListener : Behaviour { }
    public enum LightType { Directional, Spot, Point }
    public class Light : Behaviour { public LightType type; }
    public class TextMesh : Component { public string text; public float characterSize; public TextAnchor anchor; }
    public class Font : Object { }
    public class Sprite : Object { }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public class Canvas : Behaviour { public RenderMode renderMode; }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class CanvasGroup : Behaviour { public float alpha; public bool blocksRaycasts; }
    public class Coroutine { }
    public class YieldInstruction { }
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public class WaitForFixedUpdate : YieldInstruction { }
    public enum FindObjectsSortMode { None, InstanceID }
    public class AsyncOperation { public bool isDone; public float progress; }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration, AfterAssembliesLoaded, BeforeSplashScreen, BeforeSceneLoad, AfterSceneLoad }
    public class SerializeFieldAttribute : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a, int b) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type t) { } }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class TextAsset : Object { public string text; }
}
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }
    public struct Scene { }
    public static class SceneManager
    {
        public static AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode) { return null; }
        public static AsyncOperation UnloadSceneAsync(string name) { return null; }
    }
}
namespace UnityEngine.AI
{
    public struct NavMeshHit { public Vector3 position; }
    public static class NavMesh
    {
        public const int AllAreas = -1;
        public static bool SamplePosition(Vector3 p, out NavMeshHit hit, float d, int mask) { hit = default(NavMeshHit); return false; }
    }
    public class NavMeshAgent : Behaviour
    {
        public float speed, stoppingDistance, radius, height; public bool isStopped; public Vector3 velocity;
        public bool SetDestination(Vector3 t) { return true; }
        public bool Warp(Vector3 p) { return true; }
    }
}
namespace UnityEngine.UI
{
    public class Graphic : Behaviour { public Color color; }
    public class Image : Graphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical }
        public Sprite sprite; public Type type; public FillMethod fillMethod; public float fillAmount;
    }
    public class Text : Graphic { public string text; public int fontSize; public Font font; public TextAnchor alignment; }
    public class CanvasScaler : Behaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; }
    public class GraphicRaycaster : Behaviour { }
}
namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }
    public class InputAction : IDisposable
    {
        public InputAction(string name = null, InputActionType type = InputActionType.Value, string binding = null) { }
        public CompositeSyntax AddCompositeBinding(string composite) { return new CompositeSyntax(); }
        public void AddBinding(string path) { }
        public T ReadValue<T>() where T : struct { return default(T); }
        public bool WasPressedThisFrame() { return false; }
        public bool IsPressed() { return false; }
        public void Enable() { } public void Disable() { } public void Dispose() { }
        public struct CompositeSyntax { public CompositeSyntax With(string name, string path) { return this; } }
    }
}
namespace UnityEngine.TestTools { public class UnityTestAttribute : Attribute { } }
namespace NUnit.Framework { public class ValueSourceAttribute : Attribute { public ValueSourceAttribute(string n) { } } }
namespace Unity.AI.Navigation { using UnityEngine; public class NavMeshSurface : Behaviour { public void BuildNavMesh() { } } }
namespace NUnit.Framework
{
    public class TestAttribute : Attribute { }
    public class SetUpAttribute : Attribute { }
    public class TearDownAttribute : Attribute { }
    public static class Assert
    {
        public static void AreEqual(System.Object a, System.Object b) { } public static void AreEqual(System.Object a, System.Object b, string m) { }
        public static void Ignore(string m) { }
        public static void AreSame(System.Object a, System.Object b) { }
        public static void IsTrue(bool c) { } public static void IsTrue(bool c, string m) { }
        public static void IsFalse(bool c) { } public static void IsFalse(bool c, string m) { }
        public static void IsNotNull(System.Object o) { } public static void IsNotNull(System.Object o, string m) { }
        public static void Greater(float a, float b) { } public static void Greater(float a, float b, string m) { }
        public static void GreaterOrEqual(float a, float b) { }
        public static void Less(float a, float b) { }
        public static void Throws<T>(Action a) where T : System.Exception { }
    }
    public static class CollectionAssert { public static void AreEqual(System.Collections.IEnumerable a, System.Collections.IEnumerable b) { } }
}
namespace UnityEditor
{
    using UnityEngine;
    public class MenuItemAttribute : Attribute { public MenuItemAttribute(string path) { } }
    public static class EditorUtility
    {
        public static void SetDirty(Object o) { }
        public static void DisplayProgressBar(string t, string i, float p) { } public static void ClearProgressBar() { }
    }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string path) where T : Object { return null; }
        public static void CreateAsset(Object o, string path) { }
        public static void SaveAssets() { } public static void Refresh() { }
        public static bool IsValidFolder(string p) { return true; }
        public static string CreateFolder(string parent, string name) { return null; }
        public static string[] FindAssets(string filter) { return null; }
        public static Object[] LoadAllAssetsAtPath(string path) { return null; }
        public static T GetBuiltinExtraResource<T>(string path) where T : Object { return null; }
    }
    public class SceneAsset : Object { }
    public static class PrefabUtility { public static GameObject SaveAsPrefabAsset(GameObject go, string path) { return go; } }
    public class SerializedObject
    {
        public SerializedObject(Object o) { }
        public SerializedProperty FindProperty(string n) { return null; }
        public void ApplyModifiedPropertiesWithoutUndo() { }
    }
    public class SerializedProperty
    {
        public Object objectReferenceValue; public int arraySize; public string stringValue; public float floatValue; public int enumValueIndex; public int intValue;
        public SerializedProperty GetArrayElementAtIndex(int i) { return null; }
    }
    public class EditorWindow : ScriptableObject
    {
        public static T GetWindow<T>(string title) where T : EditorWindow { return null; }
    }
    public static class EditorGUILayout
    {
        public static void HelpBox(string m, MessageType t) { }
        public static bool ToggleLeft(string l, bool v) { return v; }
        public static bool Toggle(bool v, params GUILayoutOption[] o) { return v; }
        public static Vector2 BeginScrollView(Vector2 s) { return s; } public static void EndScrollView() { }
        public static void BeginHorizontal() { } public static void EndHorizontal() { }
    }
    public enum MessageType { None, Info, Warning, Error }
    public static class EditorApplication { public static void Exit(int code) { } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public class EditorBuildSettingsScene
    {
        public string path; public bool enabled;
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }
    public enum BuildTarget { StandaloneWindows64, StandaloneLinux64, StandaloneOSX }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public enum BuildOptions { None }
    public static class BuildPipeline { public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) { return null; } }
    public static class PlayerSettings
    {
        public static ColorSpace colorSpace; public static string productName, companyName;
        public static string GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget t) { return string.Empty; }
        public static void SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget t, string d) { }
    }
}
namespace UnityEngine { public enum ColorSpace { Gamma, Linear } public class GUILayoutOption { } public static class GUILayout {
    public static void Label(string t, params GUILayoutOption[] o) { }
    public static bool Button(string t, params GUILayoutOption[] o) { return false; }
    public static GUILayoutOption Width(float w) { return null; } } }
namespace UnityEditor.Build
{
    using System;
    public struct NamedBuildTarget { public static NamedBuildTarget Standalone; }
    public interface IPreprocessBuildWithReport { int callbackOrder { get; } void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
    public class BuildFailedException : Exception { public BuildFailedException(string m) : base(m) { } }
}
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed }
    public class BuildSummary { public BuildResult result; public int totalErrors, totalWarnings; }
    public class BuildReport { public BuildSummary summary; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) { return default(UnityEngine.SceneManagement.Scene); }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string path) { return true; }
        public static UnityEngine.SceneManagement.Scene OpenScene(string path) { return default(UnityEngine.SceneManagement.Scene); }
    }
}
namespace UnityEditor.PackageManager
{
    public enum StatusCode { InProgress, Success, Failure }
    public class Error { public string message; }
    public class PackageInfo { public string version; }
    public class AddRequest { public bool IsCompleted; public StatusCode Status; public Error Error; public PackageInfo Result; }
    public static class Client { public static AddRequest Add(string id) { return null; } }
}
