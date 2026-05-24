Shader "Custom/Surface-3D-NURBS-03"
{
    Properties
    {
        _WaveSpeed ("Wave Speed", Float) = 2.5
        _WaveAmp ("Wave Amplitude", Float) = 0.75
        _TessFactor ("Tessellation Factor", Range(1, 64)) = 30.0
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
            // Target 4.6 is required for Tessellation support in DX11/Vulkan/Metal
            #pragma target 4.6
            
            #pragma vertex vert
            #pragma hull hs
            #pragma domain ds
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            float _WaveSpeed;
            float _WaveAmp;
            float _TessFactor;

            // Application to Vertex input
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Vertex to Hull communication
            struct v2h
            {
                float4 vertex : INTERNAL_VERTEX;
                float2 uv : TEXCOORD0;
            };

            // Hull Constant data output (Tessellation factors configuration)
            struct HS_CONSTANT_OUTPUT
            {
                float edges[4]  : SV_TessFactor;
                float inside[2] : SV_InsideTessFactor;
            };

            // Domain to Fragment shader
            struct d2f
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
            // Simply passes control patch data raw down into the tessellation control unit
            v2h vert (appdata v)
            {
                v2h o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            // --- HULL CONSTANT SHADER ---
            // Configures how densely the GPU will dynamically subdivide this quad patch
            HS_CONSTANT_OUTPUT ConstantsHS(InputPatch<v2h, 4> patch, uint patchID : SV_PrimitiveID)
            {
                HS_CONSTANT_OUTPUT o;
                o.edges[0] = _TessFactor;
                o.edges[1] = _TessFactor;
                o.edges[2] = _TessFactor;
                o.edges[3] = _TessFactor;
                o.inside[0] = _TessFactor;
                o.inside[1] = _TessFactor;
                return o;
            }

            // --- HULL SHADER ---
            [domain("quad")]
            [partitioning("integer")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(4)]
            [patchconstantfunc("ConstantsHS")]
            v2h hs(InputPatch<v2h, 4> patch, uint id : SV_OutputControlPointID, uint patchID : SV_PrimitiveID)
            {
                return patch[id];
            }

            // --- DOMAIN SHADER ---
            // Evaluates the math at every generated micro-vertex coordinate over the parametric space
            [domain("quad")]
            d2f ds(HS_CONSTANT_OUTPUT input, float2 uvDomain : SV_DomainLocation, const OutputPatch<v2h, 4> patch)
            {
                d2f o;
                
                // CORRECTED BILINEAR INTERPOLATION FOR CLOCKWISE QUAD TOPOLOGY:
                // Bottom edge: lerp between patch[0] (BL) and patch[1] (BR)
                // Top edge:    lerp between patch[3] (TL) and patch[2] (TR)
                float2 uv = lerp(
                    lerp(patch[0].uv, patch[1].uv, uvDomain.x),
                    lerp(patch[3].uv, patch[2].uv, uvDomain.x),
                    uvDomain.y
                );

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
            float4 frag (d2f i) : SV_Target
            {
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