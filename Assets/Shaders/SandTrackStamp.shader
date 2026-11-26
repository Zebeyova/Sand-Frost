Shader "Zebeyova/SandTrackStamp" {
    Properties {
        _MainTex ("Foot Main Tex", 2D) = "white" {}
        _StampUV ("Stamp UV", Vector) =  (0.5, 0.5, 0.01, 0.02)
        _StampAngle ("Stamp Angle", float) = 0
        _StampStrength ("Stamp Strength", Range(0,1)) = 0.8
        _RidgeStrength ("Ridge Strength", Range(0,1)) = 0.5
        _Fade ("Fade", Range(0,1)) = 0.5
    }

    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass {
            Name "SandTrackStamp"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _StampUV;
            float _StampAngle;
            float _StampStrength;
            float _RidgeStrength;
            float _Fade;
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
                float2 offset = input.uv - _StampUV.xy;

                float s, c;
                sincos(_StampAngle, s, c);
                half2 rotated = float2(offset.x * c - offset.y * s, offset.x * s + offset.y * c);
                half2 halfSize = max(_StampUV.zw, 0.00001);
                half2 ellipse = abs(rotated) / halfSize;
                half disToCenter = length(ellipse);

                //脚印中心下陷
                half depression = 1.0h - smoothstep(0.55h, 1.0h, disToCenter);
                //脚印外围堆积
                half outer = 1.0 - smoothstep(0.95h, 1.25h, disToCenter);
                half ridge = saturate(outer - depression);

                half4 oldData = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half depressionData = max(oldData.r * _Fade, depression * _StampStrength);
                half ridgeData = max(oldData.g * _Fade, ridge * _StampStrength * _RidgeStrength);

                return half4(depressionData, ridgeData, 0, 1);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}