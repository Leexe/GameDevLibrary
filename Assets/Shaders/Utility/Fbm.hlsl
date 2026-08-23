#ifndef FBM_HLSL
#define FBM_HLSL

float FbmHash21(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac((p.x + p.y) * p.y);
}

float FbmNoise21(float2 p)
{
    float2 i = floor(p);
    float2 u = smoothstep(float2(0.0, 0.0), float2(1.0, 1.0), frac(p));
    float a = FbmHash21(i + float2(0.0, 0.0));
    float b = FbmHash21(i + float2(1.0, 0.0));
    float c = FbmHash21(i + float2(0.0, 1.0));
    float d = FbmHash21(i + float2(1.0, 1.0));
    float r = lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
    return r * r;
}

float fbm(float2 uv, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float e = 3.0;

    for (int i = 0; i < octaves; ++i)
    {
        value += amplitude * FbmNoise21(uv);
        uv = uv * e;
        amplitude *= 0.5;
        e *= 0.95;
    }
    return value;
}

// Credit to https://gist.github.com/sneha-belkhale/d944211b9af1e3575392d4e460676f30

float FbmHash31(float3 p)
{
    p = frac(p * float3(443.897, 441.423, 437.195));
    p += dot(p, p.yzx + 19.19);
    return frac((p.x + p.y) * p.z);
}

float FbmNoise31(float3 p)
{
    float3 i = floor(p);
    float3 u = smoothstep(float3(0, 0, 0), float3(1, 1, 1), frac(p));
    
    float a = FbmHash31(i + float3(0.0, 0.0, 0.0));
    float b = FbmHash31(i + float3(1.0, 0.0, 0.0));
    float c = FbmHash31(i + float3(0.0, 1.0, 0.0));
    float d = FbmHash31(i + float3(1.0, 1.0, 0.0));
    float e = FbmHash31(i + float3(0.0, 0.0, 1.0));
    float f = FbmHash31(i + float3(1.0, 0.0, 1.0));
    float g = FbmHash31(i + float3(0.0, 1.0, 1.0));
    float h = FbmHash31(i + float3(1.0, 1.0, 1.0));
    
    float k0 = lerp(a, b, u.x);
    float k1 = lerp(c, d, u.x);
    float k2 = lerp(e, f, u.x);
    float k3 = lerp(g, h, u.x);
    
    float k4 = lerp(k0, k1, u.y);
    float k5 = lerp(k2, k3, u.y);
    
    float r = lerp(k4, k5, u.z);
    return r * r;
}

float fbm(float3 uv, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float e = 3.0;
    
    for (int i = 0; i < octaves; ++i)
    {
        value += amplitude * FbmNoise31(uv);
        uv = uv * e;
        amplitude *= 0.5;
        e *= 0.95;
    }
    return value;
}

#endif
