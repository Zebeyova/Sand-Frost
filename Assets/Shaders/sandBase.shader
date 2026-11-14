Shader "Zebeyova/URP/sandBase"
{
    Properties
    {
        [MainTexture]
        _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _NormalTex ("Normal Tex", 2D) = "bump" {}
        _NormalScale ("Normal Scale", float) = 1

        _OcclusionMap ("Occlusion Map(R)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1

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

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalTex);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_OcclusionMap);
        SAMPLER(sampler_OcclusionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;

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

                VertexNormalInputs norInputs = GetVertexNormalInputs(input.normalOS);
                o.normalWS = norInputs.normalWS;
                o.tangentWS = norInputs.tangentWS;
                o.bitangentWS = norInputs.bitangentWS;

                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 SandFrag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 normalWS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalMap, input.uv), _NormalScale);

                return color * _BaseColor;
            }
            ENDHLSL
        }
    }
}