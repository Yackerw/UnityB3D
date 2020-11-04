
#if HIGH_QUALITY

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
	float4 vertColor : COLOR2;
	SHADOW_COORDS(1)
};

inline v2f vertFunc(appdata_full v, float4 _Color) {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.worldNormal = normalize(mul(v.normal.xyz, (float3x3) unity_WorldToObject));
	o.uv2 = v.texcoord1;
	o.ambient = ShadeSH9(half4(o.worldNormal, 1));
#if ADDSPHERE_ON || MULSPHERE_ON
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
#endif
	o.vertColor = v.color * _Color;
	TRANSFER_SHADOW(o)
		return o;
}

inline fixed4 fragFunc(v2f i, sampler2D _MainTex
#if ADDTEX_ON
	, sampler2D _AddTex, float4 _AddTex_ST
#endif
#if MULTEX_ON
	, sampler2D _MulTex, float4 _MulTex_ST
#endif
#if MULTEX2_ON
	, sampler2D _MulTex2, float4 _MulTex2_ST
#endif
#if ADDSPHERE_ON
	, sampler2D _AddSphereMap
#endif
#if MULSPHERE_ON
	, sampler2D _MulSphereMap
#endif
#if SPEC_ON
	, float _Glossiness
#endif
#ifdef RENDER_CUTOUT
	, float _Cutoff
#endif
) {
	i.worldNormal = normalize(i.worldNormal);
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
	fixed4 col = tex2D(_MainTex, i.uv) * i.vertColor;
#ifdef RENDER_CUTOUT
	clip(col.a - _Cutoff);
#endif
	// apply lighting
	col.rgb *= ((_LightColor0.rgb * max(0, lightFace) * atten) + i.ambient);
	// specular
#if SPEC_ON
	float3 E = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));

	float specAmnt = clamp(dot(E, SR), 0, 1);
	col.rgb += _LightColor0.rgb * max(0, lightFace) * atten * pow(specAmnt, 5) * _Glossiness;
#endif

#if ADDSPHERE_ON || ADDTEX_ON
	float3 additive = { 0, 0, 0 };
#endif
#if MULSPHERE_ON || MULTEX_ON || MULTEX2_ON
	float3 multiplicative = { 1, 1, 1 };
#endif

#if ADDSPHERE_ON || MULSPHERE_ON
	// calculate reflections
	float2 reflcoords;
	reflcoords.xy = (mul(UNITY_MATRIX_V, i.worldNormal).xy + 1.0) / 2.0;
#if ADDSPHERE_ON
	additive += tex2Dlod(_AddSphereMap, float4(reflcoords.xy, 0, 0));
#endif
#if MULSPHERE_ON
	multiplicative *= tex2Dlod(_MulSphereMap, float4(reflcoords.xy, 0, 0));
#endif
#endif
	// also add the regular additive/multiplicative maps
#if ADDTEX_ON
	additive += tex2D(_AddTex, TRANSFORM_TEX(i.uv2, _AddTex));
#endif
#if MULTEX_ON
	multiplicative *= tex2D(_MulTex, TRANSFORM_TEX(i.uv2, _MulTex));
#endif
#if MULTEX2_ON
	multiplicative *= tex2D(_MulTex2, TRANSFORM_TEX(i.uv2, _MulTex2));
#endif
	// apply our additive and multiplicative values now...
	// i'm about to sin
	col.rgb = pow(col.rgb, 0.4545);
#if MULTEX_ON || MULSPHERE_ON || MULTEX2_ON
	col.rgb *= pow(multiplicative, 0.4545);
#endif
#if ADDTEX_ON || ADDSPHERE_ON
	col.rgb += pow(additive, 0.4545);
#endif
	col.rgb = pow(col.rgb, 2.2);
	return col;
}

#endif

#if MED_QUALITY

struct v2f {
	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD3;
	float4 pos : SV_POSITION;
	float3 ambient : COLOR1;
#if ADDSPHERE_ON || MULSPHERE_ON
	float3 reflcoords : TEXCOORD2;
#endif
	SHADOW_COORDS(1)
	float lightFace : TEXCOORD4;
#if SPEC_ON
	float specAmnt : TEXCOORD5;
#endif
};





inline v2f vertFunc(appdata_full v
#if SPEC_ON
	, float _Glossiness
#endif

) {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	float3 worldNormal = normalize(mul(v.normal.xyz, (float3x3) unity_WorldToObject));
	o.uv2 = v.texcoord1;
	o.ambient = ShadeSH9(half4(worldNormal, 1));
#if ADDSPHERE_ON || MULSPHERE_ON
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
#endif
	TRANSFER_SHADOW(o)

	// lighting
	o.lightFace = 0.0;
#if SPEC_ON
	// specular too
	float3 SR;
#endif
	if (_WorldSpaceLightPos0.w < 0.5f) { // directional light
		o.lightFace = max(dot(worldNormal, _WorldSpaceLightPos0.xyz), 0);
#if SPEC_ON
		SR = reflect(_WorldSpaceLightPos0.xyz, worldNormal);
#endif
	}
	else { // point light
		o.lightFace = normalize(UnityObjectToClipPos(_WorldSpaceLightPos0.xyz) - o.pos);
#if SPEC_ON
		SR = reflect(o.lightFace, worldNormal);
#endif

	// specular
#if SPEC_ON
		float3 E = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));

		o.specAmnt = pow(clamp(dot(E, SR), 0, 1), 5) * _Glossiness;
#endif


	// sphere maps
#if ADDSPHERE_ON || MULSPHERE_ON
		// calculate reflections
		o.reflcoords.xy = (mul(UNITY_MATRIX_V, worldNormal).xy + 1.0) / 2.0;
		//o.reflcoords = reflect(normalize(worldPos - _WorldSpaceCameraPos), worldNormal);
#endif

	}

		return o;
}





inline fixed4 fragFunc(v2f i, sampler2D _MainTex, float4 _Color
#if ADDTEX_ON
	, sampler2D _AddTex, float4 _AddTex_ST
#endif
#if MULTEX_ON
	, sampler2D _MulTex, float4 _MulTex_ST
#endif
#if MULTEX2_ON
	, sampler2D _MulTex2, float4 _MulTex2_ST
#endif
#if ADDSPHERE_ON
	, sampler2D _AddSphereMap
#endif
#if MULSPHERE_ON
	, sampler2D _MulSphereMap
#endif
#ifdef RENDER_CUTOUT
	, float _Cutoff
#endif

) {
	float atten = SHADOW_ATTENUATION(i);
	fixed4 col = tex2D(_MainTex, i.uv) * _Color;
#ifdef RENDER_CUTOUT
	clip(col.a - _Cutoff);
#endif
	// apply lighting
	col.rgb *= i.ambient + _LightColor0.rgb * i.lightFace * atten;
	// specular
#if SPEC_ON
	col.rgb += _LightColor0.rgb * i.lightFace * atten * i.specAmnt;
#endif
	// additive/multiplicative textures
#if ADDTEX_ON || ADDSPHERE_ON
	float3 additive = { 0, 0, 0 };
#endif
#if MULTEX_ON || MULSPHERE_ON || MULTEX2_ON
	float3 multiplicative = { 1, 1, 1 };
#endif

/*#if ADDSPHERE_ON || MULSPHERE_ON
	i.reflcoords = normalize(i.reflcoords);
	float m = 2.0 * sqrt(i.reflcoords.x*i.reflcoords.x + i.reflcoords.y*i.reflcoords.y + (i.reflcoords.z + 1.0)*(i.reflcoords.z + 1.0));
	i.reflcoords.x = i.reflcoords.x / m + 0.5;
	i.reflcoords.y = i.reflcoords.y / m + 0.5;
#endif*/

#if ADDSPHERE_ON
	additive += tex2Dlod(_AddSphereMap, float4(i.reflcoords.xy, 0, 0));
#endif
#if MULSPHERE_ON
	multiplicative *= tex2Dlod(_MulSphereMap, float4(i.reflcoords.xy, 0, 0));
#endif
	// also add the regular additive/multiplicative maps
#if ADDTEX_ON
	additive += tex2D(_AddTex, TRANSFORM_TEX(i.uv2, _AddTex));
#endif
#if MULTEX_ON
	multiplicative *= tex2D(_MulTex, TRANSFORM_TEX(i.uv2, _MulTex));
#endif
#if MULTEX2_ON
	multiplicative *= tex2D(_MulTex2, TRANSFORM_TEX(i.uv2, _MulTex2));
#endif

	// i'm about to sin
	col.rgb = pow(col.rgb, 0.4545);
#if MULTEX_ON || MULSPHERE_ON || MULTEX2_ON
	col.rgb *= pow(multiplicative, 0.4545);
#endif
#if ADDTEX_ON || ADDSPHERE_ON
	col.rgb += pow(additive, 0.4545);
#endif
	col.rgb = pow(col.rgb, 2.2);

	return col;
}

#endif