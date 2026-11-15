Shader "Zebeyova/URP/sandBase"
{
    Properties
    {
        [MainTexture]
        _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _NormalTex ("Normal Tex", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0,0.5)) = 0

        _OcclusionMap ("Occlusion Map(R)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1

        _HeightMap ("Height map", 2D) = "white" {}

        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        #pragma multi_compile_ _ _MAIN_LIGHT_SHADOWS
        #pragma multi_compile_ _ _MAIN_LIGHT_SHADOWS_CASCADE
        #pragma multi_compile_ _ _MAIN_LIGHT_SHADOWS_SCREEN

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalTex);
        SAMPLER(sampler_NormalTex);
        TEXTURE2D(_OcclusionMap);
        SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_HeightMap);
        SAMPLER(sampler_HeightMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;

            float4 _NormalTex_ST;
            float _NormalScale;
            float _OcclusionStrength;
            float _Smoothness;
        CBUFFER_END

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p.yx + 45.32);
            return frac(p.x * p.y);
        }
        ENDHLSL

        Pass
        {
            Name "sandBase"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex SandVert
            #pragma fragment SandFrag
            #pragma target 3.0


            struct Attributes
            {
                float2 uv : TEXCOORD0;
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float3 bitangentOS : TEXCOORD1;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float2 norUV : TEXCOORD5;

                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings SandVert(Attributes input)
            {
                Varyings o;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;

                VertexNormalInputs norInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.normalWS = norInputs.normalWS;
                o.tangentWS = norInputs.tangentWS;
                o.bitangentWS = norInputs.bitangentWS;

                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.norUV = TRANSFORM_TEX(input.uv, _NormalTex);
                return o;
            }

            half4 SandFrag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 nTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.norUV), _NormalScale);
                half height = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv);
                half3 normalWS = normalize(normalize(input.tangentWS) * nTS.x
                    + normalize(input.bitangentWS) * nTS.y
                    + normalize(input.normalWS) * nTS.z);
                half3 ambientLight = SampleSH(normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half halfLambert = saturate(NdotL) * 0.5 + 0.5;

                half shadow = mainLight.shadowAttenuation;
                // half lightCoord = shadow * NdotL;
                half3 directLight = mainLight.color * NdotL * shadow;
                half3 lighting = ambientLight + directLight;

                half3 finalColor = (color * _BaseColor * lighting).rgb;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
    }
}