Shader "Custom/Sphere-INSTANCE-14"
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

            
            // Global 3D Displacement Function (Exactly matches the patch shader)
            float3 ApplyStrongDeformation(float3 localPos, float3 worldOrigin, float waveSpeed, float waveAmp)
            {
                float3 globalCoords = localPos + worldOrigin;
                float timeFactor = _Time.y * waveSpeed;
                
                // Y Axis (Sharp Ridge Networks)
                float waveY1 = sin(timeFactor + globalCoords.x * 0.8f) * cos(timeFactor + globalCoords.z * 0.8f);
                float waveY2 = 1.0f - abs(sin(timeFactor * 1.5f + (globalCoords.x + globalCoords.z) * 1.2f));
                float deltaY = (waveY1 * 0.4f + waveY2 * 0.6f) * waveAmp * 2.5f; 

                // X & Z Axis (Swirling / Horizontal push)
                float deltaX = sin(timeFactor * 1.1f + globalCoords.z * 1.5f) * waveAmp * 0.6f;
                float deltaZ = cos(timeFactor * 1.3f + globalCoords.x * 1.5f) * waveAmp * 0.6f;
                
                return localPos + float3(deltaX, deltaY, deltaZ);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                float3 vertexPos = v.vertex.xyz;
                
                // Extract the baked 2D grid anchor coordinates for this sphere instance center
                float2 sphereAnchor = v.uv2; 
                float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23; 

                // 1. Reconstruct the base local position of the control point anchor
                float3 anchorPosLocal = float3(sphereAnchor.x, 0.0f, sphereAnchor.y);
                
                // 2. Evaluate the 3D deformation at the anchor's resting coordinate
                float3 deformedAnchor = ApplyStrongDeformation(anchorPosLocal, worldOrigin, _WaveSpeed, _WaveAmp);
                
                // 3. Find the translation offset (how far the anchor moved from its resting position)
                float3 anchorDisplacement = deformedAnchor - anchorPosLocal;
                
                // 4. Translate the sphere's local vertices by that offset to keep the sphere completely solid
                vertexPos += anchorDisplacement;
                
                // Pure color layout configurations
                if (abs(sphereAnchor.x) < 1.5f && abs(sphereAnchor.y) < 1.5f) 
                {
                    o.color = float4(0.0f, 1.0f, 0.5f, 1.0f); // Neon Emerald for active internal nodes
                }
                else
                {
                    o.color = _Color; // Perimeter nodes
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