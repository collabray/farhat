using System;
using System.IO;
using UnityEngine;

namespace Farhat.Save
{
    /// <summary>
    /// Versioned JSON saves. A write goes to a temporary file first, the previous good save is kept as a backup,
    /// and a failed read falls back to the backup, so a crash in the middle of a write never destroys progress.
    /// </summary>
    public sealed class SaveSystem
    {
        readonly string directory;
        public SaveData Current { get; private set; }

        public SaveSystem() : this(Application.persistentDataPath) { }

        public SaveSystem(string directory)
        {
            this.directory = directory;
            Current = new SaveData();
        }

        public string PathFor(int slot) { return Path.Combine(directory, "save_slot_" + slot + ".json"); }
        string BackupFor(int slot) { return PathFor(slot) + ".bak"; }
        string TempFor(int slot) { return PathFor(slot) + ".tmp"; }

        public bool Exists(int slot) { return File.Exists(PathFor(slot)) || File.Exists(BackupFor(slot)); }

        public void Write(int slot, SaveData data)
        {
            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(data, true);
            string tmp = TempFor(slot);
            File.WriteAllText(tmp, json);
            string main = PathFor(slot);
            if (File.Exists(main))
            {
                File.Copy(main, BackupFor(slot), true);
                File.Delete(main);
            }
            File.Move(tmp, main);
            Current = data;
        }

        public SaveData Load(int slot)
        {
            SaveData data = TryRead(PathFor(slot));
            if (data == null) data = TryRead(BackupFor(slot));
            if (data == null) data = new SaveData();
            Current = SaveMigrator.Migrate(data);
            return Current;
        }

        public void Delete(int slot)
        {
            if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot));
            if (File.Exists(BackupFor(slot))) File.Delete(BackupFor(slot));
            if (File.Exists(TempFor(slot))) File.Delete(TempFor(slot));
        }

        static SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not read " + path + ": " + e.Message);
                return null;
            }
        }
    }
}
