Shader "Unlit/CylindricalBillboard"
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

            Interpolators vert(Meshdata v)
            {
                Interpolators o;
                float3 lookDir = ObjSpaceViewDir(float4(0, 0, 0, 1));
                lookDir.y = 0;
                lookDir = normalize(lookDir);

                float3 upDir = float3(0, 1, 0);
                float3 rightDir = cross(lookDir, upDir);

                float3 rotatedVertex = (rightDir * v.vertex.x) + (upDir * v.vertex.y);

                o.vertex = UnityObjectToClipPos(rotatedVertex);
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
