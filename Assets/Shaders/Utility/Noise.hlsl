#ifndef NOISE_HLSL
#define NOISE_HLSL

#include "Assets/Shaders/Utility/Math.hlsl"

// Hash Without Sine - https://www.shadertoy.com/view/4djSRW

float Hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float Hash13(float3 p3)
{
    p3 = frac(p3 * 0.1031);
    p3 += dot(p3, p3.zyx + 31.32);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float3 Hash33(float3 p3)
{
    p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yxx) * p3.zyx);
}

// PCG Hash - https://www.shadertoy.com/view/XlGcRh

uint Pcg1d(uint v)
{
    uint state = v * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

uint2 Pcg2d(uint2 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v = v ^ (v >> 16u);
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v = v ^ (v >> 16u);
    return v;
}

uint3 Pcg3d(uint3 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    return v;
}

uint4 Pcg4d(uint4 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;
    v ^= v >> 16u;
    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;
    return v;
}

float Pcg1dFloat(uint v)
{
    return float(Pcg1d(v)) * (1.0 / 4294967296.0);
}

float2 Pcg2dFloat(uint2 v)
{
    return float2(Pcg2d(v)) * (1.0 / 4294967296.0);
}

float3 Pcg3dFloat(uint3 v)
{
    return float3(Pcg3d(v)) * (1.0 / 4294967296.0);
}

float4 Pcg4dFloat(uint4 v)
{
    return float4(Pcg4d(v)) * (1.0 / 4294967296.0);
}

// Value Noise - https://iquilezles.org/articles/morenoise/

float ValueNoise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float a = Hash12(i + float2(0.0, 0.0));
    float b = Hash12(i + float2(1.0, 0.0));
    float c = Hash12(i + float2(0.0, 1.0));
    float d = Hash12(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float ValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float a = Hash13(i + float3(0.0, 0.0, 0.0));
    float b = Hash13(i + float3(1.0, 0.0, 0.0));
    float c = Hash13(i + float3(0.0, 1.0, 0.0));
    float d = Hash13(i + float3(1.0, 1.0, 0.0));
    float e = Hash13(i + float3(0.0, 0.0, 1.0));
    float f1 = Hash13(i + float3(1.0, 0.0, 1.0));
    float g = Hash13(i + float3(0.0, 1.0, 1.0));
    float h = Hash13(i + float3(1.0, 1.0, 1.0));

    float k0 = lerp(a, b, u.x);
    float k1 = lerp(c, d, u.x);
    float k2 = lerp(e, f1, u.x);
    float k3 = lerp(g, h, u.x);

    float k4 = lerp(k0, k1, u.y);
    float k5 = lerp(k2, k3, u.y);

    return lerp(k4, k5, u.z);
}

// Gradient Noise - https://iquilezles.org/articles/gradientnoise/

float GradientNoise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float2 ga = Hash22(i + float2(0.0, 0.0)) * 2.0 - 1.0;
    float2 gb = Hash22(i + float2(1.0, 0.0)) * 2.0 - 1.0;
    float2 gc = Hash22(i + float2(0.0, 1.0)) * 2.0 - 1.0;
    float2 gd = Hash22(i + float2(1.0, 1.0)) * 2.0 - 1.0;

    float va = dot(ga, f - float2(0.0, 0.0));
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));

    return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
}

// Simplex Noise - https://www.shadertoy.com/view/Msf3WH

float SimplexNoise2D(float2 p)
{
    const float K1 = 0.366025404; // (sqrt(3.0) - 1.0) / 2.0;
    const float K2 = 0.211324865; // (3.0 - sqrt(3.0)) / 6.0;

    float2 i = floor(p + (p.x + p.y) * K1);
    float2 a = p - i + (i.x + i.y) * K2;
    float m = step(a.y, a.x);
    float2 o = float2(m, 1.0 - m);
    float2 b = a - o + K2;
    float2 c = a - 1.0 + 2.0 * K2;

    float2 ga = Hash22(i + 0.0) * 2.0 - 1.0;
    float2 gb = Hash22(i + o) * 2.0 - 1.0;
    float2 gc = Hash22(i + 1.0) * 2.0 - 1.0;

    float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    float3 h2 = h * h;
    float3 h4 = h2 * h2;

    float3 n = h4 * float3(dot(a, ga), dot(b, gb), dot(c, gc));
    return 70.0 * dot(n, float3(1.0, 1.0, 1.0));
}

// Simplex Noise Derivatives - https://www.shadertoy.com/view/XdXGW8

static float3 SimplexNoiseGrad2D(float2 p)
{
    const float K1 = 0.366025404; // (sqrt(3.0) - 1.0) / 2.0;
    const float K2 = 0.211324865; // (3.0 - sqrt(3.0)) / 6.0;

    float2 i = floor(p + (p.x + p.y) * K1);
    float2 a = p - i + (i.x + i.y) * K2;
    float m = step(a.y, a.x);
    float2 o = float2(m, 1.0 - m);
    float2 b = a - o + K2;
    float2 c = a - 1.0 + 2.0 * K2;

    float2 ga = Hash22(i + 0.0) * 2.0 - 1.0;
    float2 gb = Hash22(i + o) * 2.0 - 1.0;
    float2 gc = Hash22(i + 1.0) * 2.0 - 1.0;

    float3 va = float3(dot(a, ga), dot(b, gb), dot(c, gc));
    float3 d = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    float3 d2 = d * d;
    float3 d4 = d2 * d2;

    float3 n = d4 * va;

    float2 deriv = -8.0 * (d2.x * d.x * va.x * a + d2.y * d.y * va.y * b + d2.z * d.z * va.z * c) +
                   d4.x * ga + d4.y * gb + d4.z * gc;

    return float3(70.0 * deriv, 70.0 * dot(n, float3(1.0, 1.0, 1.0)));
}

// Voronoi Noise - https://iquilezles.org/articles/voronoise/

float4 Voronoi(float2 uv, float angleOffset = 0.0)
{
    float2 g = floor(uv);
    float2 f = frac(uv);
    float minF1 = 8.0;
    float minF2 = 8.0;
    float2 cellID = float2(0.0, 0.0);

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = Hash33(float3(g + lattice, angleOffset)).xy;
            float2 v = lattice + offset - f;
            float d = dot(v, v);

            if (d < minF1)
            {
                minF2 = minF1;
                minF1 = d;
                cellID = g + lattice;
            }
            else if (d < minF2)
            {
                minF2 = d;
            }
        }
    }
    return float4(sqrt(minF1), sqrt(minF2), cellID);
}

// Fractal Brownian Motion - https://iquilezles.org/articles/fbm/

float FBM2D(float2 p, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float value = 0.0;
    float amplitude = 0.5;
    float2x2 rot = float2x2(0.80, 0.60, -0.60, 0.80);

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * ValueNoise2D(p);
        p = mul(rot, p) * lacunarity;
        amplitude *= gain;
    }
    return value;
}

float FBM3D(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float value = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * ValueNoise3D(p);
        p = p * lacunarity;
        amplitude *= gain;
    }
    return value;
}

// Curl Noise

float2 CurlNoise2D(float2 p)
{
    float3 g = SimplexNoiseGrad2D(p);
    return float2(g.y, -g.x);
}

float3 CurlNoise3D(float3 p)
{
    float3 g_yz = SimplexNoiseGrad2D(p.yz);
    float3 g_zx = SimplexNoiseGrad2D(p.zx);
    float3 g_xy = SimplexNoiseGrad2D(p.xy);

    float3 curl = float3(
        g_xy.y - g_zx.x,
        g_yz.y - g_xy.x,
        g_zx.y - g_yz.x
    );

    return normalize(curl);
}

#endif
