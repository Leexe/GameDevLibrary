#ifndef MATH_HLSL
#define MATH_HLSL

#ifndef PI
#define PI 3.14159265358979323846
#endif

#ifndef TWO_PI
#define TWO_PI 6.28318530717958647692
#endif

#ifndef TAU
#define TAU 6.28318530717958647692
#endif

#ifndef INV_PI
#define INV_PI 0.31830988618379067154
#endif

#ifndef HALF_PI
#define HALF_PI 1.57079632679489661923
#endif

#ifndef SQRT2
#define SQRT2 1.41421356237309504880
#endif

#ifndef EPSILON
#define EPSILON 1e-6
#endif

// Remap Functions - https://iquilezles.org/articles/functions/

float Gain(float x, float k)
{
    float a = 0.5 * pow(2.0 * ((x < 0.5) ? x : 1.0 - x), k);
    return (x < 0.5) ? a : 1.0 - a;
}

float AlmostIdentity(float x, float m, float n)
{
    if (x > m) return x;
    float a = 2.0 * n - m;
    float b = 2.0 * m - 3.0 * n;
    float t = x / m;
    return (a * t + b) * t * t + n;
}

// Rotation Functions

float2 Rotate2D(float2 uv, float angleRadians)
{
    float s = sin(angleRadians);
    float c = cos(angleRadians);
    return mul(float2x2(c, -s, s, c), uv);
}

float3 RotateAboutAxis(float3 v, float3 axis, float angleRadians)
{
    axis = normalize(axis);
    float s = sin(angleRadians);
    float c = cos(angleRadians);
    return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
}

#endif
