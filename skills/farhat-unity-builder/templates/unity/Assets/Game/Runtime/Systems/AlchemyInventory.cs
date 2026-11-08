using System;
using System.Collections.Generic;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>Herbs, materials and potions (bible 6.5). Crafting can be switched off by the story (chapter 13) and restored later.</summary>
    public class AlchemyInventory : MonoBehaviour, ISnapshotParticipant
    {
        [SerializeField] List<PotionRecipe> recipes = new List<PotionRecipe>();

        readonly Dictionary<string, int> items = new Dictionary<string, int>();
        readonly Dictionary<string, int> potions = new Dictionary<string, int>();
        bool craftingEnabled = true;
        UnlockManager unlocks;

        [Serializable] class Entry { public string id; public int count; }
        [Serializable]
        class State
        {
            public List<Entry> items = new List<Entry>();
            public List<Entry> potions = new List<Entry>();
            public bool craftingEnabled = true;
        }

        public string SnapshotKey { get { return "alchemy"; } }
        public bool CraftingEnabled { get { return craftingEnabled; } }
        public IList<PotionRecipe> Recipes { get { return recipes; } }

        void OnEnable()
        {
            ServiceLocator.TryGet(out unlocks);
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Register(this);
        }

        void OnDisable()
        {
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Unregister(this);
        }

        public void SetCraftingEnabled(bool value) { craftingEnabled = value; }

        public void Gather(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
            int current;
            items.TryGetValue(itemId, out current);
            items[itemId] = current + amount;
        }

        public int Count(string itemId)
        {
            int n;
            return items.TryGetValue(itemId, out n) ? n : 0;
        }

        public int PotionCount(string recipeId)
        {
            int n;
            return potions.TryGetValue(recipeId, out n) ? n : 0;
        }

        public bool IsRecipeAvailable(PotionRecipe recipe)
        {
            if (recipe == null) return false;
            if (string.IsNullOrEmpty(recipe.requiresUnlock)) return true;
            return unlocks != null && unlocks.Has(recipe.requiresUnlock);
        }

        public bool CanCraft(PotionRecipe recipe)
        {
            if (!craftingEnabled || !IsRecipeAvailable(recipe)) return false;
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                if (Count(recipe.ingredients[i].itemId) < recipe.ingredients[i].count) return false;
            }
            return true;
        }

        public bool TryCraft(PotionRecipe recipe)
        {
            if (!CanCraft(recipe)) return false;
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                items[recipe.ingredients[i].itemId] = Count(recipe.ingredients[i].itemId) - recipe.ingredients[i].count;
            }
            potions[recipe.id] = PotionCount(recipe.id) + 1;
            return true;
        }

        public bool UsePotion(string recipeId)
        {
            if (PotionCount(recipeId) <= 0) return false;
            PotionRecipe recipe = recipes.Find(r => r != null && r.id == recipeId);
            if (recipe == null) return false;
            potions[recipeId] = PotionCount(recipeId) - 1;
            switch (recipe.family)
            {
                case PotionFamily.Heal:
                    HealthComponent health = GetComponent<HealthComponent>();
                    if (health != null) health.Heal(recipe.magnitude);
                    break;
                case PotionFamily.Boost:
                    PlayerController player = GetComponent<PlayerController>();
                    if (player != null) player.AddSpeedBoost(1f + 0.4f * recipe.magnitude, recipe.durationSeconds);
                    break;
            }
            EventBus<PotionUsed>.Publish(new PotionUsed(recipe.family));
            return true;
        }

        /// <summary>Removes up to the requested number of items, taking from the largest stacks first. Used by the impulse-trap penalty.</summary>
        public int Lose(int amount)
        {
            int removed = 0;
            for (int n = 0; n < amount; n++)
            {
                string bestId = null;
                int best = 0;
                foreach (KeyValuePair<string, int> pair in items)
                {
                    if (pair.Value > best || (pair.Value == best && best > 0 && string.CompareOrdinal(pair.Key, bestId) < 0))
                    {
                        best = pair.Value;
                        bestId = pair.Key;
                    }
                }
                if (bestId == null) break;
                items[bestId] = best - 1;
                removed++;
            }
            return removed;
        }

        public string CaptureSnapshot()
        {
            State s = new State();
            foreach (KeyValuePair<string, int> pair in items) s.items.Add(new Entry { id = pair.Key, count = pair.Value });
            foreach (KeyValuePair<string, int> pair in potions) s.potions.Add(new Entry { id = pair.Key, count = pair.Value });
            s.craftingEnabled = craftingEnabled;
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            State s = JsonUtility.FromJson<State>(json);
            items.Clear();
            potions.Clear();
            for (int i = 0; i < s.items.Count; i++) items[s.items[i].id] = s.items[i].count;
            for (int i = 0; i < s.potions.Count; i++) potions[s.potions[i].id] = s.potions[i].count;
            craftingEnabled = s.craftingEnabled;
        }
    }
}
