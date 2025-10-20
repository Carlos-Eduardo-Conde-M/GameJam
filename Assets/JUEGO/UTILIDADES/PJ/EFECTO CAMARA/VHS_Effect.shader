Shader "Custom/NewSurfaceShader"
{
     Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Distortion ("Distortion", Range(0,0.1)) = 0.02
        _ScanIntensity ("Scanline Intensity", Range(0,1)) = 0.3
        _ChromaticAberration ("Chromatic Aberration", Range(0,0.01)) = 0.003
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float _Distortion;
            float _ScanIntensity;
            float _ChromaticAberration;
            float _NoiseIntensity;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // --- Distorsión horizontal (efecto wave)
                uv.x += sin(uv.y * 200.0 + _Time * 10.0) * _Distortion;

                // --- Aberración cromática (separar R/G/B ligeramente)
                float2 offset = float2(_ChromaticAberration, 0);
                fixed4 colR = tex2D(_MainTex, uv + offset);
                fixed4 colG = tex2D(_MainTex, uv);
                fixed4 colB = tex2D(_MainTex, uv - offset);
                fixed4 color = fixed4(colR.r, colG.g, colB.b, 1);

                // --- Ruido / grano animado
                float noise = tex2D(_NoiseTex, uv * 2 + _Time * 0.5).r;
                color.rgb += (noise - 0.5) * _NoiseIntensity;

                // --- Scanlines
                float scan = sin(uv.y * 800.0) * _ScanIntensity;
                color.rgb -= scan * 0.3;

                // --- Viñeta opcional
                float2 centered = uv - 0.5;
                float vignette = 1.0 - dot(centered, centered) * 1.5;
                color.rgb *= saturate(vignette);

                return saturate(color);
            }
            ENDCG
        }
    }
    
}
