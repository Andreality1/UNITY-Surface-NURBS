Shader "Custom/Sphere-INSTANCE-8"
{
    Properties
    {
        _Color ("Sphere Color", Color) = (0.0, 1.0, 0.5, 1.0) // Neon Emerald/Cyan
        _WaveSpeed ("Wave Speed", Float) = 2.5
        _WaveAmp ("Wave Amplitude", Float) = 0.75
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            HLSLPROGRAM
            #pragma target 5.0
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // Included to ensure structural layout alignment
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Maintain ID continuity through rasterization
            };

            float4 _Color;
            float _WaveSpeed;
            float _WaveAmp;

            v2f vert (appdata v)
            {
                v2f o;
                // Establish instancing data structures
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                float4 localPos = v.vertex;

                // Extract parent patch world position from the structural instancing array [cite: 32, 48]
                float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23;

                // Check bounds using local positions of the pre-baked mesh structure [cite: 68, 69]
                bool isInnerX = (localPos.x > -2.2f && localPos.x < 2.2f);
                bool isInnerZ = (localPos.z > -2.2f && localPos.z < 2.2f);

                if (isInnerX && isInnerZ)
                {
                    // Perfectly match your Bezier surface wave calculation [cite: 33, 70]
                    localPos.y += sin(_Time.y * _WaveSpeed + ((localPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp;
                }

                o.vertex = UnityObjectToClipPos(localPos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return _Color;
            }
            ENDHLSL
        }
    }
}