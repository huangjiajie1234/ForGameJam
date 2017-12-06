Shader "WMSK/Lit Fog Of War" {
	Properties {
                _MainTex ("Base (RGB)", 2D) = "black" {}
                _NoiseTex ("Noise (RGB)", 2D) = "white" {}
                _Specular ("Specular", Color) = (0, 0, 0, 0)
                _Smoothness ("Smoothness", Range(0,1)) = 0.0
         		_EmissionColor("Color", Color) = (1,1,1,1)
        }
   SubShader {
                Tags { "Queue"="Transparent" "RenderType"="Transparent" }
                LOD 200
         
                CGPROGRAM
                // Physically based Standard lighting model, and enable shadows on all light types
                #pragma surface surf StandardSpecular fullforwardshadows alpha
                
				// Use shader model 3.0 target, to get nicer looking lighting
                #pragma target 3.0
                sampler2D _MainTex;
                sampler2D _NoiseTex;
                
                struct Input {
                	float2 uv_MainTex;
                };
                
                half _Smoothness;
                fixed4 _Specular;
                half4 _EmissionColor;
                
                void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
                	fixed fogAlpha = tex2D (_MainTex, IN.uv_MainTex).a;
                    half vxy = (IN.uv_MainTex.x + IN.uv_MainTex.y);
					half wt = _Time[1] * 0.5;
					half2 waveDisp1 = half2(wt + cos(wt+IN.uv_MainTex.y * 32.0) * 0.125, 0) * 0.05;
					fixed4 fog1 = tex2D(_NoiseTex, (IN.uv_MainTex + waveDisp1) * 8);
                    wt*=1.1;
					half2 waveDisp2 = half2(wt + cos(wt+IN.uv_MainTex.y * 8.0) * 0.5, 0) * 0.05;
					fixed4 fog2 = tex2D(_NoiseTex, (IN.uv_MainTex + waveDisp2) * 2);
                    fixed4 fog = (fog1 + fog2) * 0.5;
                    
                    o.Albedo = fixed3(0,0,0);
                    o.Smoothness = _Smoothness;
                    o.Specular = _Specular;
                    o.Emission = fog.rgb * _EmissionColor.rgb * fogAlpha;
                    o.Alpha = fogAlpha;
                }
                ENDCG
        }
        FallBack "Diffuse"
}