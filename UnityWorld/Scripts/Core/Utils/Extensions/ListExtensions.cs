using System.Collections.Generic;
using System.Text;
namespace UnityWorld.Core
{
    public static class ListExtensions
    {
        public static string ToInfoString<T>(this List<T> list)
        {
            return $"[{string.Join(", ", list.Select(t => t.ToString() ?? "null"))}]";
        }

        
    }
}