Shader "Custom/BoidInstancedURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _RotationOffset ("Rotation Offset (XYZ)", Vector) = (0, 0, 0, 0)
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _MainTex_ST;
        float  _Metallic;
        float  _Smoothness;
        float  _BoidScale;
        float4 _RotationOffset;
        int    _InstanceOffset;
    CBUFFER_END

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    struct BoidData
    {
        float3 position;
        float3 velocity;
        float3 forward;
        float3 up;
        float  maxSpeed;
    };

    StructuredBuffer<BoidData> boidsBuffer;

    #define DEG2RAD 0.0174532925

    void setup() { }

    /// Transforms an object-space position and normal into world space
    /// using the boid's orientation basis from the compute buffer.
    void GetBoidPositionAndNormalWS(
        float4 positionOS, float3 normalOS, uint instanceID,
        out float3 positionWS, out float3 normalWS)
    {
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        BoidData boid = boidsBuffer[instanceID + _InstanceOffset];

        // Build Orthonormal Basis
        float3 forward = normalize(boid.forward);
        float3 up = normalize(boid.up);
        float3 right = normalize(cross(up, forward));
        up = cross(forward, right);
   
        // Rotation Offset
        float3 rad = _RotationOffset.xyz * DEG2RAD;
        float3 s, c;
        sincos(rad, s, c);
        float3x3 rX = float3x3(1, 0, 0, 0, c.x, -s.x, 0, s.x, c.x);
        float3x3 rY = float3x3(c.y, 0, s.y, 0, 1, 0, -s.y, 0, c.y);
        float3x3 rZ = float3x3(c.z, -s.z, 0, s.z, c.z, 0, 0, 0, 1);
        float3x3 localRotation = mul(rY, mul(rX, rZ));
        float3 rotatedPosOS = mul(localRotation, positionOS.xyz);
        float3 rotatedNormalOS = mul(localRotation, normalOS);

        // Transform to World Space
        positionWS = rotatedPosOS.x * right   * _BoidScale
                   + rotatedPosOS.y * up      * _BoidScale
                   + rotatedPosOS.z * forward * _BoidScale
                   + boid.position;

        normalWS = normalize(rotatedNormalOS.x * right
                           + rotatedNormalOS.y * up
                           + rotatedNormalOS.z * forward);
    #else
        positionWS = TransformObjectToWorld(positionOS.xyz);
        normalWS   = TransformObjectToWorldNormal(normalOS);
    #endif
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

        // Pass 1 — ForwardLit: PBR lighting, shadows, fog, ambient GI
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                half3 ambient : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetBoidPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = positionWS;
                output.normalWS   = normalWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor  = ComputeFogFactor(output.positionCS.z);
                output.ambient    = SampleSHVertex(normalWS);

                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;
                half alpha = texColor.a * _BaseColor.a;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = alpha;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSHPixel(input.ambient, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }

            ENDHLSL
        }

        // Pass 2 — ShadowCaster: writes to the shadow map
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetBoidPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                // Apply shadow bias
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                // Clamp to near clip plane
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }

            ENDHLSL
        }

        // Pass 3 — DepthOnly: depth prepass for Depth Priming optimization
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetBoidPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }

            ENDHLSL
        }

        // Pass 4 — DepthNormals: encodes normals for SSAO
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetBoidPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS  = normalWS;
                return output;
            }

            float4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = normalize(input.normalWS);
                return float4(normalWS * 0.5 + 0.5, 0.0);
            }

            ENDHLSL
        }
    }
}
