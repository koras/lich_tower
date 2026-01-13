Shader "Custom/SpineOutline2DUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThicknessPx ("Outline Thickness (px)", Range(0,8)) = 1
        _OutlineEnabled ("Outline Enabled", Range(0,1)) = 1

        // 1 = PMA, 0 = straight alpha
        _UsePMA ("Use PMA", Range(0,1)) = 1
        // 0 = 4 taps (cheaper), 1 = 8 taps (nicer)
        _Quality8Taps ("Quality 8 taps", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _OutlineColor;
                float _OutlineThicknessPx;
                float _OutlineEnabled;
                float _UsePMA;
                float _Quality8Taps;
            CBUFFER_END
            float4 _MainTex_TexelSize;

            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float SampleAlpha(float2 uv)
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return c.a;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 baseCol = tex * IN.color;
 
                // Spine часто живёт в PMA: rgb уже умножен на alpha.
                // Мы рисуем в PMA-friendly бленде (One, OneMinusSrcAlpha).
                // Если у тебя straight alpha, ставь _UsePMA=0 и мы поправим базу.
                if (_UsePMA < 0.5)
                {
                    // преобразуем straight->pma на лету (чтобы бленд был корректный)
                    baseCol.rgb *= baseCol.a;
                }

                float a = baseCol.a;
                if (_OutlineEnabled < 0.5)
                    return baseCol;

                // Размер пикселя текстуры (texel size)
                // _MainTex_TexelSize: x=1/width, y=1/height, z=width, w=height
                float2 texel = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);
                float2 stepUV = texel * _OutlineThicknessPx;

                // Если текущий пиксель непрозрачный, рисуем базу без обводки
                // (обводка нужна на внешней границе, в прозрачной области).
                if (a > 0.001)
                    return baseCol;

                // Ищем рядом альфу (контур)
                float n = 0.0;
                n = max(n, SampleAlpha(IN.uv + float2( stepUV.x, 0)));
                n = max(n, SampleAlpha(IN.uv + float2(-stepUV.x, 0)));
                n = max(n, SampleAlpha(IN.uv + float2(0,  stepUV.y)));
                n = max(n, SampleAlpha(IN.uv + float2(0, -stepUV.y)));

                if (_Quality8Taps > 0.5)
                {
                    n = max(n, SampleAlpha(IN.uv + float2( stepUV.x,  stepUV.y)));
                    n = max(n, SampleAlpha(IN.uv + float2(-stepUV.x,  stepUV.y)));
                    n = max(n, SampleAlpha(IN.uv + float2( stepUV.x, -stepUV.y)));
                    n = max(n, SampleAlpha(IN.uv + float2(-stepUV.x, -stepUV.y)));
                }

                // n > 0 значит рядом есть тело спрайта, значит мы на границе.
                float outlineMask = step(0.001, n);

                float4 o = _OutlineColor;
                // приводим outline к PMA, чтобы бленд был красивый
                o.rgb *= o.a;

                // Можно ещё умножить на IN.color.a если хочешь, чтобы outline исчезал вместе с фейдом слота
                // outlineMask *= IN.color.a;

                return o * outlineMask;
            }
            ENDHLSL
        }
    }
}
