using Unity.Mathematics;
using UnityEngine;

namespace ClothXPBD
{
    public class CustomMath
    {
        /// <summary>
        /// 计算三角形面积
        /// </summary>
        public static float GetArea(float3 p0, float3 p1, float3 p2)
        {
            var v0 = p1 - p0;
            var v1 = p2 - p0;
            return math.length(math.cross(v0, v1)) * 0.5f;
        }
    }
}
