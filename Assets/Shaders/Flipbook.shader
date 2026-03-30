Shader "Unlit/FlipbookSprite"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        [HDR] _Color ("Tint Color", Color) = (1,1,1,1)
        _Columns ("Columns", Float) = 1
        _Rows ("Rows", Float) = 1
        _AnimationSpeed ("Animation Speed (FPS)", Float) = 10
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Columns;
            float _Rows;
            float _AnimationSpeed;

            // Instanced properties, for per-bullet variations
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceAlpha)
            UNITY_INSTANCING_BUFFER_END(Props)

            Interpolators vert(MeshData v)
            {
                Interpolators o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(Interpolators i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float cols = max(1.0, _Columns);
                float rows = max(1.0, _Rows);
                float frames = cols * rows;

                // Calculate current frame based on elapsed game time
                float timeSecs = _Time.y;
                float currentFrame = floor(fmod(timeSecs * _AnimationSpeed, frames));

                // Calculate X and Y indices for the sprite sheet
                float frameX = fmod(currentFrame, cols);
                float frameY = rows - 1.0 - floor(currentFrame / cols);

                // Scale UVs according to columns and rows
                float2 size = float2(1.0 / cols, 1.0 / rows);
                float2 uv = i.uv * size + float2(frameX, frameY) * size;

                // Sample texture with tint
                fixed4 col = tex2D(_MainTex, uv) * _Color;

                // Apply per-instance alpha
                col.a *= UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceAlpha);

                // Clip Low Alpha Pixels
                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}
