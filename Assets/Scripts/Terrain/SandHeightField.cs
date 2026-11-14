using System;
using UnityEngine;

namespace Terrain
{
    /// <summary>
    /// 地形高度场参数。纯数据，不依赖任何 Unity 组件，
    /// 网格生成、碰撞体、玩法查询都用同一份参数，保证三者永远一致。
    /// 坐标约定：u/v 为地形归一化坐标（0~1），u 对应 +X，v 对应 +Z。
    /// </summary>
    [Serializable]
    public struct HeightFieldSettings
    {
        [HideInInspector] public int version;

        [Tooltip("同种子 + 同参数 = 同一张地形。")]
        public int seed;

        [Header("沙丘主体 fBm")]
        [Range(1, 8)] public int octaves;

        [Tooltip("沙丘间距：整个地形上大约出现多少个沙丘周期。")]
        public float frequency;

        [Tooltip("最高起伏（米）。")]
        public float amplitude;

        [Range(0.05f, 0.95f)] public float persistence;

        public float lacunarity;

        [Tooltip("0 = 平滑 fBm，1 = 尖锐沙脊。")]
        [Range(0f, 1f)] public float ridgeStrength;

        [Header("沙丘塑形")]
        [Tooltip("1 = 圆缓；>1 让谷底更平、沙脊更高更陡，沙丘感更强。")]
        [Range(0.1f, 6f)] public float duneSharpness;

        [Header("域扭曲 (Domain Warp)")]
        [Tooltip("让沙脊蜿蜒、错落成层层叠叠的远景；0 = 直条纹。")]
        [Range(0f, 1f)] public float warpStrength;

        [Tooltip("扭曲的疏密，越大沙脊越蜿蜒。")]
        [Range(0.5f, 8f)] public float warpScale;

        [Header("远景层次 (Macro)")]
        [Tooltip("大尺度起伏占的比重，越大远景沙丘越宏大、越有层层叠叠的天际线。")]
        [Range(0f, 1f)] public float macroAmplitude;

        [Tooltip("大沙丘的疏密，越低越大（约 1 = 全图一两个大沙丘）。")]
        [Range(0.05f, 4f)] public float macroFrequency;

        [Range(1, 6)] public int macroOctaves;

        [Header("风向")]
        [Tooltip("沿风向拉伸的倍率，>1 得到长条沙脊（Journey 那种单向沙丘）。")]
        [Range(1f, 12f)] public float windStretch;

        public Vector2 windDirection;
    }

    /// <summary>
    /// 程序化高度场采样。全部是纯函数（同输入永远同输出），
    /// 因此可以在主线程随便调，也可以以后原样搬进 Burst Job 或 Compute Shader。
    ///
    /// 结构：风向拉伸 → 域扭曲（让沙脊蜿蜒）→ 远景大沙丘层 + 沙丘主体 ridged fBm
    ///      → 沙丘塑形对比度曲线（压平谷底、立起沙脊）→ 乘以幅度。
    /// </summary>
    public static class SandHeightField
    {
        /// <summary>
        /// 采样平面上一单位的噪声被铺到多少米。
        ///
        /// 关键约定：frequency 数的是「这一整片空间里有多少个沙丘周期」。
        /// 归一化模式把一整块地形铺成 1 个单位，所以单位就是 size 米；
        /// 世界坐标模式铺的是 NOISE_SPAN 米，单位就固定为 NOISE_SPAN。
        /// 显式传入这个值（而不是写死常量），Frequency 的语义在两种模式下就完全一致，
        /// 打开拼合不会让沙丘突然变密。
        /// </summary>
        public const float NoiseSpanMeters = 1000f;

        /// <summary>
        /// 采样归一化坐标 (u, v) 处的高度（米），返回值范围 0 ~ amplitude。
        /// u/v 是 0~1 覆盖整块地形的坐标；<paramref name="spanMeters"/> 是这块地形对应的实际米数。
        /// </summary>
        public static float Sample(in HeightFieldSettings s, float u, float v, float spanMeters)
        {
            return SampleNormalized(s, u, v, Mathf.Max(1e-3f, spanMeters));
        }

        /// <summary>
        /// 按世界坐标采样高度（米）。多块地形无缝拼合的正确入口：
        /// 相邻地块只要世界坐标连续，采样到的就是同一张无限噪声场，接缝天然对齐。
        /// </summary>
        public static float SampleWorld(in HeightFieldSettings s, float worldX, float worldZ)
        {
            return SampleNormalized(s, worldX / NoiseSpanMeters, worldZ / NoiseSpanMeters, NoiseSpanMeters);
        }

        /// <summary>
        /// 内部实现：u/v 是“噪声空间”坐标，<paramref name="spanMeters"/> 是 1 个噪声单位对应的米数。
        /// </summary>
        static float SampleNormalized(in HeightFieldSettings s, float u, float v, float spanMeters)
        {
            // 风向归一化；零向量时退回 +X，避免除零。
            Vector2 wind = s.windDirection.sqrMagnitude > 1e-6f
                ? s.windDirection.normalized
                : Vector2.right;
            Vector2 side = new Vector2(-wind.y, wind.x);

            // 沿风向把采样坐标压扁：同一段距离上高度变化更慢 -> 沙脊被拉长。
            float along = Vector2.Dot(new Vector2(u, v), wind) / Mathf.Max(1f, s.windStretch);
            float across = Vector2.Dot(new Vector2(u, v), side);
            Vector2 p = wind * along + side * across;

            // 用种子做固定的坐标偏移，避免所有种子看起来是同一张图平移。
            float ox = Hash01(s.seed, 17.0f) * 128f;
            float oy = Hash01(s.seed, 43.0f) * 128f;

            // 域扭曲：把直的沙脊掰弯，错落成层层叠叠的远景。
            if (s.warpStrength > 1e-4f)
                p = DomainWarp(p, s);

            // 远景层次：低频大山丘，决定“远处总有一层更高的沙丘”的天际线。
            // macroFrequency 也按“整片空间里几个大沙丘”来数，所以要换算到同样的尺度单位。
            float macroAmp = Mathf.Clamp01(s.macroAmplitude);
            float macro = 0f;
            if (macroAmp > 1e-4f)
            {
                // macroFrequency 是相对 dune 尺度的倍数，本身已无量纲，直接沿用即可。
                float mf = Mathf.Max(0.05f, s.macroFrequency);
                int mo = Mathf.Max(1, s.macroOctaves);
                // 远景层用纯 ridged，得到清晰的大沙脊。
                macro = RidgedFbm(p, mo, mf, s.persistence, s.lacunarity, 1f, ox, oy, 513.0f);
            }

            // 沙丘主体：中频 ridged fBm。
            float dunes = RidgedFbm(p, Mathf.Max(1, s.octaves), s.frequency,
                s.persistence, s.lacunarity, s.ridgeStrength, ox, oy, 0f);

            float h = Mathf.Lerp(dunes, macro, macroAmp);

            // 沙丘塑形：把坡压平、把脊立起来，形成陡峭的沙脊面。
            h = SharpenDune(h, s.duneSharpness);

            return h * s.amplitude;
        }

        /// <summary>
        /// 域扭曲：用一张向量噪声把坐标推挤一下，让噪声等值线（沙脊）蜿蜒。
        /// 位移量以“沙丘宽度”（1/frequency）为单位，所以 frequency 变化时蜿蜒感保持一致。
        /// </summary>
        static Vector2 DomainWarp(Vector2 p, in HeightFieldSettings s)
        {
            float scale = Mathf.Max(0.5f, s.warpScale);
            float ax = Hash01(s.seed, 7.0f) * 128f;
            float ay = Hash01(s.seed, 13.0f) * 128f;
            float bx = Hash01(s.seed, 29.0f) * 128f;
            float by = Hash01(s.seed, 31.0f) * 128f;

            float dx = Mathf.PerlinNoise(p.x * scale + ax, p.y * scale + ay) * 2f - 1f;
            float dy = Mathf.PerlinNoise(p.x * scale + bx, p.y * scale + by) * 2f - 1f;

            float strength = Mathf.Clamp01(s.warpStrength) / Mathf.Max(1f, s.frequency);
            return p + new Vector2(dx, dy) * strength;
        }

        /// <summary>
        /// ridged fBm：噪声在 0.5 等值线上折出脊线。octaves 归一化到 [0,1]。
        /// salt 用于给不同层加不同偏移，避免远景层和沙丘层共用同一张噪声造成相关性。
        ///
        /// 关键：ridge 项用 ridged^sharp 而不是裸 ridged。裸的 1-|2n-1| 在脊线处是 V 形尖点，
        /// 就是画面里那些“扎人”的尖。这里对 ridged 值再做一次 smoothstep 圆角，
        /// 沙脊顶变成圆顶（真实沙丘的圆缓轮廓），而不是折纸一样的尖锥。
        /// </summary>
        static float RidgedFbm(Vector2 p, int octaves, float freq, float persistence,
            float lacunarity, float ridge, float ox, float oy, float salt)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            float f = Mathf.Max(1e-4f, freq);

            for (int i = 0; i < octaves; i++)
            {
                float n = Mathf.PerlinNoise(p.x * f + ox + salt, p.y * f + oy + salt);

                // 圆角 ridged：裸 ridged 在脊处有 V 形折角，这里把折角揉成圆顶。
                float ridged = 1f - Mathf.Abs(n * 2f - 1f);
                ridged = ridged * ridged * (3f - 2f * ridged);   // smoothstep 圆角

                float v = Mathf.Lerp(n, ridged, ridge);

                sum += v * amp;
                norm += amp;
                amp *= persistence;
                f *= lacunarity;
            }

            return norm > 0f ? sum / norm : 0f;
        }

        /// <summary>
        /// 沙丘塑形曲线：k=1 恒等；k>1 时谷底更平、脊顶更立（对比度拉伸），
        /// 但曲线处处平滑、两端斜率为 0，所以脊顶永远是圆弧而不是尖锥。
        ///
        /// 做法：先把 x 映射到 [-1,1]，用 |d|^(1/k) 拉开中段（缓谷陡脊），
        /// 再乘一个很小的补偿系数抵消 smoothstep 的削平，最后 smoothstep 圆角。
        /// 补偿系数必须很小（1.15 会把高 k 压成平顶台地），0.06 足够抵消削弱而不会饱和。
        /// </summary>
        static float SharpenDune(float x, float sharpness)
        {
            float k = Mathf.Clamp(sharpness, 0.05f, 6f);
            x = Mathf.Clamp01(x);

            float d = x * 2f - 1f;
            float ad = Mathf.Abs(d);
            float sign = d < 0f ? -1f : 1f;

            // 幂拉伸：|d|^(1/k)。k>1 -> 指数 <1 -> 曲线向两侧外鼓（缓谷陡脊）。
            float stretched = Mathf.Pow(ad, 1f / k);

            // 小幅补偿 smoothstep 的削平，避免高 k 时曲线提前饱和成平顶。
            stretched = Mathf.Clamp01(stretched * 1.06f);

            float t = 0.5f + sign * stretched * 0.5f;

            // 最后过一遍 smoothstep，把 t 在 0.5 处的折角揉圆（脊顶圆顶）。
            return t * t * (3f - 2f * t);
        }

        static float Hash01(int seed, float salt)
        {
            float x = Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
