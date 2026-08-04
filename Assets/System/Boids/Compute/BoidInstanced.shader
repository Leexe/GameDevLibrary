Shader "Custom/BoidInstancedURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.6, 1.0, 1)
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct BoidData
            {
                float3 position;
                float3 velocity;
            };

            StructuredBuffer<BoidData> boidsBuffer;

            void setup()
            { }

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                BoidData boid = boidsBuffer[input.instanceID];

                // Build a rotation matrix so the boid faces its velocity
                float3 forward = normalize(boid.velocity);
                float3 up = float3(0, 1, 0);
                
                if (abs(forward.y) > 0.999) 
                {
                    up = float3(0, 0, 1);
                }
                    
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);

                // Create a custom Object-to-World Transformation Matrix
                float4x4 objectToWorld = float4x4(
                    right.x, up.x, forward.x, boid.position.x,
                    right.y, up.y, forward.y, boid.position.y,
                    right.z, up.z, forward.z, boid.position.z,
                    0,       0,    0,         1
                );

                // Transform the local vertex to world space using our custom matrix
                float3 positionWS = mul(objectToWorld, input.positionOS).xyz;
                
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
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
