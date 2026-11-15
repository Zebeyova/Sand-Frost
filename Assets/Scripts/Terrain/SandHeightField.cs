using System;
using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// 沙漠高度场参数。u/v 是覆盖整块地形的归一化坐标，u 对应 X，v 对应 Z。
    /// frequency 和 macroFrequency 表示整块地形中大致的起伏数量。
    /// </summary>
    [Serializable]
    public struct HeightFieldSettings
    {
        [HideInInspector] public int version;
        [Tooltip("同种子 + 同参数 = 同一张地形。")] public int seed;

        [Header("沙丘主体")]
        [Range(1, 6)] public int octaves;
        [Tooltip("整块地形中大约出现多少组中尺度沙丘。2000 米地图建议 3~5。")]
        [Range(0.5f, 10f)] public float frequency;
        [Tooltip("高度场总落差上限（米）。")]
        [Range(1f, 80f)] public float amplitude;
        [Range(0.2f, 0.7f)] public float persistence;
        [Range(1.2f, 2.5f)] public float lacunarity;
        [Tooltip("沙脊对比度。建议保持较低，过高会制造尖锐暗边。")]
        [Range(0f, 1f)] public float ridgeStrength;

        [Header("沙丘塑形")]
        [Tooltip("1 为自然圆缓；大于 1 会增加坡面反差，但仍保持圆滑。")]
        [Range(0.5f, 2.5f)] public float duneSharpness;

        [Header("域扭曲")]
        [Tooltip("控制沙脊弯曲程度。建议 0.05~0.25。")]
        [Range(0f, 0.6f)] public float warpStrength;
        [Tooltip("域扭曲的尺度。越小越平缓，越大越容易出现碎裂纹理。")]
        [Range(0.5f, 4f)] public float warpScale;

        [Header("远景层次")]
        [Tooltip("宏观起伏混入比例。建议 0.2~0.4。")]
        [Range(0f, 1f)] public float macroAmplitude;
        [Tooltip("整块地形中宏观沙丘的数量。越低，沙丘越大。")]
        [Range(0.2f, 3f)] public float macroFrequency;
        [Range(1, 4)] public int macroOctaves;

        [Header("风向")]
        [Tooltip("沿风向拉伸沙丘。5~8 适合形成长缓沙脊。")]
        [Range(1f, 12f)] public float windStretch;
        public Vector2 windDirection;
    }

    /// <summary>
    /// 程序化沙漠高度场。视觉网格、碰撞体和玩法查询共用此纯函数。
    /// 高度范围为 0~amplitude，地形物体的 Y 位置决定整体海拔。
    /// </summary>
    public static class SandHeightField
    {
        // 兼容旧调用。新生成器会显式传入地块边长。
        public const float NoiseSpanMeters = 1000f;

        public static float Sample(in HeightFieldSettings settings, float u, float v, float spanMeters)
        {
            return SampleNormalized(settings, u, v);
        }

        /// <summary>
        /// 用世界坐标采样。spanMeters 应为地块边长，使沙丘尺度随地形尺寸变化。
        /// </summary>
        public static float SampleWorld(in HeightFieldSettings settings, float worldX, float worldZ,
            float spanMeters)
        {
            float span = Mathf.Max(1e-3f, spanMeters);
            return SampleNormalized(settings, worldX / span + 0.5f, worldZ / span + 0.5f);
        }

        // 兼容可能仍在使用旧签名的脚本。
        public static float SampleWorld(in HeightFieldSettings settings, float worldX, float worldZ)
        {
            return SampleWorld(settings, worldX, worldZ, NoiseSpanMeters);
        }

        static float SampleNormalized(in HeightFieldSettings settings, float u, float v)
        {
            Vector2 wind = settings.windDirection.sqrMagnitude > 1e-6f
                ? settings.windDirection.normalized
                : Vector2.right;
            Vector2 side = new Vector2(-wind.y, wind.x);
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f);

            // Keep domain warp small so it bends long dune lines without breaking their silhouettes.
            if (settings.warpStrength > 1e-4f)
            {
                float frequency = Mathf.Max(0.25f, settings.warpScale);
                float ox = Hash01(settings.seed, 7f) * 64f;
                float oy = Hash01(settings.seed, 13f) * 64f;
                float bx = Hash01(settings.seed, 29f) * 64f;
                float by = Hash01(settings.seed, 31f) * 64f;
                float dx = Mathf.PerlinNoise(p.x * frequency + ox, p.y * frequency + oy) * 2f - 1f;
                float dy = Mathf.PerlinNoise(p.x * frequency + bx, p.y * frequency + by) * 2f - 1f;
                p += new Vector2(dx, dy) * Mathf.Clamp01(settings.warpStrength) * 0.08f;
            }

            float macro = DirectionalFbm(p, wind, side,
                Mathf.Max(0.2f, settings.macroFrequency), Mathf.Max(1, settings.macroOctaves),
                settings.persistence, settings.lacunarity, settings.windStretch * 1.35f,
                settings.ridgeStrength * 0.35f, settings.seed, 101f);

            float dunes = DirectionalFbm(p, wind, side,
                Mathf.Max(0.5f, settings.frequency), Mathf.Max(1, settings.octaves),
                settings.persistence, settings.lacunarity, Mathf.Max(1f, settings.windStretch),
                settings.ridgeStrength * 0.45f, settings.seed, 211f);

            // A low-amplitude smooth detail layer keeps the foreground from looking featureless.
            float detail = Mathf.PerlinNoise(
                p.x * Mathf.Max(1f, settings.frequency) * 2.2f + Hash01(settings.seed, 401f) * 64f,
                p.y * Mathf.Max(1f, settings.frequency) * 2.2f + Hash01(settings.seed, 503f) * 64f);

            float macroWeight = Mathf.Clamp01(settings.macroAmplitude) * 0.45f;
            float h = Mathf.Lerp(dunes, macro, macroWeight);
            h = Mathf.Lerp(h, detail, 0.035f);
            h = ShapeDune(h, settings.duneSharpness);
            return Mathf.Clamp01(h) * Mathf.Max(0f, settings.amplitude);
        }

        static float DirectionalFbm(Vector2 p, Vector2 wind, Vector2 side, float frequency,
            int octaves, float persistence, float lacunarity, float stretch,
            float ridgeStrength, int seed, float salt)
        {
            float along = Vector2.Dot(p, wind) / Mathf.Max(1f, stretch);
            float across = Vector2.Dot(p, side);
            Vector2 q = wind * along + side * across;

            float sum = 0f;
            float weight = 1f;
            float normalization = 0f;
            float f = Mathf.Max(0.1f, frequency);
            float ox = Hash01(seed, salt + 1f) * 64f;
            float oy = Hash01(seed, salt + 2f) * 64f;

            for (int i = 0; i < octaves; i++)
            {
                float n = Mathf.PerlinNoise(q.x * f + ox, q.y * f + oy);
                float soft = Smooth01(n);
                float ridge = Smooth01(1f - Mathf.Abs(n * 2f - 1f));
                float layer = Mathf.Lerp(soft, ridge, Mathf.Clamp01(ridgeStrength));
                sum += layer * weight;
                normalization += weight;
                weight *= Mathf.Clamp(persistence, 0.2f, 0.7f);
                f *= Mathf.Clamp(lacunarity, 1.2f, 2.5f);
            }

            return normalization > 0f ? sum / normalization : 0.5f;
        }

        static float ShapeDune(float value, float sharpness)
        {
            float x = Mathf.Clamp01(value);
            float control = Mathf.Clamp01((sharpness - 0.5f) / 2f);
            float exponent = Mathf.Lerp(1.05f, 0.88f, control);
            float contrasted = Mathf.Lerp(x, Mathf.Pow(x, exponent), 0.35f);
            return Smooth01(contrasted);
        }

        static float Smooth01(float value)
        {
            float x = Mathf.Clamp01(value);
            return x * x * (3f - 2f * x);
        }

        static float Hash01(int seed, float salt)
        {
            float x = Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
