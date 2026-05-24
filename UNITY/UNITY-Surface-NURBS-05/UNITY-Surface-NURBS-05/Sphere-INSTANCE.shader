Shader "Custom/Sphere-INSTANCE"
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

            float4 _Color;
            float _WaveSpeed;
            float _WaveAmp;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                // Extract the true world position of this specific sphere instance from the TRS matrix
                float3 sphereWorldPos = UNITY_MATRIX_M._m03_m13_m23;
                
                float4 localPos = v.vertex;

                // To find out if this sphere is an interior moving control point,
                // we reconstruct where it sits relative to its parent patch grid spacing (4.5)
                // We add an epsilon offset to handle rounding issues cleanly.
                float patchLocalX = frac((sphereWorldPos.x + 2.25f + 0.005f) / 4.5f) * 4.5f;
                float patchLocalZ = frac((sphereWorldPos.z + 2.25f + 0.005f) / 4.5f) * 4.5f;

                // In a local 4.5x4.5 patch, the 4 internal control points sit at x,z = 1.5 and 3.0
                // We check if this sphere is one of those internal points:
                bool isInnerX = (patchLocalX > 1.4f && patchLocalX < 3.1f);
                bool isInnerZ = (patchLocalZ > 1.4f && patchLocalZ < 3.1f);

                if (isInnerX && isInnerZ)
                {
                    // Match the wave equation from your main surface shader exactly!
                    localPos.y += sin(_Time.y * _WaveSpeed + (sphereWorldPos.x * 1.5f)) * _WaveAmp;
                }

                // Transform cleanly from object space to clip space
                o.vertex = UnityObjectToClipPos(localPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}