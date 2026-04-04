Shader "Unlit/DirectionalBillboard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Columns ("Columns", Integer) = 4
        _Rows ("Rows", Integer) = 2
        _TotalFrames ("Total Active Frames", Integer) = 8
        _ObjectRotation ("Object Rotation", Float) = 0
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
            #define PI 3.14159265

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
            int _Columns;
            int _Rows;
            int _TotalFrames;
            float _ObjectRotation;

            Interpolators vert(Meshdata v)
            {
                Interpolators o;

                // Find camera in local space and ignore the Y axis
                float3 lookDir = ObjSpaceViewDir(float4(0, 0, 0, 1));
                lookDir.y = 0;
                lookDir = normalize(lookDir);

                // Calculate the new rotation for vertex
                float3 upDir = float3(0, 1, 0);
                float3 rightDir = cross(lookDir, upDir);
                float3 rotatedVertex = (rightDir * v.vertex.x) + (upDir * v.vertex.y);
                o.vertex = UnityObjectToClipPos(rotatedVertex);

                // Calculate the angle at which the camera is looking at the object, as a normalize value [0, 1]
                float rad = atan2(lookDir.x, lookDir.z);
                float objectRad = _ObjectRotation * (PI / 180.0);
                rad -= objectRad;

                // Map the radians to a [0.0 to 1.0] circle percentage.
                float anglePercent = frac(rad / (2.0 * PI));

                // Get frameID from angle
                int frameID = floor((anglePercent * _TotalFrames) + 0.5) % _TotalFrames;

                // Find Grid Coordinates
                int frameCol = frameID % _Columns;
                int frameRow = frameID / _Columns;
                int invertedRow = (_Rows - 1) - frameRow;

                // Scale and offset UVs
                float2 baseUV = TRANSFORM_TEX(v.uv, _MainTex);
                baseUV.x = (baseUV.x + frameCol) / (float)_Columns;
                baseUV.y = (baseUV.y + invertedRow) / (float)_Rows;

                o.uv = baseUV;
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
