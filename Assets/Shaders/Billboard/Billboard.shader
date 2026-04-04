Shader "Unlit/Billboard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "DisableBatching" = "True"
            "IgnoreProjector" = "True"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Meshdata
            {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _MainTex_ST;

            const float3 vect3Zero = float3(0.0, 0.0, 0.0);

            Interpolators vert(Meshdata v)
            {
                Interpolators o;
                float4 centerViewPos = float4(UnityObjectToViewPos(vect3Zero).xyz, 1.0);
                float4 flatVertexOffset = float4(v.vertex.x, v.vertex.y, 0.0, 0.0);
                float4 clipPos = mul(UNITY_MATRIX_P, centerViewPos + flatVertexOffset);

                o.vertex = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(Interpolators i) : SV_Target
            {
                fixed4 col = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
