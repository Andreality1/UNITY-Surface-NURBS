Shader "Custom/Line-INSTANCE-13~1"
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
                        
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float4 localPos = v.vertex;

                // 1. Clean, safe extraction of the instance's world origin position
                float4 worldOrigin4 = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1));
                float3 worldOrigin = worldOrigin4.xyz;

                // 2. Evaluate wave network behavior symmetrically (matches patches perfectly)
                if (abs(localPos.x) < 1.5f && abs(localPos.z) < 1.5f)
                {
                    localPos.y += sin(_Time.y * _WaveSpeed + ((localPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp;
                }

                // 3. Extrude the ribbon width in Object Space!
                // Since your mesh normals hold the segment direction vector (e.g., along X or Z axis),
                // we cross it with the upward vector (0, 1, 0) to find the perpendicular horizontal expansion side.
                float3 segmentDirection = v.normal;
                float3 localPerpendicular = cross(segmentDirection, float3(0.0f, 1.0f, 0.0f));

                // If a segment goes straight up or down, fallback to cross with forward
                if (length(localPerpendicular) < 0.001f)
                {
                    localPerpendicular = cross(segmentDirection, float3(0.0f, 0.0f, 1.0f));
                }
                localPerpendicular = normalize(localPerpendicular);

                // v.uv.x contains your side offset (-1.0 or 1.0) passed from the C# script
                // This physically widens the quad geometry in local space by your absolute thickness value
                localPos.xyz += localPerpendicular * v.uv.x * _LineThickness;

                // 4. Now hand the physically widened 3D quad over to the standard projection matrix.
                // The rasterizer will now scale it down with distance automatically!
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