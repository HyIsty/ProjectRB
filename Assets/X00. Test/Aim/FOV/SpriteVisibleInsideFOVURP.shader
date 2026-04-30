Shader "Custom/SpriteVisibleInsideFOVURP"
{
    Properties
    {
        // 적 스프라이트 텍스처
        _MainTex("Sprite Texture", 2D) = "white" {}

        // SpriteRenderer의 색상 tint를 받을 때 사용
        _Color("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Pass
        {
            Name "SpriteVisibleInsideFOV"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            // 핵심:
            // stencil 값이 1인 곳(FOV 내부)에서만 이 스프라이트를 렌더링한다.
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;     // SpriteRenderer 색상값
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 오브젝트 공간 -> 클립 공간
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // UV 전달
                output.uv = input.uv;

                // SpriteRenderer vertex color와 material tint 결합
                output.color = input.color * _Color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * input.color;

                // 투명 픽셀 정리
                clip(finalColor.a - 0.001h);

                return finalColor;
            }
            ENDHLSL
        }
    }
}