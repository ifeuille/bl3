Shader "Unlit/Uber"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
		_OutlineColor ("OutlineColor",Color) = (1,1,1,1)
		_FilterValue("FilterValue",Range(0,1))=0.374
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off ZWrite On ZTest Off
		// Blending matches SDK Sample, which 
		// works in terms of (1-a) and otherwise premultiplied.
		Blend One SrcAlpha
		ColorMask RGBA

		/*Stencil {
			Ref 0
			Comp Less
			ReadMask 255
			WriteMask 0
			Pass Keep
			Fail Keep
			ZFail Keep
		}*/

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				uint vid : SV_VertexID;
            };

			struct v2f {
				float4 pos : SV_POSITION;
				float4 scrPos:TEXCOORD1;
			};

            //sampler2D _MainTex;
            //float4 _MainTex_ST;
			sampler2D _OutlineTexture;
			float4 _OutlineTexture_ST;
			sampler2D _CameraDepthTexture;

			float _FilterValue;
			half4 _OutlineColor;
			half4 vertexs[3] = {
				{-1,0,0,1},
				{1,0,0,1},
				{0,1,0,1}
			};
			//Vertex Shader  
			v2f vert (appdata v) {

				v2f o;
				o.pos = vertexs[v.vid];// UnityObjectToClipPos (v.vertex);
				o.scrPos = ComputeScreenPos (o.pos);
				o.scrPos = vertexs[v.vid];
				return o;
			}
			//Fragment Shader  
			half4 frag (v2f i) : COLOR{
				return half4(1,0,0,1);
			    float depthValue = Linear01Depth (tex2Dproj (_CameraDepthTexture,UNITY_PROJ_COORD (i.scrPos)).r);
				bool needClip = (0.00001 >= depthValue) ? true : false;
				if (needClip)discard;
				float outline = tex2D (_OutlineTexture,i.scrPos).r;
				if (outline < _FilterValue)
				{
					return _OutlineColor;
				}
				return float4(0, 0, 0, 0);
				//float4 color = tex2D (_MainTex, i.scrPos);
			}
            ENDCG
        }
    }
}
