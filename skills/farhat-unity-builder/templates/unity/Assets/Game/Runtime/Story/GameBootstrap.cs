using System.Collections;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>
    /// Lives in the Bootstrap scene. Registers the shared services, loads the save, and runs chapters one after another until the last.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] ChapterRunner runner;
        [SerializeField] int saveSlot = 0;
        [SerializeField] bool autoStart = true;
        [SerializeField] int startChapterOverride = 0;

        bool chapterDone;
        PlayerInputActions input;

        /// <summary>Test hooks. Tests set these before loading the Bootstrap scene to avoid auto-start and to keep saves out of the real save folder.</summary>
        public static bool SuppressAutoStart;
        public static string SaveDirectoryOverride;

        public bool GameFinished { get; private set; }
        public ChapterRunner Runner { get { return runner; } }

        void Awake()
        {
            SaveSystem saves = string.IsNullOrEmpty(SaveDirectoryOverride) ? new SaveSystem() : new SaveSystem(SaveDirectoryOverride);
            SaveData data = saves.Load(saveSlot);
            UnlockManager unlocks = new UnlockManager();
            unlocks.LoadFrom(data);
            input = new PlayerInputActions();

            ServiceLocator.Register(saves);
            ServiceLocator.Register(unlocks);
            ServiceLocator.Register(new CheckpointStore());
            ServiceLocator.Register(input);
            ServiceLocator.Register<GameBootstrap>(this);
        }

        void OnEnable() { EventBus<ChapterCompleted>.Subscribe(OnChapterCompleted); }
        void OnDisable() { EventBus<ChapterCompleted>.Unsubscribe(OnChapterCompleted); }

        void OnDestroy()
        {
            if (input != null) input.Dispose();
            ServiceLocator.Clear();
        }

        void OnChapterCompleted(ChapterCompleted e) { chapterDone = true; }

        void Start()
        {
            if (autoStart && !SuppressAutoStart) StartCoroutine(Run());
        }

        public IEnumerator Run()
        {
            SaveData save = ServiceLocator.Get<SaveSystem>().Current;
            int number = startChapterOverride > 0 ? startChapterOverride : Mathf.Clamp(save.chapter, 1, ChapterCatalog.ChapterCount);
            string checkpoint = startChapterOverride > 0 ? "start" : save.checkpointId;
            return RunFrom(number, checkpoint, false);
        }

        /// <summary>Runs chapters from the given one. With single set, stops after that chapter (chapter select and tests).</summary>
        public IEnumerator RunFrom(int number, string checkpoint, bool single)
        {
            while (number <= ChapterCatalog.ChapterCount)
            {
                chapterDone = false;
                yield return runner.StartChapter(number, checkpoint);
                if (!runner.IsRunning) yield break;
                while (!chapterDone) yield return null;
                yield return runner.EndChapter();
                if (single) yield break;
                number++;
                checkpoint = "start";
            }
            GameFinished = true;
        }

        /// <summary>Chapter select. Allowed for any chapter up to the furthest reached, or any chapter after the chapter-select unlock.</summary>
        public bool CanSelect(int number)
        {
            SaveData save = ServiceLocator.Get<SaveSystem>().Current;
            UnlockManager unlocks = ServiceLocator.Get<UnlockManager>();
            return number >= 1 && number <= ChapterCatalog.ChapterCount && (number <= save.chapter || unlocks.Has("chapter-select"));
        }
    }
}
