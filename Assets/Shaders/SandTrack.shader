Shader "Zebeyova/URP/sandTrack"
{
    Properties
    {
        [MainTexture]
        _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "sandTrack"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float2 uv: TEXCOORD0;
                float3 positionOS: POSITION;
                float3 normalOS: NORMAL;
                float4 tangentOS: TANGENT;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS: SV_POSITION;
                float3 postionWS: TEXCOORD1;
                float3 normalWS: TEXCOORD2;
                float3 tangentWS: TEXCOORD3;
                float3 bitangentWS: TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;

                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS);
                o.postionWS = posInput.positionWS;

                VertexNormalInputs norInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.normalWS = norInput.normalWS;
                o.tangentWS = norInput.tangentWS;
                o.bitangentWS = norInput.bitangentWS;

                input.positionOS.xyz -= input.normalOS * SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, input.uv, 0);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return color * _BaseColor;
            }
            ENDHLSL
        }
    }
}