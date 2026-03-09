Shader "Custom/EnemyBullet_Fixed"
{
    Properties
    {
        // 主颜色属性
        [HDR]_MainColor ("Main Color", Color) = (1, 0, 0, 1)
        // 描边颜色属性
        [HDR]_OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        // 描边宽度
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        // 主纹理
        _MainTex ("Main Texture", 2D) = "white" {}
        // 纹理平铺
        _TextureScale ("Texture Scale", Range(0.1, 5)) = 1
        // 纹理强度
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.5
        // 闪烁速度 (0表示不闪烁)
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3
        // 闪烁强度
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.2
        // 光泽度
        _Glossiness ("Glossiness", Range(0, 1)) = 0.5
        // 金属度
        _Metallic ("Metallic", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // 使用ZWrite确保正确的深度渲染顺序
        ZWrite On
        ZTest LEqual
        
        // 第一个Pass：渲染描边（使用背面挤出技术）
        Pass
        {
            Name "Outline"
            Cull Front // 只渲染背面，用于描边
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // 沿法线方向挤出顶点，实现描边效果
                float3 normalOS = normalize(IN.normalOS);
                float3 positionOS = IN.positionOS.xyz + normalOS * _OutlineWidth;
                
                // 转换到裁剪空间
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                OUT.positionHCS = vertexInput.positionCS;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // 第二个Pass：渲染主颜色（使用URP的Lit着色器）
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back // 只渲染正面
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                #ifdef _ADDITIONAL_LIGHTS
                    float3 viewDirWS : TEXCOORD5;
                #endif
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _MainTex_ST;
                float _TextureScale;
                float _TextureStrength;
                float _PulseSpeed;
                float _PulseIntensity;
                float _Glossiness;
                float _Metallic;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 计算基础光照
            half3 LightingModel(float3 positionWS, float3 normalWS, float3 viewDir, 
                                float metallic, float glossiness, half3 albedo)
            {
                // 获取主光源
                Light mainLight = GetMainLight();
                
                // 计算漫反射
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * NdotL;
                
                // 计算高光
                float3 halfVector = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specularPower = exp2(glossiness * 10 + 1);
                half3 specular = mainLight.color * pow(NdotH, specularPower) * metallic;
                
                // 结合环境光
                half3 ambient = SampleSH(normalWS) * 0.5;
                
                // 最终光照结果
                half3 lighting = (diffuse + ambient) * albedo + specular;
                
                return lighting;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                
                OUT.positionHCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex) * _TextureScale;
                
                // 计算阴影坐标
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                
                // 传递光照数据
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);
                
                #ifdef _ADDITIONAL_LIGHTS
                    OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                #endif
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 采样纹理
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // 混合纹理和主颜色
                half3 albedo = lerp(_MainColor.rgb, _MainColor.rgb * texColor.rgb, _TextureStrength);
                
                // 添加脉冲效果
                if (_PulseSpeed > 0)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    albedo.rgb += pulse * _PulseIntensity * _MainColor.rgb;
                }
                
                // 计算光照
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 lighting = LightingModel(IN.positionWS, normalize(IN.normalWS), viewDir, 
                                               _Metallic, _Glossiness, albedo);
                
                // 应用阴影
                #if _MAIN_LIGHT_SHADOWS
                    Light mainLight = GetMainLight(IN.shadowCoord);
                    float shadow = mainLight.shadowAttenuation;
                    lighting *= shadow;
                #endif
                
                // 附加光源
                #ifdef _ADDITIONAL_LIGHTS
                    int additionalLightsCount = GetAdditionalLightsCount();
                    for (int i = 0; i < additionalLightsCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, IN.positionWS);
                        float3 lightDir = light.direction;
                        float3 lightColor = light.color;
                        float attenuation = light.distanceAttenuation * light.shadowAttenuation;
                        
                        // 漫反射
                        float NdotL = saturate(dot(IN.normalWS, lightDir));
                        lighting += lightColor * NdotL * attenuation * albedo * 0.5;
                    }
                #endif
                
                return half4(lighting, _MainColor.a);
            }
            ENDHLSL
        }
        
        // 阴影投射Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.texcoord;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // 深度Only Pass (用于深度纹理)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.texcoord;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    // 回退到标准Shader
    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI"
}