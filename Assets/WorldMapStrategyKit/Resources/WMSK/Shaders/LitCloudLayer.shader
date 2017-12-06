Shader "WMSK/Lit Cloud Layer" {
	Properties {
                _MainTex ("Emission (RGB)", 2D) = "white" {}
                _Specular ("Specular", Color) = (0, 0, 0, 0)
                _Smoothness ("Smoothness", Range(0,1)) = 0.0
         		_EmissionColor("Color", Color) = (1,1,1,1)
        }
   SubShader {
                Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
           		ZWrite Off
                LOD 200
         
                CGPROGRAM
                // Physically based Standard lighting model, and enable shadows on all light types
                #pragma surface surf StandardSpecular fullforwardshadows alpha
                
				// Use shader model 3.0 target, to get nicer looking lighting
                #pragma target 3.0
                sampler2D _MainTex;
                
                struct Input {
                        float2 uv_MainTex;
                };
                half _Smoothness;
                fixed4 _Specular;
                half4 _EmissionColor;
                
                void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
 
                    fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
                    o.Albedo = fixed3(0,0,0);
                    o.Smoothness = _Smoothness;
                    o.Specular = _Specular;
                    o.Emission = dot(c.rgb, _EmissionColor.rgb);
                    o.Alpha = c.a * _EmissionColor.a;
                }
                ENDCG
        }
        FallBack "Diffuse"
}