Shader "Unlit/BlackSphere_Alpha"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Alpha ("Alpha", Range(0,1)) = 1
        _SoftEdge("Soft Edge", Range(0.01,0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite On          // 仍写深度，保证能遮住后面物体
            ZTest LEqual
            Cull Front         // 只渲染内表面
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float  _Alpha;
            float  _SoftEdge;

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 V   = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float  rim = saturate(dot(normalize(IN.normalWS), V)); // 0 在边缘，1 在中心
                rim        = smoothstep(0.0, _SoftEdge, rim);          // 可调柔和度
                float alpha = _Alpha * (1 - rim);                      // 边缘渐隐，但绝不全 0
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}