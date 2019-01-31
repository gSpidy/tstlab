// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Blur transparent simple Fast" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
	    _Size("Size", Range(0, 20)) = 1
	}

		Category{

		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }


		SubShader{

		GrabPass{
		Tags{ "LightMode" = "Always" }
	}
		Pass{
		Tags{ "LightMode" = "Always" }		

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord: TEXCOORD0;
		fixed4 color : COLOR;
	};

	struct v2f {
		float4 vertex : POSITION;
		float4 uvgrab : TEXCOORD0;
		fixed4 diff : COLOR0;
	};

    
	fixed4 _Color;

	v2f vert(appdata_t v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
#if UNITY_UV_STARTS_AT_TOP
		float scale = -1.0;
#else
		float scale = 1.0;
#endif
		o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
		o.uvgrab.zw = o.vertex.zw;
		o.diff =  v.color*_Color;
		return o;
	}
	
	sampler2D _GrabTexture;
	float4 _GrabTexture_TexelSize;
	float _Size;

	half4 frag(v2f i) : COLOR{

		half4 sum = half4(0,0,0,0);
#define GRABPIXEL(weight,kernelx) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x + _GrabTexture_TexelSize.x * i.diff.a * kernelx*_Size, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight
		       
		sum += GRABPIXEL(0.096, -3.0);		
		sum += GRABPIXEL(0.231, -1.5);		
		sum += GRABPIXEL(0.346,  0.0);		
		sum += GRABPIXEL(0.231, +1.5);		
		sum += GRABPIXEL(0.096, +3.0);

		return sum;
	}
		ENDCG
	}

		GrabPass{
		Tags{ "LightMode" = "Always" }
	}
		Pass{
		Tags{ "LightMode" = "Always" }
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord: TEXCOORD0;
		fixed4 color : COLOR;
	};

	struct v2f {
		float4 vertex : POSITION;
		float4 uvgrab : TEXCOORD0;
		fixed4 diff : COLOR0;
	};

    fixed4 _Color;

	v2f vert(appdata_t v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
#if UNITY_UV_STARTS_AT_TOP
		float scale = -1.0;
#else
		float scale = 1.0;
#endif
		o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
		o.uvgrab.zw = o.vertex.zw;
		o.diff =  v.color*_Color;
		return o;
	}

	sampler2D _GrabTexture;
	float4 _GrabTexture_TexelSize;
	float _Size;

	half4 frag(v2f i) : COLOR{

		half4 sum = half4(0,0,0,1);
#define GRABPIXEL(weight,kernely) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y + _GrabTexture_TexelSize.y * i.diff.a * kernely*_Size, i.uvgrab.z, i.uvgrab.w))) * weight
		
		sum += GRABPIXEL(0.096, -3.0);		
		sum += GRABPIXEL(0.231, -1.5);		
		sum += GRABPIXEL(0.346,  0.0);		
		sum += GRABPIXEL(0.231, +1.5);		
		sum += GRABPIXEL(0.096, +3.0);
		
		return sum * i.diff;
	}
		ENDCG
	}
	}
	}
}