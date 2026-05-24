Shader "Custom/Sphere-INSTANCE-12"
{
    Properties
    {
        _Color ("Sphere Color", Color) = (1, 0.5, 0, 1)
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
                float2 uv2 : TEXCOORD1; // Reads the baked sphere anchor centers cleanly
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _Color;
            float _WaveSpeed;
            float _WaveAmp;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                float3 vertexPos = v.vertex.xyz;
                // Extract the true center anchor of this specific sphere instance
                float2 sphereAnchor = v.uv2; 

                // Check if the center anchor matches an inner control point (-0.75 or 0.75)
                if (abs(sphereAnchor.x) < 1.5f && abs(sphereAnchor.y) < 1.5f) 
                {
                    float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23; 
                    
                    // Crucial Fix: Evaluate wave equation using the rigid center position (sphereAnchor.x)
                    // instead of individual vertex coordinates to prevent shearing/gliding!
                    float waveY = sin(_Time.y * _WaveSpeed + ((sphereAnchor.x + worldOrigin.x) * 1.5f)) * _WaveAmp; 
                    
                    // Move the entire sphere up and down as a single solid unit
                    vertexPos.y += waveY;
                    
                    // Neon Emerald color layout
                    o.color = float4(0.0f, 1.0f, 0.5f, 1.0f);
                }
                else
                {
                    o.color = _Color;
                }
                
                o.vertex = UnityObjectToClipPos(float4(vertexPos, 1.0f)); 
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}