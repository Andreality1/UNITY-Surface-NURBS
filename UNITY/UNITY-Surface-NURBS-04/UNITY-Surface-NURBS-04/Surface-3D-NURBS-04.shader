Shader "Custom/Surface-3D-NURBS-04"
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
            #pragma target 5.0
            
            #pragma vertex vert
            #pragma hull hs
            #pragma domain ds
            #pragma fragment frag
            
            // 1. Enable Instancing variants
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            float _WaveSpeed;
            float _WaveAmp;
            float _TessFactor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 2. Add Instance ID to input
            };

            struct v2h
            {
                float4 vertex : INTERNAL_VERTEX;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 3. Pass Instance ID to Hull
            };

            struct HS_CONSTANT_OUTPUT
            {
                float edges[4]  : SV_TessFactor;
                float inside[2] : SV_InsideTessFactor;
            };

            struct d2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Bernstein(int i, float t) {
                if (i == 0) return (1.0f - t) * (1.0f - t) * (1.0f - t);
                if (i == 1) return 3.0f * t * (1.0f - t) * (1.0f - t);
                if (i == 2) return 3.0f * t * t * (1.0f - t);
                if (i == 3) return t * t * t;
                return 0.0f;
            }

            v2h vert (appdata v)
            {
                v2h o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // 4. Transfer ID
                
                // Note: We leave v.vertex in local space. Unity's instancing
                // will automatically supply the correct world matrix in UnityObjectToClipPos
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

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

            [domain("quad")]
            [partitioning("integer")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(4)]
            [patchconstantfunc("ConstantsHS")]
            v2h hs(InputPatch<v2h, 4> patch, uint id : SV_OutputControlPointID, uint patchID : SV_PrimitiveID)
            {
                return patch[id];
            }

            [domain("quad")]
            d2f ds(HS_CONSTANT_OUTPUT input, float2 uvDomain : SV_DomainLocation, const OutputPatch<v2h, 4> patch)
            {
                d2f o;
                
                // 5. Setup instance ID in the Domain shader so UnityObjectToClipPos knows which instance matrix to use
                UNITY_SETUP_INSTANCE_ID(patch[0]); 

                float2 uv = lerp(
                    lerp(patch[0].uv, patch[1].uv, uvDomain.x),
                    lerp(patch[3].uv, patch[2].uv, uvDomain.x),
                    uvDomain.y
                );

                float4 sum4D = float4(0.0f, 0.0f, 0.0f, 0.0f);
                for (int z = 0; z < 4; ++z) {
                    float bz = Bernstein(z, uv.y);
                    for (int x = 0; x < 4; ++x) {
                        float bx = Bernstein(x, uv.x);
                        float3 cpPos = float3((float)x * 1.5f - 2.25f, 0.0f, (float)z * 1.5f - 2.25f);
                        
                        float weight = 1.0f;
                        if ((x == 1 || x == 2) && (z == 1 || z == 2)) {
                            weight = 2.0f;
                        } else if ((x == 0 || x == 3) && (z == 0 || z == 3)) {
                            weight = 0.7071f;
                        }
                        
                        float4 controlPoint = float4(cpPos * weight, weight);
                        
                        if (x > 0 && x < 3 && z > 0 && z < 3) {
                            float3 cartesianPos = controlPoint.xyz / controlPoint.w;
                            
                            // Adding instance-based variance using the patch's object position (optional)
                            float3 worldOrigin = UNITY_MATRIX_M._m03_m13_m23; 
                            cartesianPos.y += sin(_Time.y * _WaveSpeed + ((cartesianPos.x + worldOrigin.x) * 1.5f)) * _WaveAmp;
                            
                            controlPoint.xyz = cartesianPos * controlPoint.w;
                        }
                        sum4D += controlPoint * bx * bz;
                    }
                }
                
                float3 final3D = sum4D.xyz / sum4D.w;
                
                // UnityObjectToClipPos now accounts for the specific instance's translation/rotation/scale!
                o.vertex = UnityObjectToClipPos(float4(final3D, 1.0f));
                o.uv = uv;
                
                return o;
            }

            float4 frag (d2f i) : SV_Target
            {
                float3 colorA = float3(0.0f, 0.8f, 0.9f);
                float3 colorB = float3(0.6f, 0.1f, 0.8f);
                float blendFactor = sin(i.uv.x * 3.1415f + _Time.y) * 0.5f + 0.5f;
                float3 finalColor = lerp(colorA, colorB, blendFactor * i.uv.y);
                
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