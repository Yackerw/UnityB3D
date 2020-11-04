Shader "Journey/BlitzTransparent"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	_AddTex("Additive texture", 2D) = "black" {}
	_MulTex("Multiplicative texture", 2D) = "white" {}
	_MulTex2("Multiplicative texture 2", 2D) = "white" {}
		_AddSphereMap("Add Sphere Reflection", 2D) = "black" {}
		_MulSphereMap("Mul Sphere Reflection", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.0
		_Color("Color", Color) = (1,1,1,1)
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent"
			"Queue" = "Transparent"
					"LightMode" = "ForwardBase"
		}
				ZWrite Off
				Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#pragma multi_compile_fwdadd_fullshadows
	#include "Lighting.cginc"
	#include "AutoLight.cginc"
#include "Assets/Shaders/include/Blitz.cginc"

#pragma multi_compile __ ADDTEX_ON
#pragma multi_compile __ MULTEX_ON
#pragma multi_compile __ ADDSPHERE_ON
#pragma multi_compile __ MULSPHERE_ON
#pragma multi_compile __ SPEC_ON
#pragma multi_compile __ MULTEX2_ON
#pragma multi_compile HIGH_QUALITY MED_QUALITY

			sampler2D _MainTex;
			sampler2D _AddTex;
			sampler2D _MulTex;
			sampler2D _MulTex2;
			sampler2D _AddSphereMap;
			sampler2D _MulSphereMap;
			float4 _MainTex_ST;
			float4 _AddTex_ST;
			float4 _MulTex_ST;
			float4 _MulTex2_ST;
			float4 _Color;
			float _Glossiness;

			v2f vert(appdata_full v)
			{
#if HIGH_QUALITY
				v2f o = vertFunc(v, _Color);
#endif
#if MED_QUALITY
				v2f o = vertFunc(v
#if SPEC_ON
					, _Glossiness
#endif
				);
#endif
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
#if HIGH_QUALITY
				return fragFunc(i, _MainTex
#if ADDTEX_ON
				,_AddTex, _AddTex_ST
#endif
#if MULTEX_ON
					, _MulTex, _MulTex_ST
#endif
#if MULTEX2_ON
					, _MulTex2, _MulTex2_ST
#endif
#if ADDSPHERE_ON
					, _AddSphereMap
#endif
#if MULSPHERE_ON
					, _MulSphereMap
#endif
#if SPEC_ON
					, _Glossiness
#endif
				);
#endif
			#if MED_QUALITY
			return fragFunc(i, _MainTex, _Color
#if ADDTEX_ON
				, _AddTex, _AddTex_ST
#endif
#if MULTEX_ON
				, _MulTex, _MulTex_ST
#endif
#if MULTEX2_ON
				, _MulTex2, _MulTex2_ST
#endif
#if ADDSPHERE_ON
				, _AddSphereMap
#endif
#if MULSPHERE_ON
				, _MulSphereMap
#endif
			);
#endif
			}
				ENDCG
			}
		}
			CustomEditor "BlitzInspector"
			FallBack "Legacy Shaders/Transparent/VertexLit"
}
