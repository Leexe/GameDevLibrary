Shader "Fullscreen/RetroShader"
{
    Properties
    {
        [Header(Pixelization)]
        _PixelSize ("Pixel Size", Vector) = (4, 4, 0, 0)

        [Header(Color Settings)]
        _ColorDepth ("Color Depth", Range(2, 64)) = 8

        [Header(Dithering)]
        [KeywordEnum(2x2, 4x4, 8x8)]
        _BayerMatrix ("Bayer Matrix", Float) = 2
        _DitherSpread ("Dither Spread", Range(0, 1)) = 0.05
        _DitherStrength ("Dither Strength", Range(0, 5)) = 1.0
        _DitherDarken ("Dither Darken", Range(-1, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "RetroPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _BAYERMATRIX_2X2 _BAYERMATRIX_4X4 _BAYERMATRIX_8X8

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Assets/Shaders/Utility/Dithering.hlsl"

            float2 _PixelSize;
            float _ColorDepth;
            float _DitherSpread;
            float _DitherStrength;
            float _DitherDarken;
            half4 frag(Varyings i) : SV_Target
            {
                // Pixelization
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 gridSize = floor(float2(_ScreenParams.y * aspect, _ScreenParams.y) / _PixelSize.x);
                float2 pixelUV = (floor(i.texcoord * gridSize) + 0.5) / gridSize;

                // Sample Screen
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, pixelUV);

                // Convert to sRGB
                col.rgb = LinearToSRGB(col.rgb);
                
                // Dithering
                float2 ditherPos = pixelUV * gridSize;
#if defined(_BAYERMATRIX_2X2)
                float dither = DitherCentered2x2(ditherPos, _DitherSpread);
#elif defined(_BAYERMATRIX_4X4)
                float dither = DitherCentered4x4(ditherPos, _DitherSpread);
#else
                float dither = DitherCentered8x8(ditherPos, _DitherSpread);
#endif
                col.rgb += dither * _DitherStrength - _DitherDarken;

                // Quantization
                col.rgb = round(col.rgb * _ColorDepth) / _ColorDepth;

                // Convert to Linear
                col.rgb = SRGBToLinear(col.rgb);

                return col;
            }
            ENDHLSL
        }
    }
}
