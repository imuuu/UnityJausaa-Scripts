using System.Collections.Generic;

namespace Game.PoolSystem
{
    public static class GenericPool<T> where T : class, new()
    {
        private static readonly Stack<T> pool = new Stack<T>();

        public static T Get()
        {
            return pool.Count > 0 ? pool.Pop() : new T();
        }

        public static void Return(T obj)
        {
            pool.Push(obj);
        }
    }

}
