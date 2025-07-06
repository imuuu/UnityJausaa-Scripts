using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Utilities
{
    public static class SimpleUtilities
    {
        public static IEnumerable<Type> GetFilteredTypeList(object obj)
        {
            return obj.GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => obj.GetType().IsAssignableFrom(t));
        }
    }
}
