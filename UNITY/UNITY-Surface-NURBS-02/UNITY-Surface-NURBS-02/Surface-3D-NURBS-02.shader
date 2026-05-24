Shader "Custom/Surface-3D-NURBS-02"
{
    Properties
    {
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
            
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _WaveSpeed;
            float _WaveAmp;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // --- BERNSTEIN POLYNOMIAL EVALUATION ---
            float Bernstein(int i, float t) {
                if (i == 0) return (1.0f - t) * (1.0f - t) * (1.0f - t);
                if (i == 1) return 3.0f * t * (1.0f - t) * (1.0f - t);
                if (i == 2) return 3.0f * t * t * (1.0f - t);
                if (i == 3) return t * t * t;
                return 0.0f;
            }

            // --- VERTEX SHADER ---
            v2f vert (appdata v)
            {
                v2f o;
                
                // Track parametric UV space directly from the vertex attributes
                float2 uv = v.uv;
                float4 sum4D = float4(0.0f, 0.0f, 0.0f, 0.0f);
                
                // Reconstruct the 4x4 math control grid inside the vertex pipeline
                for (int z = 0; z < 4; ++z) {
                    float bz = Bernstein(z, uv.y);
                    for (int x = 0; x < 4; ++x) {
                        float bx = Bernstein(x, uv.x);
                        
                        // Recalculate control grid position matching vs3.hlsl logic
                        float3 cpPos = float3((float)x * 1.5f - 2.25f, 0.0f, (float)z * 1.5f - 2.25f);
                        
                        // Assign the rational projective weights (Conic alignment)
                        float weight = 1.0f;
                        if ((x == 1 || x == 2) && (z == 1 || z == 2)) {
                            weight = 2.0f; 
                        } else if ((x == 0 || x == 3) && (z == 0 || z == 3)) {
                            weight = 0.7071f; 
                        }
                        
                        float4 controlPoint = float4(cpPos * weight, weight);
                        
                        // Apply wave modifier directly to internal Cartesian spaces
                        if (x > 0 && x < 3 && z > 0 && z < 3) {
                            float3 cartesianPos = controlPoint.xyz / controlPoint.w;
                            cartesianPos.y += sin(_Time.y * _WaveSpeed + (cartesianPos.x * 1.5f)) * _WaveAmp;
                            controlPoint.xyz = cartesianPos * controlPoint.w;
                        }
                        
                        // Blend 4D position
                        sum4D += controlPoint * bx * bz;
                    }
                }
                
                // --- RATIONAL PROJECTION DIVISION ---
                float3 final3D = sum4D.xyz / sum4D.w;
                
                // Transform your calculated 3D coordinates into camera space
                o.vertex = UnityObjectToClipPos(float4(final3D, 1.0f));
                o.uv = uv;
                
                return o;
            }

            // --- PIXEL SHADER ---
            float4 frag (v2f i) : SV_Target
            {
                // Pulling original pixel3.hlsl style color scheme
                float3 colorA = float3(0.0f, 0.8f, 0.9f); // Cyan
                float3 colorB = float3(0.6f, 0.1f, 0.8f); // Deep Purple
                
                float blendFactor = sin(i.uv.x * 3.1415f + _Time.y) * 0.5f + 0.5f;
                float3 finalColor = lerp(colorA, colorB, blendFactor * i.uv.y);
                
                // Screenspace anti-aliased diagnostic lines
                float2 grid = abs(frac(i.uv * 10.0f - 0.5f) - 0.5f) / fwidth(i.uv * 10.0f);
                float lineFactor = min(grid.x, grid.y);
                float gridPattern = 1.0f - min(lineFactor, 1.0f);
                
                finalColor += float3(0.2f, 0.4f, 0.5f) * gridPattern;
                return float4(finalColor, 1.0f);
            }
            ENDHLSL
        }
    }
}