using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Data
{
    [Serializable]
    public class IngredientAmount
    {
        public string itemId;
        public int count = 1;
    }

    [CreateAssetMenu(menuName = "Farhat/Potion Recipe")]
    public class PotionRecipe : ScriptableObject
    {
        public string id;
        public string displayName;
        public PotionFamily family;
        public int tier = 1;
        public float magnitude = 1f;
        public float durationSeconds = 0f;
        public string requiresUnlock;   // for example potion.heal, empty means always available
        public List<IngredientAmount> ingredients = new List<IngredientAmount>();
    }
}
