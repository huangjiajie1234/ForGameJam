// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "WMSK/Unlit Country Frontiers Order 3" {
 
Properties {
    _Color ("Color", Color) = (0,1,0,1)
    _OuterColor("Outer Color", Color) = (0,0.8,0,0.8)
}
 
SubShader {
	LOD 300
    Tags {
        "Queue"="Geometry+300"
        "RenderType"="Opaque"
    	}
 	Blend SrcAlpha OneMinusSrcAlpha    	
    Pass {
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.z-=0.00001; //0.001; 
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
		
    }
}

SubShader {
	LOD 200
    Tags {
        "Queue"="Geometry+300"
        "RenderType"="Opaque"
    	}
    Blend SrcAlpha OneMinusSrcAlpha
    
    Pass { // (+1,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor, _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x += 1.25 * (v.vertex.w/_ScreenParams.x);
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color * 0.5 + _OuterColor * 0.5;					
		}
		ENDCG
		
    }
   Pass { // (-1,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor, _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x -= 1.25 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color * 0.5 + _OuterColor * 0.5;					
		}
		ENDCG
		
    }    
  Pass { // (0,-1)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor, _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y -= 1.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color * 0.5 + _OuterColor * 0.5;					
		}
		ENDCG
    } 
           
    Pass { // (0, +1)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor, _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y += 1.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color * 0.5 + _OuterColor * 0.5;					
		}
		ENDCG
    }    
    
     Pass {
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.z-=0.00002; //0.002; 
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
		
    }  
}

SubShader {
	LOD 100
    Tags {
        "Queue"="Geometry+300"
        "RenderType"="Opaque"
    	}
    Blend SrcAlpha OneMinusSrcAlpha

	// right wing ---------------------------------
  
     Pass { // (+3,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x += 4.25 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    }
            
    Pass { // (+2,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x += 2.75 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    }
    
    // left wing ---------------------------------
    
 
     Pass { // (-3,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x -= 4.25 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    }
    
    
    Pass { // (-2,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x -= 2.75 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
		
    }     
    
    
    // top wing ---------------------------------------

                           
	Pass { // (0,+3)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y += 4.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    } 
                                                      
    Pass { // (0,+2)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y += 2.75 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
		
    } 
    
    
    // bottom wing ---------------------------------------
                            
	Pass { // (0,-3)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y -= 4.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    } 
        
  Pass { // (0,-2)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _OuterColor;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y -= 2.75 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _OuterColor;					
		}
		ENDCG
    }            
       
      // central kernel ---------------------------------------
        
    Pass { // (+1,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x += 1.25 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
		
    }
    
    
   Pass { // (-1,0)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.x -= 1.25 * (v.vertex.w/_ScreenParams.x);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;			
		}
		ENDCG
		
    }   
    
     
       
  Pass { // (0,-1)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
 			v.vertex.y -= 1.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
    } 
 
                                
     Pass { // (0,+1)
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.y += 1.25 * (v.vertex.w/_ScreenParams.y);		
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
    }    

    
    Pass {
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.vertex.z-=0.00002; //0.002; 
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
		ENDCG
		
    }  
    
}
 
}
