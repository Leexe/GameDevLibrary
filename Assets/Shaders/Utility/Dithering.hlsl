#ifndef DITHERING_HLSL
#define DITHERING_HLSL

static const float bayerMatrix2x2[4] = {
    0.0 / 4.0, 2.0 / 4.0,
    3.0 / 4.0, 1.0 / 4.0
};

static const float bayerMatrix4x4[16] = {
    0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
    3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
};

static const float bayerMatrix8x8[64] = {
    0.0 / 64.0, 48.0 / 64.0, 12.0 / 64.0, 60.0 / 64.0, 3.0 / 64.0, 51.0 / 64.0, 15.0 / 64.0, 63.0 / 64.0,
    32.0 / 64.0, 16.0 / 64.0, 44.0 / 64.0, 28.0 / 64.0, 35.0 / 64.0, 19.0 / 64.0, 47.0 / 64.0, 31.0 / 64.0,
    8.0 / 64.0, 56.0 / 64.0, 4.0 / 64.0, 52.0 / 64.0, 11.0 / 64.0, 59.0 / 64.0, 7.0 / 64.0, 55.0 / 64.0,
    40.0 / 64.0, 24.0 / 64.0, 36.0 / 64.0, 20.0 / 64.0, 43.0 / 64.0, 27.0 / 64.0, 39.0 / 64.0, 23.0 / 64.0,
    2.0 / 64.0, 50.0 / 64.0, 14.0 / 64.0, 62.0 / 64.0, 1.0 / 64.0, 49.0 / 64.0, 13.0 / 64.0, 61.0 / 64.0,
    34.0 / 64.0, 18.0 / 64.0, 46.0 / 64.0, 30.0 / 64.0, 33.0 / 64.0, 17.0 / 64.0, 45.0 / 64.0, 29.0 / 64.0,
    10.0 / 64.0, 58.0 / 64.0, 6.0 / 64.0, 54.0 / 64.0, 9.0 / 64.0, 57.0 / 64.0, 5.0 / 64.0, 53.0 / 64.0,
    42.0 / 64.0, 26.0 / 64.0, 38.0 / 64.0, 22.0 / 64.0, 41.0 / 64.0, 25.0 / 64.0, 37.0 / 64.0, 21.0 / 64.0
};

float Dither2x2(float2 screenPos)
{
    uint2 coord = (uint2)screenPos % 2;
    return bayerMatrix2x2[coord.y * 2 + coord.x];
}

float Dither4x4(float2 screenPos)
{
    uint2 coord = (uint2)screenPos % 4;
    return bayerMatrix4x4[coord.y * 4 + coord.x];
}

float Dither8x8(float2 screenPos)
{
    uint2 coord = (uint2)screenPos % 8;
    return bayerMatrix8x8[coord.y * 8 + coord.x];
}

float DitherCentered2x2(float2 screenPos, float spread = 1.0)
{
    return (Dither2x2(screenPos) - 0.5) * spread;
}

float DitherCentered4x4(float2 screenPos, float spread = 1.0)
{
    return (Dither4x4(screenPos) - 0.5) * spread;
}

float DitherCentered8x8(float2 screenPos, float spread = 1.0)
{
    return (Dither8x8(screenPos) - 0.5) * spread;
}

// Credit: https://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare/
float InterleavedGradientNoise(float2 screenPos)
{
    return frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))));
}

#endif
