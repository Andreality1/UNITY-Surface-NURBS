Shader "Custom/Surface-3D-NURBS-01"
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

            // --- VERTEX SHADER ---
            v2f vert (appdata v)
            {
                v2f o;
                
                // Directly mirror your native C++ matrix manipulation behavior
                // UnityObjectToClipPos multiplies MVP * position
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                return o;
            }

            // --- PIXEL SHADER ---
            float4 frag (v2f i) : SV_Target
            {
                float3 colorA = float3(0.0f, 0.8f, 0.9f); // Cyan
                float3 colorB = float3(0.6f, 0.1f, 0.8f); // Deep Purple
                
                float blendFactor = sin(i.uv.x * 3.1415f + _Time.y) * 0.5f + 0.5f;
                float3 finalColor = lerp(colorA, colorB, blendFactor * i.uv.y);
                
                // Classic screen-space derivative anti-aliased grid logic
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