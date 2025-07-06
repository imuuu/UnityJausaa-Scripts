using System.Collections.Generic;

namespace Game.PoolSystem
{
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new ();

        public static List<T> Get()
        {
            if (pool.Count > 0)
            {
                List<T> list = pool.Pop();
                list.Clear();
                return list;
            }
            return new List<T>();
        }

        public static void Return(List<T> list)
        {
            list.Clear();
            pool.Push(list);
        }
    }
}

