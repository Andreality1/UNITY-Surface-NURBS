Shader "Custom/Line-INSTANCE-13"
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

                // Clean, D3D11-safe extraction of the instance's world origin position
                float4 worldOrigin4 = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1));
                float3 worldOrigin = worldOrigin4.xyz;

                // Check if this specific vertex falls on the moving inner grid coordinates (-1.5 to 1.5)
                if (abs(localPos.x) < 1.5f)
                {
                    if (abs(localPos.z) < 1.5f)
                    {
                        // Wavy network behavior matches your patches perfectly
                        localPos.y += sin(_Time.y * _WaveSpeed + ((localPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp;
                    }
                }

                // 1. Transform the baseline vertex position to clip space
                float4 clipPos = UnityObjectToClipPos(localPos);

                // 2. Safely transform the segment direction to clip space step-by-step
                float3 normWorld = UnityObjectToWorldNormal(v.normal);

                // Extract view-projection matrix safely to avoid inline token issues
                float3x3 viewProj3x3 = (float3x3)UNITY_MATRIX_VP;
                float3 normClip = mul(viewProj3x3, normWorld);

                // 3. Compute screen space perpendicular vector for width expansion
                float2 screenNormal = normalize(float2(-normClip.y, normClip.x));

                // 4. Extrude outward perpendicular to segment line, accounting for aspect ratio correctness
                float2 offset = screenNormal * v.uv.x * _LineThickness * clipPos.w;
                offset.x /= (_ScreenParams.x / _ScreenParams.y); // Fix stretch asymmetry

                clipPos.xy += offset;

                o.vertex = clipPos;
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