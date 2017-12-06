Shader "WMSK/Unlit Surface Single Color Alpha" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Tags {
        "Queue"="Geometry+1"
        "RenderType"="Transparent"
    	}
    Color [_Color]
    Offset 1,1
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
    }
}
 
}
