Shader "Wogua/ProceduralFireParticle"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (1, 0.92, 0.36, 1)
        _MiddleColor ("Middle Color", Color) = (1, 0.28, 0.015, 1)
        _TipColor ("Tip Color", Color) = (0.35, 0.015, 0.005, 1)
        _Distortion ("Distortion", Range(0, 0.35)) = 0.16
        _Softness ("Edge Softness", Range(0.01, 0.3)) = 0.09
        _Brightness ("Brightness", Range(0.5, 3)) = 1.35
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ProceduralFire"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                half4 _MiddleColor;
                half4 _TipColor;
                half _Distortion;
                half _Softness;
                half _Brightness;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                    lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x),
                    f.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.55;
                float2x2 rotation = float2x2(0.8, -0.6, 0.6, 0.8);
                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = mul(rotation, p * 2.03) + 11.7;
                    amplitude *= 0.5;
                }
                return value;
            }

            float FlameTongue(
                float2 samplePosition,
                float center,
                float height,
                float halfWidth,
                float phase,
                float edgeSoftness)
            {
                float normalizedY = samplePosition.y / max(height, 0.001);
                float verticalFade = saturate(1.0 - normalizedY);
                float sway = sin(samplePosition.y * 8.5 - _Time.y * 3.0 + phase) *
                    (0.018 + samplePosition.y * 0.045);
                float highFrequencySway = sin(samplePosition.y * 17.0 + _Time.y * 2.15 + phase * 1.7) *
                    samplePosition.y * 0.012;
                float taperedWidth = halfWidth * pow(verticalFade, 0.58) + 0.012;
                float horizontal = abs(samplePosition.x - center - sway - highFrequencySway) / taperedWidth;
                float shape = horizontal * horizontal + pow(max(normalizedY, 0.0), 1.55);
                return 1.0 - smoothstep(1.0 - edgeSoftness, 1.0 + edgeSoftness, shape);
            }

            float FireShape(float2 samplePosition, float scale, float verticalScale, float edgeSoftness)
            {
                samplePosition.x /= max(scale, 0.001);
                samplePosition.y /= max(verticalScale, 0.001);

                float baseShape = pow(samplePosition.x / 0.5, 2.0) + pow((samplePosition.y - 0.11) / 0.2, 2.0);
                float baseMask = 1.0 - smoothstep(0.92, 1.12, baseShape);
                float left = FlameTongue(samplePosition, -0.29, 0.64, 0.27, 1.6, edgeSoftness);
                float center = FlameTongue(samplePosition, 0.015, 1.0, 0.31, 0.1, edgeSoftness);
                float right = FlameTongue(samplePosition, 0.29, 0.72, 0.25, 3.8, edgeSoftness);
                return max(baseMask, max(center, max(left, right)));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.worldXY = positions.positionWS.xy;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float y = saturate(input.uv.y);
                float centeredX = input.uv.x - 0.5;
                float time = _Time.y;

                float tipWeight = smoothstep(0.06, 0.96, y);
                float globalSway = (
                    sin(time * 2.15) * 0.07 +
                    sin(time * 3.75 + 1.3) * 0.025) * tipWeight;
                float2 noiseUv = float2(centeredX * 4.2, y * 4.1 - time * 2.45);
                float coarseNoise = Fbm(noiseUv);
                float detailNoise = Fbm(noiseUv * 1.9 + float2(coarseNoise * 1.8, -time * 1.1));
                float risingFlow = Fbm(float2(
                    (centeredX + globalSway) * 6.4 + coarseNoise * 0.8,
                    y * 8.2 - time * 4.35));
                float2 flamePoint = float2(
                    centeredX + globalSway + (coarseNoise - 0.5) * _Distortion * (0.35 + y * 0.9),
                    y + (detailNoise - 0.5) * 0.045 * y + (risingFlow - 0.5) * 0.025 * tipWeight);

                float outer = FireShape(flamePoint, 1.0, 0.96, 0.075);
                float glow = FireShape(flamePoint, 1.07, 0.99, 0.16);

                float2 middlePoint = flamePoint;
                middlePoint.x += 0.015 + sin(time * 2.4 + y * 7.0) * y * 0.014;
                middlePoint.y += 0.035;
                float middle = FireShape(middlePoint, 0.68, 0.69, 0.07);

                float2 corePoint = flamePoint;
                corePoint.x -= 0.025 + sin(time * 2.8 + y * 9.0) * y * 0.01;
                corePoint.y += 0.07;
                float core = FireShape(corePoint, 0.39, 0.43, 0.065);

                float basePulse = 0.5 + 0.5 * sin(time * 7.0 + centeredX * 13.0);
                float hotBaseShape = pow(centeredX / 0.34, 2.0) + pow((y - 0.075) / 0.14, 2.0);
                float hotBase = (1.0 - smoothstep(0.72, 1.08, hotBaseShape)) *
                    lerp(0.86, 1.0, basePulse);
                core = max(core, hotBase);

                float bottomFade = smoothstep(0.0, 0.022, y);
                float alpha = max(outer, glow * 0.23) * bottomFade * input.color.a;
                clip(alpha - 0.006);

                half3 outerBottom = half3(1.0, 0.24, 0.008);
                half3 outerTip = half3(0.48, 0.012, 0.002);
                half3 outerColor = lerp(outerBottom, outerTip, smoothstep(0.18, 0.98, y));
                half3 middleBottom = half3(1.0, 0.91, 0.22);
                half3 middleTip = half3(1.0, 0.22, 0.006);
                half3 middleColor = lerp(middleBottom, middleTip, smoothstep(0.08, 0.73, y));
                half3 coreBottom = half3(1.0, 1.0, 0.9);
                half3 coreTip = half3(1.0, 0.67, 0.08);
                half3 coreColor = lerp(coreBottom, coreTip, smoothstep(0.04, 0.46, y));

                half3 fireColor = outerColor;
                fireColor = lerp(fireColor, middleColor, saturate(middle * 1.15));
                fireColor = lerp(fireColor, coreColor, saturate(core * 1.2));
                float risingRibbon = smoothstep(0.5, 0.82, risingFlow) * outer *
                    (1.0 - smoothstep(0.5, 0.98, y));
                fireColor = lerp(fireColor, half3(1.0, 0.62, 0.045), risingRibbon * 0.34);
                float edgeHeat = saturate((glow - outer) * 2.2);
                fireColor += half3(0.45, 0.035, 0.0) * edgeHeat;
                float flicker = lerp(0.9, 1.08, detailNoise);
                fireColor *= flicker * _Brightness * input.color.rgb;
                alpha = max(alpha, core * 0.98 * bottomFade);
                return half4(fireColor, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
