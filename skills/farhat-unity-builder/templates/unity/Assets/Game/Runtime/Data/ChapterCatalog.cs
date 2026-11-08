using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Data
{
    /// <summary>All 52 chapter definitions. Stored at Assets/Resources/ChapterCatalog.asset.</summary>
    [CreateAssetMenu(menuName = "Farhat/Chapter Catalog")]
    public class ChapterCatalog : ScriptableObject
    {
        public const string ResourceName = "ChapterCatalog";
        public const int ChapterCount = 52;

        public List<ChapterDefinition> chapters = new List<ChapterDefinition>();

        public int Count { get { return chapters.Count; } }

        public ChapterDefinition GetByNumber(int number)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i] != null && chapters[i].number == number) return chapters[i];
            }
            return null;
        }

        public static ChapterCatalog LoadDefault()
        {
            return Resources.Load<ChapterCatalog>(ResourceName);
        }
    }
}
