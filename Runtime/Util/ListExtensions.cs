
using System.Collections.Generic;

namespace Xesin.GameplayFramework
{ 
    public static class ListExtensions
    {
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list.Count > index && index >= 0;
        }
    }
}
