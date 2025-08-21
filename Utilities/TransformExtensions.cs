using System;
using System.Collections.Generic;
using Game.PoolSystem;
using UnityEngine;

namespace Game.Extensions
{
    /// <summary>
    /// Extension methods for UnityEngine.Transform.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Rotates this transform so its forward vector points directly away from the specified world position.
        /// </summary>
        /// <param name="transform">The transform to rotate.</param>
        /// <param name="targetPosition">World position of the target.</param>
        public static void LookAtInvert(this Transform transform, Vector3 targetPosition)
        {
            // Vector3 direction = (targetPosition - transform.position).normalized * -10f;
            // direction = transform.position + direction;
            // transform.LookAt(direction);

            Vector3 invertedPoint = transform.position * 2f - targetPosition;
            transform.LookAt(invertedPoint);
        }

        /// <summary>
        /// Rotates this transform so its forward vector points directly away from the specified target transform.
        /// </summary>
        /// <param name="transform">The transform to rotate.</param>
        /// <param name="target">The target transform.</param>
        public static void LookAtInvert(this Transform transform, Transform target)
        {
            transform.LookAtInvert(target.position);
        }

        // public static void TraverseChildren(Transform parent, ref List<MapSpawnController> spawnControllers, ref List<MapSpawnItemBase> spawnItems)
        // {
        //     foreach (Transform child in parent)
        //     {
        //         MapSpawnController controller = child.GetComponent<MapSpawnController>();
        //         if (controller != null)
        //         {
        //             spawnControllers.Add(controller);
        //             Debug.Log($"Found controller: {child.name}");
        //             continue;
        //         }

        //         MapSpawnItemBase spawnItem = child.GetComponent<MapSpawnItemBase>();
        //         if (spawnItem != null)
        //         {
        //             spawnItems.Add(spawnItem);
        //             Debug.Log($"Found spawn item: {child.name}");
        //         }

        //         TraverseChildren(child, ref spawnControllers, ref spawnItems);
        //     }
        // }

        /// <summary>
        /// Buffer overload to avoid allocating a new list. Does NOT clear the buffer.
        /// Call buffer.Clear() yourself if you want to reuse it.
        /// VERY SLOW FOR DEEP HIERARCHIES, DO NOT USE IN PERFORMANCE-CRITICAL CODE
        /// </summary>
        public static void TraverseChildren<T>(this Transform parent, List<T> buffer, bool includeInactive = false, bool includeSelf = false)
            where T : class
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            // Non-recursive DFS to avoid stack overflows on deep hierarchies.
            var stack = ListPool<Transform>.Get();
            try
            {
                if (includeSelf) stack.Add(parent);
                else
                {
                    for (int i = parent.childCount - 1; i >= 0; --i)
                        stack.Add(parent.GetChild(i));
                }

                // Temp component list to avoid per-node array allocations
                var comps = ListPool<Component>.Get();
                try
                {
                    while (stack.Count > 0)
                    {
                        int last = stack.Count - 1;
                        Transform t = stack[last];
                        stack.RemoveAt(last);

                        if (!includeInactive && !t.gameObject.activeInHierarchy)
                            continue;

                        // Gather matches: works for Components and interfaces implemented by Components
                        comps.Clear();
                        t.GetComponents(comps); // fills comps with all Component instances on this GameObject
                        for (int i = 0; i < comps.Count; ++i)
                        {
                            var c = comps[i];
                            if (c == null) continue; // missing script
                            if (c is T match)
                                buffer.Add(match);
                        }

                        // Push children
                        for (int i = t.childCount - 1; i >= 0; --i)
                            stack.Add(t.GetChild(i));
                    }
                }
                finally
                {
                    ListPool<Component>.Return(comps);
                }
            }
            finally
            {
                ListPool<Transform>.Return(stack);
            }
        }
    }
}

