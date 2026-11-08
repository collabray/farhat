using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>
    /// Turns a ChapterDefinition into a played chapter: loads the chapter scene additively, spawns the player, initialises objectives,
    /// records the start checkpoint, and on completion grants unlocks, rates the chapter and writes the save.
    /// </summary>
    public class ChapterRunner : MonoBehaviour
    {
        [SerializeField] ChapterCatalog catalog;
        [SerializeField] GameObject playerPrefab;
        [SerializeField] ThirdPersonCamera cameraRig;
        [SerializeField] int saveSlot = 0;

        float chapterStartedAt;
        string loadedScene;

        public ChapterDefinition Chapter { get; private set; }
        public GameObject Player { get; private set; }
        public HealthComponent PlayerHealth { get; private set; }
        public ShadowController Shadow { get; private set; }
        public AlchemyInventory Inventory { get; private set; }
        public ObjectiveTracker Objectives { get; private set; }
        public HintService Hints { get; private set; }
        public CheckpointStore Checkpoints { get; private set; }
        public UnlockManager Unlocks { get; private set; }
        public SaveSystem Saves { get; private set; }
        public bool IsRunning { get; private set; }
        public int OptionalFound { get; set; }
        public int OptionalTotal { get; set; }

        public ChapterCatalog Catalog { get { return catalog; } set { catalog = value; } }
        public GameObject PlayerPrefab { get { return playerPrefab; } set { playerPrefab = value; } }
        public ThirdPersonCamera CameraRig { get { return cameraRig; } set { cameraRig = value; } }

        void OnEnable() { ServiceLocator.Register<ChapterRunner>(this); }
        void OnDisable() { ServiceLocator.Unregister<ChapterRunner>(); }

        public IEnumerator StartChapter(int number, string checkpointId)
        {
            Saves = ServiceLocator.Get<SaveSystem>();
            Unlocks = ServiceLocator.Get<UnlockManager>();
            Checkpoints = ServiceLocator.Get<CheckpointStore>();

            Chapter = catalog != null ? catalog.GetByNumber(number) : null;
            if (Chapter == null)
            {
                Debug.LogError("[Chapter] No ChapterDefinition for chapter " + number + ". Run Farhat > Content > Import Chapters.");
                yield break;
            }

            Checkpoints.Clear();
            AsyncOperation load = SceneManager.LoadSceneAsync(Chapter.sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError("[Chapter] Scene '" + Chapter.sceneName + "' is not in the build settings. Run Farhat > Content > Generate Greybox Scenes.");
                yield break;
            }
            while (!load.isDone) yield return null;
            loadedScene = Chapter.sceneName;

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (PlayerStart.All.Count > 0)
            {
                position = PlayerStart.All[0].transform.position;
                rotation = PlayerStart.All[0].transform.rotation;
            }
            CheckpointAnchor anchor = string.IsNullOrEmpty(checkpointId) ? null : CheckpointAnchor.Find(checkpointId);
            if (anchor != null)
            {
                position = anchor.transform.position;
                rotation = anchor.transform.rotation;
            }

            if (Player != null) Destroy(Player);
            Player = Instantiate(playerPrefab, position, rotation);
            PlayerHealth = Player.GetComponent<HealthComponent>();
            Shadow = Player.GetComponent<ShadowController>();
            Inventory = Player.GetComponent<AlchemyInventory>();
            if (PlayerHealth != null)
            {
                PlayerHealth.SetMaxHearts(5 + Unlocks.BonusHearts, true);
                PlayerHealth.ResetChapterStats();
            }
            if (cameraRig != null) cameraRig.Target = Player.transform;

            SaveData save = Saves.Current;
            Objectives = new ObjectiveTracker();
            Objectives.Initialize(Chapter, save.chapter == number ? save.completedObjectives : null);
            Hints = new HintService(Objectives);
            ServiceLocator.Register(Objectives);
            ServiceLocator.Register(Hints);
            Checkpoints.Register(Objectives);
            OptionalFound = 0;

            yield return null; // let components register as snapshot participants
            Checkpoints.Record("start");
            chapterStartedAt = Time.time;
            IsRunning = true;
            EventBus<ChapterStarted>.Publish(new ChapterStarted(number));
        }

        public void RecordCheckpoint(string id)
        {
            if (!IsRunning || Checkpoints == null) return;
            if (!Chapter.HasCheckpoint(id)) { Debug.LogWarning("[Chapter] Checkpoint '" + id + "' is not declared by chapter " + Chapter.number); }
            Checkpoints.Record(id);
        }

        public void CompleteChapter()
        {
            if (!IsRunning) return;
            IsRunning = false;
            for (int i = 0; i < Chapter.unlocks.Count; i++) Unlocks.Grant(Chapter.unlocks[i]);

            SaveData data = Saves.Current;
            Unlocks.WriteTo(data);
            float lost = PlayerHealth != null ? PlayerHealth.HeartsLostThisChapter : 0f;
            float seconds = Time.time - chapterStartedAt;
            data.SetStars(Chapter.number, ChapterScoring.Stars(lost, seconds, OptionalFound, OptionalTotal, Chapter.costTier));
            data.playSeconds += seconds;
            data.chapter = Mathf.Min(Chapter.number + 1, ChapterCatalog.ChapterCount);
            data.checkpointId = "start";
            data.completedObjectives.Clear();
            data.hearts = 5 + Unlocks.BonusHearts;
            Saves.Write(saveSlot, data);
            EventBus<ChapterCompleted>.Publish(new ChapterCompleted(Chapter.number));
        }

        public IEnumerator EndChapter()
        {
            if (Player != null) Destroy(Player);
            Player = null;
            if (Objectives != null && Checkpoints != null) Checkpoints.Unregister(Objectives);
            if (!string.IsNullOrEmpty(loadedScene))
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedScene);
                if (unload != null) { while (!unload.isDone) yield return null; }
                loadedScene = null;
            }
        }
    }
}
