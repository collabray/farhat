using Unity.AI.Navigation;
using UnityEngine;

namespace Farhat.Systems
{
    /// <summary>
    /// Bakes the NavMesh when a chapter scene loads, then enables the agents that need it (enemies, companions).
    /// Generated greybox scenes use this so no editor bake step is needed; hand-built scenes may bake in the editor instead.
    /// </summary>
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        [SerializeField] NavMeshSurface surface;
        [SerializeField] GameObject[] enableAfterBake = new GameObject[0];

        public NavMeshSurface Surface { get { return surface; } set { surface = value; } }
        public GameObject[] EnableAfterBake { get { return enableAfterBake; } set { enableAfterBake = value; } }

        void Start()
        {
            if (surface != null) surface.BuildNavMesh();
            for (int i = 0; i < enableAfterBake.Length; i++)
            {
                if (enableAfterBake[i] != null) enableAfterBake[i].SetActive(true);
            }
        }
    }
}
