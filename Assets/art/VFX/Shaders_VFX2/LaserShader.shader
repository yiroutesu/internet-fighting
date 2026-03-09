Shader "Custom/AdvancedLaser"
{
    Properties
    {
        // 基础属性
        [MainColor] _BaseColor("激光颜色", Color) = (1, 0, 0, 1)
        _CoreColor("核心颜色", Color) = (1, 1, 1, 1)
        _Intensity("强度", Range(0, 10)) = 2
        
        // 形状控制
        _Width("宽度", Range(0.01, 1)) = 0.1
        _CoreWidth("核心宽度", Range(0, 0.5)) = 0.02
        _Length("长度", Range(0.1, 10)) = 5
        
        // 动态效果
        _ScrollSpeed("滚动速度", Range(-5, 5)) = 2
        _PulseSpeed("脉动速度", Range(0, 5)) = 1
        _PulseAmount("脉动幅度", Range(0, 1)) = 0.3
        _ScanlineSpeed("扫描线速度", Range(0, 10)) = 3
        _ScanlineWidth("扫描线宽度", Range(0.01, 1)) = 0.3
        
        // 边缘效果
        _EdgeFade("边缘淡化", Range(0, 2)) = 0.5
        _NoiseAmount("噪波强度", Range(0, 1)) = 0.1
        
        // 高级效果
        _Distortion("扭曲强度", Range(0, 1)) = 0.1
        _GlowIntensity("辉光强度", Range(0, 5)) = 2
        _BloomThreshold("Bloom阈值", Range(0, 1)) = 0.8
        
        // 纹理
        [NoScaleOffset] _NoiseTex("噪波纹理", 2D) = "white" {}
        [NoScaleOffset] _GradientTex("渐变纹理", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Blend One One  // 加法混合
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "LaserPass"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };
            
            // 属性变量
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CoreColor;
                float _Intensity;
                float _Width;
                float _CoreWidth;
                float _Length;
                float _ScrollSpeed;
                float _PulseSpeed;
                float _PulseAmount;
                float _ScanlineSpeed;
                float _ScanlineWidth;
                float _EdgeFade;
                float _NoiseAmount;
                float _Distortion;
                float _GlowIntensity;
                float _BloomThreshold;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // 应用脉动效果到宽度
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount + 1.0;
                float width = _Width * pulse;
                
                // 沿法线方向扩展形成激光带
                float3 normalOS = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normalOS * width * (input.uv.x - 0.5) * 2.0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                
                // 计算视方向
                output.viewDir = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                // 雾效
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // 时间相关变量
                float time = _Time.y;
                
                // 重新计算pulse（修正错误）
                float pulse = sin(time * _PulseSpeed) * _PulseAmount + 1.0;
                
                // 噪波纹理采样
                float noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 
                    float2(uv.x * 2.0, uv.y + time * _ScrollSpeed * 0.1)).r;
                float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 
                    float2(uv.x * 5.0, uv.y * 0.5 + time * _ScrollSpeed * 0.2)).r;
                
                // 扭曲UV
                float2 distortedUV = uv;
                distortedUV.x += sin(uv.y * 10.0 + time * 3.0) * _Distortion * 0.1;
                
                // 渐变纹理
                float gradient = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, 
                    float2(uv.x, 0.5)).r;
                
                // 核心区域（中心最亮）
                float core = smoothstep(0.5 - _CoreWidth, 0.5 + _CoreWidth, 1.0 - abs(uv.x - 0.5) * 2.0);
                
                // 边缘淡化
                float edgeFade = pow(gradient, _EdgeFade);
                
                // 扫描线效果
                float scanlinePos = frac(time * _ScanlineSpeed);
                float scanline = smoothstep(0.0, _ScanlineWidth, abs(uv.y - scanlinePos)) * 
                                 smoothstep(0.0, _ScanlineWidth * 2.0, 1.0 - abs(uv.y - scanlinePos));
                
                // 组合噪波
                float combinedNoise = lerp(noise1, noise2, 0.5) * _NoiseAmount;
                
                // 主激光形状
                float laserShape = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
                laserShape = pow(laserShape, 4.0); // 更锐利的边缘
                
                // 应用噪波和扫描线
                laserShape *= (1.0 + combinedNoise) * (1.0 + scanline * 0.5);
                
                // 应用边缘淡化
                laserShape *= edgeFade;
                
                // 颜色混合
                half4 color = lerp(_BaseColor, _CoreColor, core * 0.8);
                color.rgb *= laserShape * _Intensity * (1.0 + pulse * 0.3);
                
                // 添加辉光
                float glow = smoothstep(0.1, 0.5, laserShape);
                color.rgb += glow * _GlowIntensity * _BaseColor.rgb;
                
                // 应用Bloom
                #if defined(_BLOOM_ENABLED)
                    color.a = saturate((color.r + color.g + color.b) / 3.0 - _BloomThreshold);
                #else
                    color.a = laserShape;
                #endif
                
                // 雾效
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/Unlit"
}