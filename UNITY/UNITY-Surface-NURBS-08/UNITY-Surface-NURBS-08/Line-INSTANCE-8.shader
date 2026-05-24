Shader "Custom/Line-INSTANCE-8.shader"
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
                UNITY_SETUP_INSTANCE_ID(v);

                float4 localPos = v.vertex;
                
                // Get the instance world origin transformation matrix context
                float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23;

                // Replicate the boundary displacement constraint logic.
                // Because v.vertex matches local space coordinates, we can evaluate 
                // the animation based on its position offset relative to our known 4x4 matrix rules.
                float localX = frac((localPos.x + 2.25f) / 1.5f);
                float localZ = frac((localPos.z + 2.25f) / 1.5f);

                // Check if this specific vertex represents one of the 4 animating inner points
                // We use small epsilon bounds to handle floating point discrepancies cleanly
                bool isInnerX = (localPos.x > -2.2f && localPos.x < 2.2f);
                bool isInnerZ = (localPos.z > -2.2f && localPos.z < 2.2f);

                if (isInnerX && isInnerZ)
                {
                    // Animate the height exactly like your RationalBezierSurface implementation [cite: 33]
                    localPos.y += sin(_Time.y * _WaveSpeed + ((localPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp;
                }

                o.vertex = UnityObjectToClipPos(localPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _LineColor;
            }
            ENDCG
        }
    }
}