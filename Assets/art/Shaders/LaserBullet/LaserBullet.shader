Shader "URP/LaserBulletOpaqueOutlineSimple"
{
    Properties
    {
        // 基础颜色
        [HDR] _BaseColor("Base Color", Color) = (2, 0.8, 0.3, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (4, 2, 1, 1)
        
        // 描边参数
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0.001, 0.1)) = 0.01
        _OutlineIntensity("Outline Intensity", Range(0, 2)) = 1
        
        // 核心发光参数
        _CoreIntensity("Core Intensity", Range(1, 8)) = 3
        _RimPower("Rim Power", Range(1, 5)) = 2
        _RimIntensity("Rim Intensity", Range(0, 3)) = 1
        
        // 动态效果
        _PulseSpeed("Pulse Speed", Range(0, 3)) = 1
        _PulseIntensity("Pulse Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // 描边Pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Front
            ZWrite On
            
            HLSLPROGRAM
            
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineIntensity;
            CBUFFER_END
            
            Varyings vertOutline(Attributes input)
            {
                Varyings output;
                
                // 沿法线方向扩展顶点
                float3 positionOS = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                
                output.positionHCS = TransformObjectToHClip(positionOS);
                
                return output;
            }
            
            half4 fragOutline(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb * _OutlineIntensity, 1.0);
            }
            ENDHLSL
        }

        // 主Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _CoreIntensity;
                float _RimPower;
                float _RimIntensity;
                float _PulseSpeed;
                float _PulseIntensity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 脉动效果
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float pulseOffset = pulse * _PulseIntensity * 0.01;
                
                // 应用脉动
                input.positionOS.xyz += input.normalOS * pulseOffset;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - output.positionWS);
                output.uv = input.uv;
                
                // 雾效
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // 获取主光源
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                
                // 基础光照
                float NDotL = saturate(dot(normalWS, lightDir));
                
                // 菲涅尔效果
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rim = pow(fresnel, _RimPower) * _RimIntensity;
                
                // 核心发光
                float2 centerUV = input.uv - 0.5;
                float radius = length(centerUV) * 2.0;
                float coreGlow = (1.0 - saturate(radius)) * _CoreIntensity;
                
                // 脉动效果
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                
                // 组合自发光
                float emission = (coreGlow * 0.7 + rim * 0.3 + pulse * _PulseIntensity);
                
                // 基础颜色
                half3 baseColor = _BaseColor.rgb * (0.5 + NDotL * 0.5);
                
                // 自发光
                half3 emissionColor = _EmissionColor.rgb * emission;
                
                // 最终颜色
                half3 finalColor = baseColor + emissionColor;
                
                // 应用雾效
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}