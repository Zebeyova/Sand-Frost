using System;
using UnityEngine;

namespace SandAndFrost.World
{
    /// <summary>
    /// 地形高度场参数。纯数据，不依赖任何 Unity 组件，
    /// 网格生成、碰撞体、玩法查询都用同一份参数，保证三者永远一致。
    /// 坐标约定：u/v 为地形归一化坐标（0~1），u 对应 +X，v 对应 +Z。
    /// </summary>
    [Serializable]
    public struct HeightFieldSettings
    {
        [Tooltip("同种子 + 同参数 = 同一张地形。")]
        public int seed;

        [Header("fBm")]
        [Range(1, 8)] public int octaves;

        [Tooltip("基础噪声频率：整个地形上大约出现多少个起伏周期。")]
        public float frequency;

        [Tooltip("最高起伏（米）。")]
        public float amplitude;

        [Range(0.05f, 0.95f)] public float persistence;

        public float lacunarity;

        [Tooltip("0 = 平滑 fBm，1 = 尖锐沙脊。")]
        [Range(0f, 1f)] public float ridgeStrength;

        [Tooltip(">1 让低处更平、脊更突出，沙丘感更强。")]
        [Range(0.1f, 4f)] public float duneSharpness;

        [Header("风向")]
        [Tooltip("沿风向拉伸的倍率，>1 得到长条沙脊（Journey 那种单向沙丘）。")]
        [Range(1f, 12f)] public float windStretch;

        public Vector2 windDirection;
    }

    /// <summary>
    /// 程序化高度场采样。全部是纯函数（同输入永远同输出），
    /// 因此可以在主线程随便调，也可以以后原样搬进 Burst Job 或 Compute Shader。
    /// </summary>
    public static class SandHeightField
    {
        /// <summary>
        /// 采样归一化坐标 (u, v) 处的高度（米），返回值范围 0 ~ amplitude。
        /// </summary>
        public static float Sample(in HeightFieldSettings s, float u, float v)
        {
            // 风向归一化；零向量时退回 +X，避免除零。
            Vector2 wind = s.windDirection.sqrMagnitude > 1e-6f
                ? s.windDirection.normalized
                : Vector2.right;
            Vector2 side = new Vector2(-wind.y, wind.x);

            // 沿风向把采样坐标压扁：同一段距离上高度变化更慢 -> 沙脊被拉长。
            float along = Vector2.Dot(new Vector2(u, v), wind) / Mathf.Max(1f, s.windStretch);
            float across = Vector2.Dot(new Vector2(u, v), side);
            Vector2 q = wind * along + side * across;

            // 用种子做一个固定的坐标偏移，避免所有种子看起来是同一张图平移。
            float offsetX = Hash01(s.seed, 17.0f) * 128f;
            float offsetY = Hash01(s.seed, 43.0f) * 128f;

            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            float freq = Mathf.Max(1e-4f, s.frequency);

            for (int i = 0; i < s.octaves; i++)
            {
                float smooth = Mathf.PerlinNoise(q.x * freq + offsetX, q.y * freq + offsetY);
                // ridged：在噪声 0.5 的等值线上折出脊线。
                float ridged = 1f - Mathf.Abs(smooth * 2f - 1f);
                float n = Mathf.Lerp(smooth, ridged, s.ridgeStrength);

                sum += n * amp;
                norm += amp;
                amp *= s.persistence;
                freq *= s.lacunarity;
            }

            float h = norm > 0f ? sum / norm : 0f;
            h = Mathf.Pow(Mathf.Clamp01(h), Mathf.Max(0.1f, s.duneSharpness));
            return h * s.amplitude;
        }

        static float Hash01(int seed, float salt)
        {
            float x = Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
