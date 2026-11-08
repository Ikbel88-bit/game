Shader "RoboClashXL/Atmosphere"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.03,0.08,0.28,1)
        _Horizon("Horizon", Color) = (0.78,0.52,0.40,1)
        _Ground("Ground", Color) = (0.05,0.045,0.05,1)
        _SunDir("Sun Direction", Vector) = (0.4,0.6,0.6,0)
        _SunColor("Sun Color", Color) = (1,0.85,0.6,1)
        _CloudColor("Cloud Color", Color) = (1,0.82,0.72,1)
        _CloudScale("Cloud Scale", Float) = 2.5
        _CloudSpeed("Cloud Speed", Float) = 0.01
        _Stars("Star Density", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Ground, _SunColor, _CloudColor;
                float4 _SunDir;
                float _CloudScale, _CloudSpeed, _Stars;
            CBUFFER_END

            struct A { float4 p : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.p.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.dir = ws - _WorldSpaceCameraPos;
                return o;
            }

            float h21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vn(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(h21(i), h21(i + float2(1,0)), f.x), lerp(h21(i + float2(0,1)), h21(i + float2(1,1)), f.x), f.y);
            }
            float fbm(float2 p)
            {
                float a = 0.5, s = 0;
                for (int k = 0; k < 5; k++) { s += a * vn(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sd = normalize(_SunDir.xyz);
                float y = d.y;

                float3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(y), 0.45));
                float3 below = lerp(_Horizon.rgb * 0.5, _Ground.rgb, saturate(-y * 3.0));
                col = lerp(col, below, saturate(-y * 30.0));

                float s = saturate(dot(d, sd));
                col += _SunColor.rgb * (pow(s, 1500) * 12.0 + pow(s, 40) * 0.5 + pow(s, 6) * 0.12);

                float night = saturate(-sd.y * 3.0 + 0.2);
                float2 sp = d.xz / (abs(y) + 0.3) * 220.0;
                col += step(0.9965, h21(floor(sp))) * night * step(0.0, y) * _Stars;

                if (y > 0.0)
                {
                    float2 cu = d.xz / (y + 0.12) * _CloudScale * 0.3 + float2(_Time.y * _CloudSpeed, 0);
                    float c = smoothstep(0.52, 0.78, fbm(cu)) * smoothstep(0.0, 0.2, y);
                    float lit = 0.55 + 0.45 * saturate(dot(d, sd) * 0.5 + 0.5);
                    col = lerp(col, _CloudColor.rgb * lit, c * 0.9);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
