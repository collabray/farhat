using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Core
{
    /// <summary>Pool for frequently spawned objects (hit effects, projectiles). Avoids per-frame allocation and instantiation.</summary>
    public sealed class GameObjectPool
    {
        readonly GameObject prefab;
        readonly Transform parent;
        readonly Stack<GameObject> free = new Stack<GameObject>();

        public GameObjectPool(GameObject prefab, Transform parent, int prewarm)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                GameObject go = Object.Instantiate(prefab, parent);
                go.SetActive(false);
                free.Push(go);
            }
        }

        public int FreeCount { get { return free.Count; } }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject go = free.Count > 0 ? free.Pop() : Object.Instantiate(prefab, parent);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            go.SetActive(false);
            free.Push(go);
        }
    }
}
