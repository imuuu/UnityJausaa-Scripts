using System.Collections.Generic;
using UnityEngine;

namespace Game.PoolSystem
{
    /// <summary>
    /// Pools Vector3 arrays keyed by their length. (Arrays are fixed in size.)
    /// </summary>
    public static class Vector3ArrayPool
    {
        private static readonly Dictionary<int, Stack<Vector3[]>> pool = new ();

        public static Vector3[] GetArray(int length)
        {
            if (pool.TryGetValue(length, out Stack<Vector3[]> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
            return new Vector3[length];
        }

        public static void ReturnArray(Vector3[] array)
        {
            int length = array.Length;
            if (!pool.TryGetValue(length, out Stack<Vector3[]> stack))
            {
                stack = new Stack<Vector3[]>();
                pool[length] = stack;
            }
            stack.Push(array);
        }
    }

}