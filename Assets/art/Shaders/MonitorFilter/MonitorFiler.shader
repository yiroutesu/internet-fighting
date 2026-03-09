Shader "Custom/MonitorFilter"
{
    Properties
    {
        // 基础设置
        _MainTex ("Screen Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.2, 0.8, 0.2, 1)
        
        // 监视器效果
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.2
        _ScanlineSpeed ("Scanline Speed", Range(0, 10)) = 2.0
        _ScanlineDensity ("Scanline Density", Range(0, 500)) = 200
        _PixelSize ("Pixel Size", Range(1, 50)) = 3
        _ChromaOffset ("Chromatic Aberration", Range(0, 0.05)) = 0.01
        _Distortion ("Screen Distortion", Range(0, 0.1)) = 0.02
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.3
        
        // CRT效果
        _Curvature ("Screen Curvature", Range(0, 0.05)) = 0.02
        _CRTReflection ("CRT Reflection", Range(0, 1)) = 0.1
        _BloomIntensity ("Bloom Intensity", Range(0, 1)) = 0.1
        
        // 噪波与静态
        _NoiseIntensity ("Noise Intensity", Range(0, 0.5)) = 0.05
        _StaticFreq ("Static Frequency", Range(0, 100)) = 20
        _StaticSpeed ("Static Speed", Range(0, 10)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _ScanlineDensity;
            float _PixelSize;
            float _ChromaOffset;
            float _Distortion;
            float _VignetteIntensity;
            float _Curvature;
            float _CRTReflection;
            float _BloomIntensity;
            float _NoiseIntensity;
            float _StaticFreq;
            float _StaticSpeed;
            
            // 随机函数
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 噪波函数
            float noise(float2 uv)
            {
                float2 noiseUV = floor(uv * _StaticFreq) + _Time.y * _StaticSpeed;
                return rand(noiseUV) * _NoiseIntensity;
            }
            
            // 扫描线函数
            float scanlines(float2 uv, float time)
            {
                float scanline = sin(uv.y * _ScanlineDensity + time * _ScanlineSpeed) * 0.5 + 0.5;
                return 1.0 - scanline * _ScanlineIntensity;
            }
            
            // 屏幕弯曲效果
            float2 crtCurve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) * _Curvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                return saturate(uv);
            }
            
            // 渐晕效果
            float vignette(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(uv, uv) * _VignetteIntensity;
                return saturate(vignette);
            }
            
            // 像素化效果
            float2 pixelate(float2 uv)
            {
                float2 pixelScale = float2(_ScreenParams.x / _PixelSize, _ScreenParams.y / _PixelSize);
                return floor(uv * pixelScale) / pixelScale;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 获取屏幕UV
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // 添加屏幕扭曲
                float2 distortedUV = screenUV;
                distortedUV.x += sin(screenUV.y * 20.0 + _Time.y) * _Distortion;
                distortedUV.y += sin(screenUV.x * 15.0 + _Time.y * 1.5) * _Distortion * 0.5;
                
                // CRT弯曲效果
                float2 curvedUV = crtCurve(distortedUV);
                
                // 检查UV是否在有效范围内
                if (curvedUV.x < 0.0 || curvedUV.x > 1.0 || curvedUV.y < 0.0 || curvedUV.y > 1.0)
                    return fixed4(0, 0, 0, 1);
                
                // 像素化
                float2 pixelUV = pixelate(curvedUV);
                
                // 获取原始屏幕颜色
                fixed4 screenColor = tex2D(_MainTex, pixelUV);
                
                // 应用基础颜色
                float luminance = dot(screenColor.rgb, float3(0.299, 0.587, 0.114));
                fixed4 baseColor = lerp(screenColor, fixed4(_BaseColor.rgb * luminance, screenColor.a), _BaseColor.a);
                
                // 色差效果
                fixed4 chromaColor = baseColor;
                float2 offset = float2(_ChromaOffset, 0);
                chromaColor.r = tex2D(_MainTex, pixelUV + offset).r;
                chromaColor.b = tex2D(_MainTex, pixelUV - offset).b;
                
                // 扫描线效果
                float scanline = scanlines(pixelUV, _Time.y);
                chromaColor.rgb *= scanline;
                
                // 添加随机噪点
                float staticNoise = noise(pixelUV);
                chromaColor.rgb += staticNoise;
                
                // 渐晕效果
                float vignetteFactor = vignette(pixelUV);
                chromaColor.rgb *= vignetteFactor;
                
                // CRT反射效果
                float reflection = sin(pixelUV.y * 100 + _Time.y * 3) * 0.5 + 0.5;
                chromaColor.rgb += reflection * _CRTReflection * baseColor.rgb;
                
                // 简单的Bloom效果
                float bloom = smoothstep(0.7, 1.0, luminance) * _BloomIntensity;
                chromaColor.rgb += baseColor.rgb * bloom;
                
                return chromaColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}