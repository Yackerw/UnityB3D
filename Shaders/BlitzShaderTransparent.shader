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

	#pragma multi_compile __ ADDTEX_ON
	#pragma multi_compile __ MULTEX_ON
	#pragma multi_compile __ ADDSPHERE_ON
	#pragma multi_compile __ MULSPHERE_ON
	#pragma multi_compile __ SPEC_ON
#pragma multi_compile __ MULTEX2_ON

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float2 uv2 : TEXCOORD3;
					float4 pos : SV_POSITION;
					float3 worldNormal : NORMAL;
					float3 ambient : COLOR1;
	#if ADDSPHERE_ON || MULSPHERE_ON
					float3 worldPos : TEXCOORD2;
	#endif
					SHADOW_COORDS(1)
				};

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
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldNormal = normalize(mul(v.normal.xyz, (float3x3) unity_WorldToObject));
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.uv2 = v.texcoord1;
					o.ambient = ShadeSH9(half4(o.worldNormal, 1));
	#if ADDSPHERE_ON || MULSPHERE_ON
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
	#endif
					TRANSFER_SHADOW(o)
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float atten = SHADOW_ATTENUATION(i);
					float lightFace = 0.0;
	#if SPEC_ON
					// specular too
					float3 SR;
	#endif
					if (_WorldSpaceLightPos0.w < 0.5f) { // directional light
						lightFace = dot(i.worldNormal, _WorldSpaceLightPos0.xyz);
	#if SPEC_ON
						SR = reflect(_WorldSpaceLightPos0.xyz, i.worldNormal);
	#endif
					}
					else { // point light
						lightFace = normalize(UnityObjectToClipPos(_WorldSpaceLightPos0.xyz) - i.pos);
	#if SPEC_ON
						SR = reflect(lightFace, i.worldNormal);
	#endif
					}
					// sample the texture
					fixed4 col = tex2D(_MainTex, i.uv);
					// apply lighting
					col.rgb *= ((_LightColor0.rgb * max(0, lightFace) * atten) + i.ambient);
					col.a *= _Color.a;
					// specular
	#if SPEC_ON
					float3 E = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));

					float specAmnt = clamp(dot(E, SR), 0, 1);
					col.rgb += _LightColor0.rgb * max(0, lightFace) * atten * pow(specAmnt,5) * _Glossiness;
	#endif

	#if ADDSPHERE_ON || ADDTEX_ON
					float3 additive = { 0, 0, 0 };
	#endif
	#if MULSPHERE_ON || MULTEX_ON || MULTEX2_ON
					float3 multiplicative = { 1, 1, 1 };
	#endif

	#if ADDSPHERE_ON || MULSPHERE_ON
					// calculate reflections
					float3 r = reflect(normalize(i.worldPos - _WorldSpaceCameraPos), i.worldNormal);
					float m = 2.0 * sqrt(r.x*r.x + r.y*r.y + (r.z + 1.0)*(r.z + 1.0));
					float2 reflcoords;
					reflcoords.x = r.x / m + 0.5;
					reflcoords.y = r.y / m + 0.5;
	#if ADDSPHERE_ON
					additive += tex2D(_AddSphereMap, reflcoords);
	#endif
	#if MULSPHERE_ON
					multiplicative *= tex2D(_MulSphereMap, reflcoords);
	#endif
	#endif
					// apply our additive and multiplicative values now...
#if ADDTEX_ON
					additive += tex2D(_AddTex, TRANSFORM_TEX(i.uv2, _AddTex));
#endif
#if MULTEX_ON
					multiplicative *= tex2D(_MulTex, TRANSFORM_TEX(i.uv2, _MulTex));
#endif
#if MULTEX2_ON
					multiplicative *= tex2D(_MulTex2, TRANSFORM_TEX(i.uv2, _MulTex2));
#endif
					// also add the regular additive/multiplicative maps
#if MULTEX_ON
					multiplicative *= tex2D(_MulTex, i.uv);
#endif
	#if ADDTEX_ON
					additive += tex2D(_AddTex, i.uv);
	#endif
					col.rgb *= _Color;
					return col;
				}
				ENDCG
			}
		}
			CustomEditor "BlitzInspector"
			FallBack "Legacy Shaders/Transparent/VertexLit"
}

/*{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	_AddTex("Additive texture", 2D) = "black" {}
	_MulTex("Multiplicative texture", 2D) = "white" {}
		_AddSphereMap("Add Sphere Reflection", 2D) = "black" {}
		_MulSphereMap("Mul Sphere Reflection", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.0
		_Color("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent"
		"Queue"="Transparent"
				"LightMode"="ForwardBase"
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

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 ambient : COLOR1;
				float3 worldPos : TEXCOORD2;
				SHADOW_COORDS(1)
			};

			sampler2D _MainTex;
			sampler2D _AddTex;
			sampler2D _MulTex;
			sampler2D _AddSphereMap;
			sampler2D _MulSphereMap;
			float4 _MainTex_ST;
			float4 _Color;
			float _Glossiness;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = normalize(mul(v.normal.xyz, (float3x3) unity_WorldToObject));
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float atten = SHADOW_ATTENUATION(i);
				float lightFace = 0.0;
				// specular too
				float3 SR;
				if (_WorldSpaceLightPos0.w < 0.5f) { // directional light
					lightFace = dot(i.worldNormal, _WorldSpaceLightPos0.xyz);
					SR = reflect(_WorldSpaceLightPos0.xyz, i.worldNormal);
				}
				else { // point light
					lightFace = normalize(UnityObjectToClipPos(_WorldSpaceLightPos0.xyz) - i.pos);
					SR = reflect(lightFace, i.worldNormal);
				}
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				col.a *= _Color.a;
				// apply lighting
				col.rgb *= ((_LightColor0.rgb * max(0, lightFace) * atten) + i.ambient);
				// specular
				float3 E = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));

				float specAmnt = clamp(dot(E, SR), 0, 1);
				col.rgb += _LightColor0.rgb * max(0, lightFace) * atten * pow(specAmnt,5) * _Glossiness;

				// calculate reflections
				float3 r = reflect(normalize(i.worldPos - _WorldSpaceCameraPos), i.worldNormal);
				float m = 2.0 * sqrt(r.x*r.x + r.y*r.y + (r.z + 1.0)*(r.z + 1.0));
				float2 reflcoords;
				reflcoords.x = r.x / m + 0.5;
				reflcoords.y = r.y / m + 0.5;

				// apply reflections
				col.rgb += tex2D(_AddSphereMap, reflcoords) + tex2D(_AddTex, i.uv);
				col.rgb *= tex2D(_MulSphereMap, reflcoords) * tex2D(_MulTex, i.uv) * _Color;
				return col;
			}
			ENDCG
		}
	}
		FallBack "Legacy Shaders/Transparent/VertexLit"
}
*/