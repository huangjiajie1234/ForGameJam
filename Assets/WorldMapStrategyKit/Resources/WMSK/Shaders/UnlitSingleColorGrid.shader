Shader "WMSK/Unlit Single Color Grid" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Color [_Color]
        Tags {
        "Queue"="Geometry+260"
        "RenderType"="Opaque"
    }
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
    }
}
 
}
