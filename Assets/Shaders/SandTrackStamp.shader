Shader "Zebeyova/SandTrackStamp" {
    Properties {
        _MainTex ("Foot Main Tex", 2D) = "white" {}
        _StampStrength ("Stamp Strength", Range(0,1)) = 0.8
        _ImprintColor ("Imprint Color", Color) =  (0.72, 0.68, 0.6, 1)
    }

    SubShader {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass {
            Name "SandTrackStamp"
            Tags { "LightMode" = "UniversalForward" }

            ZTest LEqual
            ZWrite Off
            Cull Off
            Blend DstColor Zero
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _ImprintColor;
            half _StampStrength;
            CBUFFER_END

            struct Attributes {
                float2 uv : TEXCOORD0;
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input) {
                Varyings o;

                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;

                return o;
            }

            half4 Frag(Varyings input) : SV_Target {
                float2 centerUV = input.uv * 2.0 - 1.0;
                float disToCenter = length(centerUV);
                half footprint = 1.0h - smoothstep(0.72h, 1.0h, disToCenter);

                half mask = saturate(footprint * _StampStrength);
                half NdotL = saturate(dot(normalize(input.normalWS), normalize(GetMainLight().direction)));
                half edgeLight = lerp(0.72h, 1.08h, NdotL);
                half3 darkened = lerp(half3(1.0h, 1.0h, 1.0h), _ImprintColor.rgb * edgeLight, mask);

                return half4(darkened, 1.0h);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}