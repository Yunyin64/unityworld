using System.Collections.Generic;
using System.Text;
namespace UnityWorld.Core
{
    public static class MathExtensions
    {
        public static int ToInt(this float num)
        {
            return (int)(num + 0.5f);
        }

        
    }
}