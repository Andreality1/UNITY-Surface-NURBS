Shader "Custom/Surface-3D-NURBS-08"
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
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _Color;
            float _WaveSpeed;
            float _WaveAmp;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // Get the true world translation position of this specific individual sphere
                float3 sphereWorldPos = UNITY_MATRIX_M._m03_m13_m23;
                float4 localPos = v.vertex;

                // Reconstruct exactly where this sphere sits relative to the 4.5 spacing rules of your grid system.
                // We add an epsilon offset (0.005f) to handle precision rounding safely.
                float patchLocalX = frac((sphereWorldPos.x + 2.25f + 0.005f) / 4.5f) * 4.5f;
                float patchLocalZ = frac((sphereWorldPos.z + 2.25f + 0.005f) / 4.5f) * 4.5f;

                // In a local 4.5x4.5 patch framework, your 4 internal animating control points sit precisely at 1.5 and 3.0
                bool isInnerX = (patchLocalX > 1.4f && patchLocalX < 3.1f);
                bool isInnerZ = (patchLocalZ > 1.4f && patchLocalZ < 3.1f);

                if (isInnerX && isInnerZ)
                {
                    // Perfectly synchronizes with your surface vertex displacement equation
                    localPos.y += sin(_Time.y * _WaveSpeed + (sphereWorldPos.x * 1.5f)) * _WaveAmp;
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