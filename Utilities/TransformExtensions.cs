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
        public static void TraverseChildren<T>(this Transform parent, List<T> buffer, bool includeInactive = false, bool includeSelf = false, bool breakOnFirstMatch = false)
            where T : class
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            // Non-recursive DFS to avoid stack overflows on deep hierarchies.
            List<Transform> stack = ListPool<Transform>.Get();
            try
            {
                if (includeSelf) stack.Add(parent);
                else
                {
                    for (int i = parent.childCount - 1; i >= 0; --i)
                        stack.Add(parent.GetChild(i));
                }

                // Temp component list to avoid per-node array allocations
                List<Component> comps = ListPool<Component>.Get();
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
                            Component c = comps[i];
                            if (c == null) continue;

                            if (c is T match)
                            {
                                buffer.Add(match);

                                if (breakOnFirstMatch) return;
                            }

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

        /// <summary>
        /// Walks up the transform’s parent chain and collects components of type <typeparamref name="T"/> into <paramref name="buffer"/>.
        /// Does NOT clear the buffer (clear it yourself if you reuse it). Non-recursive.
        /// Supports both Component types and interfaces implemented by Components.
        /// </summary>
        /// <param name="start">The transform to start from.</param>
        /// <param name="buffer">A list to append found components to (not cleared).</param>
        /// <param name="maxDepth">
        /// Maximum number of steps to traverse upward (including the starting node if <paramref name="includeSelf"/> is true).
        /// </param>
        /// <param name="includeSelf">If true, also checks the starting transform before going to parents.</param>
        /// <param name="includeInactive">If false, skips inactive GameObjects.</param>
        /// <param name="breakOnFirstMatch">If true, returns immediately after the first match is added to the buffer.</param>
        /// <typeparam name="T">Component or interface type to search for.</typeparam>
        public static void TraverseParents<T>(
            this Transform start,
            List<T> buffer,
            int maxDepth = int.MaxValue,
            bool includeSelf = true,
            bool includeInactive = true,
            bool breakOnFirstMatch = false)
            where T : class
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (maxDepth <= 0) return;

            bool tIsComponent = typeof(Component).IsAssignableFrom(typeof(T));
            Type tType = typeof(T);

            List<Component> comps = null;
            if (!tIsComponent)
                comps = ListPool<Component>.Get();

            try
            {
                Transform current = includeSelf ? start : start.parent;
                int steps = 0;

                while (current != null && steps < maxDepth)
                {
                    if (includeInactive || current.gameObject.activeInHierarchy)
                    {
                        if (tIsComponent)
                        {
                            if (current.TryGetComponent(tType, out Component c) && c != null)
                            {
                                buffer.Add(c as T);
                                if (breakOnFirstMatch) return;
                            }
                        }
                        else
                        {
                            comps!.Clear();
                            current.GetComponents(comps);
                            for (int i = 0; i < comps.Count; i++)
                            {
                                Component c = comps[i];
                                if (c != null && c is T match)
                                {
                                    buffer.Add(match);
                                    if (breakOnFirstMatch) return;
                                }
                            }
                        }
                    }

                    current = current.parent;
                    steps++;
                }
            }
            finally
            {
                if (comps != null)
                    ListPool<Component>.Return(comps);
            }
        }


        /// <summary>
        /// Returns the first component of type <typeparamref name="T"/> found in the transform’s parent chain.
        /// Fast for runtime lookups. Supports interfaces as well as Components.
        /// </summary>
        /// <param name="start">The transform to start from.</param>
        /// <param name="maxDepth">Maximum number of parent steps to check (including self if <paramref name="includeSelf"/> is true).</param>
        /// <param name="includeSelf">If true, checks the starting transform before its parents.</param>
        /// <param name="includeInactive">If false, skips inactive GameObjects.</param>
        /// <typeparam name="T">Component or interface type to find.</typeparam>
        /// <returns>The first matching component (or null if none found within <paramref name="maxDepth"/>).</returns>

        public static T FindInParents<T>(
            this Transform start,
            int maxDepth = int.MaxValue,
            bool includeSelf = true,
            bool includeInactive = true)
            where T : class
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (maxDepth <= 0) return null;

            bool tIsComponent = typeof(Component).IsAssignableFrom(typeof(T));
            Type tType = typeof(T);

            if (tIsComponent)
            {
                Transform current = includeSelf ? start : start.parent;
                int steps = 0;

                while (current != null && steps < maxDepth)
                {
                    if (includeInactive || current.gameObject.activeInHierarchy)
                    {
                        if (current.TryGetComponent(tType, out Component c) && c != null)
                            return c as T;
                    }
                    current = current.parent;
                    steps++;
                }

                return null;
            }
            else
            {
                List<Component> comps = ListPool<Component>.Get();
                try
                {
                    Transform current = includeSelf ? start : start.parent;
                    int steps = 0;

                    while (current != null && steps < maxDepth)
                    {
                        if (includeInactive || current.gameObject.activeInHierarchy)
                        {
                            comps.Clear();
                            current.GetComponents(comps);
                            for (int i = 0; i < comps.Count; i++)
                            {
                                Component c = comps[i];
                                if (c != null && c is T match)
                                    return match;
                            }
                        }
                        current = current.parent;
                        steps++;
                    }

                    return null;
                }
                finally
                {
                    ListPool<Component>.Return(comps);
                }
            }
        }
    }
}

