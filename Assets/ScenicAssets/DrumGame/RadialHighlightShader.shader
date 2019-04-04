Shader "Custom/RadialHighlight"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// just invert the colors
				col.rgb = 1 - col.rgb;
				return col;

			// 	fixed2 st = i.uv;
			// 	fixed3 mainColor = fixed3(0.735,0.177,0.072);
			// 	fixed3 edgeColor = fixed3(0.930,0.902,0.161);
			// 	float mainWidth = 0.1;
			// 	float edgeWidth = 0.01;

			// 	// Use polar coordinates instead of cartesian
			// 	fixed2 toCenter = fixed2(0.5, 0.5)-st;
			// 	float angle = atan(toCenter.y / toCenter.x);
			// 	//angle = u_time + angle;
			// 	float radius = length(toCenter)*4.0;

			// 	// Map the angle (-PI to PI) to the Hue (from 0 to 1)
			// 	// and the Saturation to the radius
				
			// 	fixed3 black = fixed3(0.0, 0.0, 0.0);
			// 	float p = step(0.372, length(toCenter));
			// 	mainColor = lerp(mainColor, black, p);

			// 	return fixed4(mainColor,1.0);
			}
			ENDCG
		}
	}
}
