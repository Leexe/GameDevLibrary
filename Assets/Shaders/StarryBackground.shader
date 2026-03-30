Shader "Unlit/StarryBackground"
{
    Properties
    {
        [Header(Scrolling)]
        [Tooltip(Controls how fast the fog moves horizontally)]
        _FogScrollSpeedX ("Fog Scroll Speed X", Float) = 0.0
        [Tooltip(Controls how fast the fog moves vertically)]
        _FogScrollSpeedY ("Fog Scroll Speed Y", Float) = 0.1
        [Tooltip(Controls how fast the stars move horizontally)]
        _StarScrollSpeedX ("Star Scroll Speed X", Float) = 0.0
        [Tooltip(Controls how fast the stars move vertically)]
        _StarScrollSpeedY ("Star Scroll Speed Y", Float) = 0.1

        [Header(Resolution)]
        [Tooltip(The pixelation resolution for fog. Lower values will pixelate)]
        _FogPixelResolution ("Fog Pixel Resolution", Range(1.0, 2048.0)) = 600.0
        [Tooltip(The pixelation resolution constraint for the star field lower values will pixelate)]
        _StarPixelResolution ("Star Pixel Resolution", Range(1.0, 2048.0)) = 600.0

        [Header(Fog)]
        [Tooltip(The number of noise layers used for the fog. Higher values add more detail)]
        _Octaves ("Octaves", Integer) = 10
        [Tooltip(The overall scale of the fog noise)]
        _FogScale ("Fog Scale", Float) = 1
        [Tooltip(How fast the fog festers in place)]
        _FogSpeed ("Fog Speed", Float) = 0.5
        [Tooltip(The color gradient applied to the fog)]
        _FogColorRamp ("Fog Color Ramp", 2D) = "white" {}
        [Toggle(USE_8X8_DITHER)] _Use8x8Dither ("Use 8x8 Dither", Float) = 0
        [Tooltip(Controls the intensity of the dithering)]
        _FogDitherSpread ("Fog Dither Spread", Range(0, 1)) = 0.05

        [Header(Stars)]
        [Tooltip(The density of the star grid)]
        _StarGrid ("Star Grid", Range(1, 1000)) = 700.0
        [Tooltip(The size of each individual star)]
        _StarSize ("Star Scale", Range(0.0, 1.0)) = 0.3
        [Tooltip(The opacity of the stars)]
        _StarOpacity ("Star Opacity", Range(0.0, 1.0)) = 1
        [Tooltip(The probability of a star appearing in a grid cell)]
        _StarProbability ("Star Probability", Range(0.0, 1.0)) = 0.02
        [Tooltip(The speed of the star twinkling)]
        _StarFlicker ("Star Flicker", Float) = 3
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "PreviewType"="Plane"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_8X8_DITHER

            #include "UnityCG.cginc"
            #include "Assets/Shaders/Utility/Fbm.hlsl"
            #include "Assets/Shaders/Utility/Dithering.hlsl"
            #include "Assets/Shaders/Utility/keijiro/SimplexNoise2D.hlsl"

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2D(_FogColorRamp);
            float _FogScrollSpeedX;
            float _FogScrollSpeedY;
            float _StarScrollSpeedX;
            float _StarScrollSpeedY;
            float _FogPixelResolution;
            float _StarPixelResolution;
            int _Octaves;
            float _StarGrid;
            float _StarSize;
            float _StarProbability;
            float _StarOpacity;
            float _FogScale;
            float _FogSpeed;
            float _FogDitherSpread;
            float _StarFlicker;

            Interpolators vert(MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(Interpolators i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                // Panning
                float2 fogPannedUV = i.uv;
                fogPannedUV.x += _Time.y * _FogScrollSpeedX;
                fogPannedUV.y += _Time.y * _FogScrollSpeedY;
                float2 starPannedUV = i.uv;
                starPannedUV.x += _Time.y * _StarScrollSpeedX;
                starPannedUV.y += _Time.y * _StarScrollSpeedY;

                // Pixelation
                float2 fogPixelUV = floor(fogPannedUV * _FogPixelResolution * float2(aspect, 1.0)) / _FogPixelResolution
                    / aspect;
                float2 starPixelUV = floor(starPannedUV * _StarPixelResolution * float2(aspect, 1.0)) /
                    _StarPixelResolution / aspect;

                // Dithering Fog
                int ditherX = i.uv.x * _FogPixelResolution * aspect;
                int ditherY = i.uv.y * _FogPixelResolution;
                #ifdef USE_8X8_DITHER
                float dither = bayerMatrix8x8[(ditherX % 8) + (ditherY % 8) * 8];
                #else
                float dither = bayerMatrix4x4[(ditherX % 4) + (ditherY % 4) * 4];
                #endif
                float fogNoise = (dither - 0.5) * _FogDitherSpread;

                // FBM Fog
                float offset = fbm(float3(_FogScale * fogPixelUV, _Time.y * _FogSpeed), _Octaves);
                fogNoise += fbm(float3(fogPixelUV + offset, _Time.y * _FogSpeed), _Octaves);
                // Sample Color From Color Ramp
                float3 fbmColor = UNITY_SAMPLE_TEX2D(_FogColorRamp, float2(fogNoise, 0.5)).rgb;

                // Stars
                // 1) Create Grid
                float2 starGrid = floor(starPixelUV * _StarGrid);
                // 2) Rescale Simplex Noise to be in range [0, 1]
                float randomStar = SimplexNoise(starGrid * 0.314 + 0.5) * 0.5 + 0.5;
                // 3) Offset the center of the star within each cell
                float offsetX = SimplexNoise(starGrid * 0.314 + float2(13.5, 0.0)) * 0.4;
                float offsetY = SimplexNoise(starGrid * 0.314 + float2(0.0, 42.7)) * 0.4;
                // 4) Get the local position of the star within the cell
                float2 starLocal = frac(starPixelUV * _StarGrid) - 0.5 - float2(offsetX, offsetY);
                // 5) Draw a small dot for star
                float starShape = smoothstep(_StarSize, 0.0, length(starLocal));
                float star = starShape * step(1.0 - _StarProbability, randomStar);
                // 6) Make stars twinkle
                float twinkle = sin(_Time.y * _StarFlicker + randomStar * 1000.0) * 0.5 + 0.5;
                float3 stars = star * twinkle * _StarOpacity;

                return float4(fbmColor + stars, 1.0);
            }
            ENDCG
        }
    }
}
