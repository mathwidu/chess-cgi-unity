Shader "ChessCgi/CaptureAura"
{
    Properties
    {
        _AuraColor ("Aura Color", Color) = (0.2, 0.9, 0.35, 1)
        _CoreAlpha ("Core Alpha", Range(0, 1)) = 0.12
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.55
        _RimPower ("Rim Power", Range(0.5, 6)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Aura"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _AuraColor;
                half _CoreAlpha;
                half _RimAlpha;
                half _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - facing, _RimPower);
                half alpha = saturate(_CoreAlpha + rim * _RimAlpha);
                return half4(_AuraColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
