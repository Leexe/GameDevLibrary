Shader "Custom/GrassInstance"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _MainTex ("Texture", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _WindSpeed ("Wind Speed", Float) = 1
        _WindStrength ("Wind Strength", Float) = 1
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _TopColor;
        float4 _BottomColor;
        float4 _MainTex_ST;
        float _Cutoff;
        float _Metallic;
        float _Smoothness;
        int _InstanceOffset;
        float _WindSpeed;
        float _WindStrength;
    CBUFFER_END

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    struct GrassData
    {
        float3 position;
        float rotationY;
        float2 scale;
    };

    StructuredBuffer<GrassData> grassBuffer;

    void setup()
    {
    }

    void GetGrassPositionAndNormalWS(float4 positionOS, float3 normalOS, uint instanceID,
                                     out float3 positionWS, out float3 normalWS)
    {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

        GrassData grassData = grassBuffer[instanceID + _InstanceOffset];

        // Scale
        float3x3 scaleMatrix = float3x3(grassData.scale.x, 0, 0, 0, grassData.scale.y, 0, 0, 0, grassData.scale.x);
        positionWS = mul(scaleMatrix, positionOS.xyz);

        // Rotate
        float s, c;
        sincos(grassData.rotationY, s, c);
        float3x3 rotY = float3x3(c, 0, s, 0, 1, 0, -s, 0, c);
        positionWS = mul(rotY, positionWS);
        normalWS = mul(rotY, normalOS);

        // Offset
        positionWS = grassData.position + positionWS;
        normalWS = normalize(normalWS);

        // Wind
        float windTime = _WindSpeed * _Time.y;
        float2 windOffset = float2(sin(windTime + 0.5 * positionWS.x), cos(windTime * 0.8 + 0.5 * positionWS.z));
        float heightMap = saturate(positionOS.y);
        positionWS.xz += heightMap * windOffset * _WindStrength;

        #else

        positionWS = TransformObjectToWorld(positionOS.xyz);
        normalWS = TransformObjectToWorldNormal(normalOS);

        #endif
    }
    ENDHLSL


    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

        // Pass 1: PBR lighting, shadows, fog, ambient GI
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
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
                GetGrassPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.ambient = SampleSHVertex(normalWS);

                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 textColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y) * textColor.rgb;
                half alpha = lerp(_BottomColor.a, _TopColor.a, input.uv.y) * textColor.a;
                clip(alpha - _Cutoff);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = alpha;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                inputData.bakedGI = SampleSHPixel(input.ambient, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Pass 2: Writes to the shadow map
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

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
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetGrassPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                // Apply shadow bias
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                // Pass UV Data
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

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
                half alpha = lerp(_BottomColor.a, _TopColor.a, input.uv.y) * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Pass 3: Write to Depth Map
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Meshdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL; // Need normal for GetGrassPositionAndNormalWS
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetGrassPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = lerp(_BottomColor.a, _TopColor.a, input.uv.y) * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Pass 4: Encodes Normals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

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
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Interpolators vert(Meshdata input)
            {
                Interpolators output = (Interpolators)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS, normalWS;
                GetGrassPositionAndNormalWS(input.positionOS, input.normalOS, input.instanceID, positionWS, normalWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag(Interpolators input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = lerp(_BottomColor.a, _TopColor.a, input.uv.y) * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                float3 normalWS = normalize(input.normalWS);
                return float4(normalWS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
