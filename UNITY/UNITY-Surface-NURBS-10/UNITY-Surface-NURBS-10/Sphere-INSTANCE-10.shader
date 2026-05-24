Shader "Custom/Sphere-INSTANCE-10"
{
    Properties
    {
        _Color ("Sphere Color", Color) = (1, 0.5, 0, 1)
        _SphereScale ("Visual Scale", Float) = 0.15
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

            HLSLPROGRAM
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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Required to pass tracking data through cleanly
            };

            float4 _Color;
            float _SphereScale;
            float _WaveSpeed;
            float _WaveAmp;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(int, _ControlPointID)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. Initialize instancing structures
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // 2. Explicitly declare variables before reading the macro to satisfy the D3D11 compiler
                int cpID = 0;
                int cx = 0;
                int cz = 0;

                // 3. Extract the ID safely from the instanced property batch
                cpID = UNITY_ACCESS_INSTANCED_PROP(Props, _ControlPointID);
                
                // 4. Unpack into 2D grid coordinates
                cx = cpID % 4;
                cz = cpID / 4;

                // Match your base patch layout math exactly 
                float3 cpPos = float3((float)cx * 1.5f - 2.25f, 0.0f, (float)cz * 1.5f - 2.25f);

                // Wave animation logic matching the patch domain shader 
                if (cx > 0 && cx < 3 && cz > 0 && cz < 3) 
                {
                    float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23; // 
                    cpPos.y += sin(_Time.y * _WaveSpeed + ((cpPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp; // 
                }

                // Offset the sphere geometry's local vertices by the control point location
                float3 finalLocalPos = cpPos + (v.vertex.xyz * _SphereScale);
                
                o.vertex = UnityObjectToClipPos(float4(finalLocalPos, 1.0f)); // 
                
                // Color coding: Inner control points turn green, border points stay amber
                o.color = _Color;
                if ((cx == 1 || cx == 2) && (cz == 1 || cz == 2)) {
                    o.color = float4(0.0f, 1.0f, 0.5f, 1.0f); 
                }

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // Safe context layout tracking
                return i.color;
            }
            ENDHLSL
        }
    }
}