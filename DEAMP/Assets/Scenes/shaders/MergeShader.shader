Shader "Custom/MergeShader"
{
	Properties
	{
		_Tex1("Texture", 2D) = "white" {}
		_Tex2("Texture", 2D) = "white" {}
		_Tex3("Texture", 2D) = "white" {}
		_iResolutionX("_iResolutionX", float) = 500
		_iResolutionY("_iResolutionY", float) = 500


		_eyeX("_eyeX", float) = 0.5
		_eyeY("_eyeY", float) = 0.5

		_boundF("_boundF", float) = 0.2
		_boundM("_boundM", float) = 0.
		_boundP("_boundP", float) = 0.2

		_showEdges("_showEdges", int) = 0
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

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}
				sampler2D _Tex1;
				sampler2D _Tex2;
				sampler2D _Tex3;
				uniform float _iResolutionX;
				uniform float _iResolutionY;
				uniform float _eyeX;
				uniform float _eyeY;
				uniform float _boundF;
				uniform float _boundM;
				uniform float _boundP;
				uniform int _showEdges;

				bool squareBorder(float2 cd, float r) {
					bool b0 = cd.x >= -r && cd.x <= r;
					bool b1 = cd.y >= -r && cd.y <= r;
					bool b2 = cd.y >= r - 0.003 && cd.y <= r + 0.003;
					bool b3 = cd.y >= -r - 0.003 && cd.y <= -r + 0.003;
					bool b4 = cd.x >= r - 0.003 && cd.x <= r + 0.003;
					bool b5 = cd.x >= -r - 0.003 && cd.x <= -r + 0.003;
					bool e1 = b0 && b2;
					bool e2 = b0 && b3;
					bool e3 = b1 && b4;
					bool e4 = b1 && b5;
					return e1 || e2 || e3 || e4;
				}

				fixed4 frag(v2f i) : SV_Target
			{
				float2 tc = i.uv;
				float2 iResolution = float2(_iResolutionX, _iResolutionY);
				float2 cursorPos = float2(_eyeX * 2.0 - 1.0, _eyeY * 2.0 - 1.0);
				float2 pq = tc * 2.0 - 1.0 - cursorPos;

				float4 col = ((abs(pq.x * 0.5f) < _boundF) && (abs(pq.y * 0.5f) < _boundF)) ? tex2D(_Tex1, i.uv) : 
							 ((abs(pq.x * 0.5f) < _boundM) && (abs(pq.y * 0.5f) < _boundM)) ? tex2D(_Tex2, i.uv) : tex2D(_Tex3, i.uv);
				
				fixed4 newColor;

				if (_showEdges == 1) {
					float2 cd = pq * 0.5f;
					if (squareBorder(cd, _boundF) || squareBorder(cd, _boundM))
						newColor = float4(1.0, 0.0, 0.0, 0.0);
					else 
						newColor = col;
				}
				else newColor = col;

				if (length(i.uv - float2(_eyeX, _eyeY)) < 0.003)
					return float4(1.0, 0.0, 0.0, 0.0);
				return newColor;
			}
			ENDCG
		}
		}
}
