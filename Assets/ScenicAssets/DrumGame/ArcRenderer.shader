Shader "Custom/ArcRenderer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[PerRendererData]_Color ("Color", Color) = (0.0, 0.0, 0.0, 0.0)
		[PerRendererData]_ColorIntensity("ColorIntensity", Float) = 1.0

		[PerRendererData]_Fill ("Fill", Range(0.0, 1.0)) = 0.25
		_Radius ("Radius", Range (0.0, 1.0)) = 0.5
		_Thickness ("Thickness", Range (0.0, 1.0)) = 0.25
		_EdgeWidth ("EdgeWidth", Range (0.0, 0.2)) = 0.1
		[PerRendererData]_Angle ("Angle", Range(0.0, 1.0)) = 0.5
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		
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
			float4 _Color;
			float _ColorIntensity;
			float _Fill;
			float _Angle;
			float _Radius;
			float _Thickness;
			float _EdgeWidth;

			fixed2 Rotate(fixed2 UV, float Rotation) 
			{
				float2 Center = (0.5, 0.5);
				UV -= Center;
				float s = sin(Rotation);
				float c = cos(Rotation);
				float2x2 rMatrix = float2x2(c, -s, s, c);
				rMatrix *= 0.5;
				rMatrix += 0.5;
				rMatrix = rMatrix * 2 - 1;
				UV.xy = mul(UV.xy, rMatrix);
				UV += Center;
				return UV;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// fixed4 col = tex2D(_MainTex, i.uv);

				//Map input parameters in terms of pi
				float fill = _Fill * 3.14159 * 2.0 - 3.14159;
				float angle = _Angle * 6.28318;

				//Angle operations: prepare the angle which will be drawn as an arc
				float2 st = i.uv;
				st = Rotate(st, angle);
				st = float2(st.x * 2.0 - 1.0, st.y * 2.0 - 1.0);
				float arctan = atan2(st.x, st.y);

				//Use the angle and remapped fill to clean up the resulting angle
				float drawAngle = 1.0 - ceil(arctan - fill);
				drawAngle = clamp(drawAngle, 0.0, 1.0);

				//float delta = fwidth(uvLength);

				//Draw two circles, the inner one is inverted
				float uvLength = length(st);
				float drawOuterCircle = smoothstep(_Radius, _Radius - _EdgeWidth, uvLength);
				float drawInnerCircle = smoothstep(_Radius - _Thickness - _EdgeWidth, _Radius - _Thickness, uvLength);
				float alpha = drawOuterCircle * drawInnerCircle * drawAngle;

				float3 mainColor = _Color.rgb * _ColorIntensity;

				return fixed4(mainColor, alpha);
			}
			ENDCG
		}
	}
}
