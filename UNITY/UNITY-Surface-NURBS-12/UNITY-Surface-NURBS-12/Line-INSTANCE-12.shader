Shader "Custom/Line-INSTANCE-12"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1.0, 0.6, 0.0, 1.0) // Neon Amber
        _WaveSpeed ("Wave Speed", Float) = 2.5
        _WaveAmp ("Wave Amplitude", Float) = 0.75
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma target 5.0
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _LineColor;
            float _WaveSpeed;
            float _WaveAmp;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); // [cite: 65]

                float4 localPos = v.vertex;
                float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23; // [cite: 65]

                // Check if this specific vertex falls on the moving inner grid coordinates (-0.75 or 0.75) [cite: 9]
                // Using an epsilon check ensures accurate execution across compilers
                bool isInnerX = (abs(localPos.x) < 1.5f);
                bool isInnerZ = (abs(localPos.z) < 1.5f);

                if (isInnerX && isInnerZ) // [cite: 70]
                {
                    // Matches the math engine pattern exactly [cite: 11, 50]
                    localPos.y += sin(_Time.y * _WaveSpeed + ((localPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp; // [cite: 70]
                }

                o.vertex = UnityObjectToClipPos(localPos); // [cite: 71]
                return o; // [cite: 72]
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _LineColor;
            }
            ENDCG
        }
    }
}