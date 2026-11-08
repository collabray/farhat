using UnityEngine;
using Farhat.Data;

namespace Farhat.Story
{
    /// <summary>Light-star rating from the bible (section 7): based on health lost, time taken and optional items found. Stars unlock cosmetics and gallery items only.</summary>
    public static class ChapterScoring
    {
        public static float ParSeconds(CostTier tier)
        {
            switch (tier)
            {
                case CostTier.A: return 35f * 60f;
                case CostTier.B: return 28f * 60f;
                default: return 20f * 60f;
            }
        }

        public static int Stars(float heartsLost, float seconds, int optionalFound, int optionalTotal, CostTier tier)
        {
            int stars = 1;
            if (heartsLost <= 3f) stars++;
            bool enoughOptional = optionalTotal <= 0 || optionalFound * 2 >= optionalTotal;
            if (seconds <= ParSeconds(tier) && enoughOptional) stars++;
            return Mathf.Clamp(stars, 1, 3);
        }
    }
}
