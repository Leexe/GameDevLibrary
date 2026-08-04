Shader "Custom/BoidInstancedURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _BoidScale ("Boid Scale", Float) = 0.5
        _RotationOffset ("Rotation Offset (XYZ)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float _BoidScale;
                float4 _RotationOffset;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct BoidData
            {
                float3 position;
                float3 velocity;
                float3 forward;
                float3 up;
                float maxSpeed;
            };

            StructuredBuffer<BoidData> boidsBuffer;

            void setup()
            { }

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                BoidData boid = boidsBuffer[input.instanceID];

                float3 forward = normalize(boid.forward);
                float3 up = normalize(boid.up);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                // Create a custom Object-to-World Transformation Matrix
                float4x4 objectToWorld = float4x4(
                    right.x * _BoidScale, up.x * _BoidScale, forward.x * _BoidScale, boid.position.x,
                    right.y * _BoidScale, up.y * _BoidScale, forward.y * _BoidScale, boid.position.y,
                    right.z * _BoidScale, up.z * _BoidScale, forward.z * _BoidScale, boid.position.z,
                    0,       0,    0,         1
                );

                // Apply a local XYZ rotation offset (convert degrees to radians)
                float3 rad = _RotationOffset.xyz * 0.0174532925;
                float3 s, c;
                sincos(rad, s, c);
                
                // Build standard rotation matrices
                float3x3 rX = float3x3(1, 0, 0, 0, c.x, -s.x, 0, s.x, c.x);
                float3x3 rY = float3x3(c.y, 0, s.y, 0, 1, 0, -s.y, 0, c.y);
                float3x3 rZ = float3x3(c.z, -s.z, 0, s.z, c.z, 0, 0, 0, 1);

                // Apply rotations in Unity's standard Z -> X -> Y order
                float3 rotatedPosOS = mul(rY, mul(rX, mul(rZ, input.positionOS.xyz)));

                // Transform the local vertex to world space using our custom matrix
                float3 positionWS = mul(objectToWorld, float4(rotatedPosOS, 1.0)).xyz;
                
                // Project to the camera's clipping space
                output.positionCS = TransformWorldToHClip(positionWS);
            #else
                // Fallback if not instanced (e.g. viewed in the material preview window)
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            #endif

                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}
