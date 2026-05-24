Shader "Custom/Line-INSTANCE-14"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1.0, 0.6, 0.0, 1.0) // Neon Amber
        _LineThickness ("Line Thickness", Range(0.0, 0.1)) = 0.02
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
                float3 normal : NORMAL;   // Holds segment direction vector passed from mesh
                float2 uv     : TEXCOORD0; // x = side offset (-1 or 1), y = line interpolation factor
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _LineColor;
            float _LineThickness;
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

                float3 localPos = v.vertex.xyz;
                float3 worldOrigin = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;

                // 1. Calculate the base endpoint position of the line segment
                float3 baselinePoint = localPos;
                
                // 2. Find where an adjacent reference point along the line direction would sit
                float3 segmentDirRaw = v.normal;
                float3 neighborPoint = baselinePoint + segmentDirRaw * 0.1f;

                // 3. Apply the strong 3D deformation to BOTH points independently
                float3 deformedBaseline = ApplyStrongDeformation(baselinePoint, worldOrigin, _WaveSpeed, _WaveAmp);
                float3 deformedNeighbor = ApplyStrongDeformation(neighborPoint, worldOrigin, _WaveSpeed, _WaveAmp);

                // 4. Compute the dynamic 3D direction vector of the line after stretching/bending
                float3 dynamicSegmentDir = normalize(deformedNeighbor - deformedBaseline);

                // 5. Compute the ribbon's side-expansion vector perpendicular to the new 3D path
                float3 localPerpendicular = cross(dynamicSegmentDir, float3(0.0f, 1.0f, 0.0f));
                if (length(localPerpendicular) < 0.001f)
                {
                    localPerpendicular = cross(dynamicSegmentDir, float3(0.0f, 0.0f, 1.0f));
                }
                localPerpendicular = normalize(localPerpendicular);

                // 6. Extrude outwards based on side index sign (v.uv.x is -1.0 or 1.0)
                float3 finalPosition = deformedBaseline + localPerpendicular * v.uv.x * _LineThickness;

                o.vertex = UnityObjectToClipPos(float4(finalPosition, 1.0f));
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