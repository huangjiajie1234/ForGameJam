Shader "WMSK/Unlit Single Texture"{

Properties { _MainTex ("Texture", 2D) = "" }
SubShader {
	    Tags {
        "Queue"="Geometry"
        "RenderType"="Opaque"
    }
    Offset 2,2
	Pass {
		SetTexture[_MainTex]
		} 
	}
}