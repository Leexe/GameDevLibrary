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

// Simplex Noise - https://github.com/ashima/webgl-noise

static float3 SimplexNoiseGrad2D(float2 v)
{
    const float C1 = (3.0 - sqrt(3.0)) / 6.0;
    const float C2 = (sqrt(3.0) - 1.0) / 2.0;

    float2 i  = floor(v + dot(v, C2));
    float2 x0 = v - i + dot(i, C1);

    float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float2 x1 = x0 + C1 - i1;
    float2 x2 = x0 + C1 * 2.0 - 1.0;

    float2 p0 = Hash22(i);
    float2 p1 = Hash22(i + i1);
    float2 p2 = Hash22(i + 1.0);

    float3 phi = float3(p0.x, p1.x, p2.x) * TWO_PI;
    float2 g0 = float2(cos(phi.x), sin(phi.x));
    float2 g1 = float2(cos(phi.y), sin(phi.y));
    float2 g2 = float2(cos(phi.z), sin(phi.z));

    float3 m  = float3(dot(x0, x0), dot(x1, x1), dot(x2, x2));
    float3 px = float3(dot(g0, x0), dot(g1, x1), dot(g2, x2));

    m = max(0.5 - m, 0.0);
    float3 m3 = m * m * m;
    float3 m4 = m * m3;

    float3 temp = -8.0 * m3 * px;
    float2 grad = m4.x * g0 + temp.x * x0 +
                  m4.y * g1 + temp.y * x1 +
                  m4.z * g2 + temp.z * x2;

    float noise = dot(m4, px);
    return float3(grad, 70.0 * noise);
}

float SimplexNoise2D(float2 v)
{
    return SimplexNoiseGrad2D(v).z;
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

// Curl Noise - Robert Bridson (SIGGRAPH 2007)

float3 CurlNoise(float3 p, float step = 0.01)
{
    float3 dx = float3(step, 0.0, 0.0);
    float3 dy = float3(0.0, step, 0.0);
    float3 dz = float3(0.0, 0.0, step);

    float n1 = ValueNoise2D((p + dy).xy);
    float n2 = ValueNoise2D((p - dy).xy);
    float n3 = ValueNoise2D((p + dz).yz);
    float n4 = ValueNoise2D((p - dz).yz);
    float n5 = ValueNoise2D((p + dx).zx);
    float n6 = ValueNoise2D((p - dx).zx);

    float x = (n1 - n2) - (n3 - n4);
    float y = (n5 - n6) - (n1 - n2);
    float z = (n3 - n4) - (n5 - n6);

    return normalize(float3(x, y, z) / (2.0 * step + EPSILON));
}

// Shader Graph Custom Function Wrappers

void ValueNoise2D_float(float2 UV, out float Out) { Out = ValueNoise2D(UV); }
void ValueNoise2D_half(half2 UV, out half Out)   { Out = (half)ValueNoise2D((float2)UV); }

void ValueNoise3D_float(float3 Position, out float Out) { Out = ValueNoise3D(Position); }
void ValueNoise3D_half(half3 Position, out half Out)   { Out = (half)ValueNoise3D((float3)Position); }

void GradientNoise2D_float(float2 UV, out float Out) { Out = GradientNoise2D(UV); }
void GradientNoise2D_half(half2 UV, out half Out)   { Out = (half)GradientNoise2D((float2)UV); }

void SimplexNoise2D_float(float2 UV, out float Out) { Out = SimplexNoise2D(UV); }
void SimplexNoise2D_half(half2 UV, out half Out)   { Out = (half)SimplexNoise2D((float2)UV); }

void Voronoi_float(float2 UV, float AngleOffset, out float F1, out float F2, out float2 CellID)
{
    float4 v = Voronoi(UV, AngleOffset);
    F1 = v.x;
    F2 = v.y;
    CellID = v.zw;
}
void Voronoi_half(half2 UV, half AngleOffset, out half F1, out half F2, out half2 CellID)
{
    float4 v = Voronoi((float2)UV, (float)AngleOffset);
    F1 = (half)v.x;
    F2 = (half)v.y;
    CellID = (half2)v.zw;
}

void FBM2D_float(float2 UV, int Octaves, float Lacunarity, float Gain, out float Out)
{
    Out = FBM2D(UV, Octaves, Lacunarity, Gain);
}
void FBM2D_half(half2 UV, int Octaves, half Lacunarity, half Gain, out half Out)
{
    Out = (half)FBM2D((float2)UV, Octaves, (float)Lacunarity, (float)Gain);
}

void FBM3D_float(float3 Position, int Octaves, float Lacunarity, float Gain, out float Out)
{
    Out = FBM3D(Position, Octaves, Lacunarity, Gain);
}
void FBM3D_half(half3 Position, int Octaves, half Lacunarity, half Gain, out half Out)
{
    Out = (half)FBM3D((float3)Position, Octaves, (float)Lacunarity, (float)Gain);
}

void CurlNoise_float(float3 Position, float Step, out float3 Out)
{
    Out = CurlNoise(Position, Step);
}
void CurlNoise_half(half3 Position, half Step, out half3 Out)
{
    Out = (half3)CurlNoise((float3)Position, (float)Step);
}

#endif
