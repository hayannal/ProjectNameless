Shader "KriptoFX/RFX1/Particle" {
	Properties {
	[HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
	 [HideInInspector]_Cutout ("_Cutout", Float) = 0.2
	 [HideInInspector]_InvFade ("Soft Particles Factor", Float) = 1.0
	 [HideInInspector]_FresnelStr ("Fresnel Strength", Float) = 1.0
	 [HideInInspector]SrcMode ("SrcMode", int) = 5
     [HideInInspector]DstMode ("DstMode", int) = 10
	 [HideInInspector]CullMode ("Cull Mode", int) = 2 //0 = off, 2=back
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RFX1"="Particle"}
				Blend [SrcMode] [DstMode]
				Cull [CullMode] 
				ZWrite Off
				
	SubShader {
		Pass {
				
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma multi_compile BlendAdd BlendAlpha BlendMul BlendMul2
			#pragma multi_compile VertLight_OFF VertLight4_ON VertLight4Normal_ON
			#pragma shader_feature FrameBlend_OFF

			#pragma multi_compile Clip_OFF Clip_ON Clip_ON_Alpha
			#pragma multi_compile FresnelFade_OFF FresnelFade_ON
			#pragma multi_compile _ _MOBILEDEPTH_ON
#pragma target 3.0
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _TintColor;
			float _Cutout;
			half _FresnelStr;
			half _BloomThreshold;
			
			struct appdata_t {
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				half4 color : COLOR;
#ifdef FrameBlend_OFF
				float2 texcoord : TEXCOORD0;
#else
#if UNITY_VERSION == 600
				float4 texcoords : TEXCOORD0;
				float texcoordBlend : TEXCOORD1;
#else
				float2 texcoord : TEXCOORD0;
				float4 texcoordBlendFrame : TEXCOORD1;
#endif
#endif
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
#ifdef FrameBlend_OFF
				float2 texcoord : TEXCOORD0;
#else
				float4 texcoord : TEXCOORD0;
				fixed blend : TEXCOORD1;
#endif
				UNITY_FOG_COORDS(2)

				float4 projPos : TEXCOORD3;

#ifdef FresnelFade_ON
				float fresnel : TEXCOORD4;
#endif

			};

			
			

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);

				o.color = v.color;
				//o.color.rgb *= ComputeVertexLight(v.vertex, v.normal);

#ifdef FrameBlend_OFF
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
#else
#if UNITY_VERSION == 600
				o.texcoord.xy = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
				o.texcoord.zw = TRANSFORM_TEX(v.texcoords.zw, _MainTex);
				o.blend = v.texcoordBlend;
#else
				o.texcoord.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord.zw = TRANSFORM_TEX(v.texcoordBlendFrame.xy, _MainTex);
				o.blend = v.texcoordBlendFrame.z;
#endif
#endif
#ifdef FresnelFade_ON
				o.fresnel = abs(dot(normalize(v.normal), normalize(ObjSpaceViewDir(v.vertex))));
				o.fresnel = saturate((pow(o.fresnel, _FresnelStr)) * 2);
#endif
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			float _InvFade;
			
			half4 frag (v2f i) : SV_Target
			{
	
			#ifdef FrameBlend_OFF
				half4 tex = tex2D(_MainTex, i.texcoord);
			#else
				//half4 tex = Tex2DInterpolated(_MainTex, i.texcoord, _Tiling);
				half4 tex1 = tex2D(_MainTex, i.texcoord.xy);
				half4 tex2 = tex2D(_MainTex, i.texcoord.zw);
				half4 tex = lerp(tex1, tex2, i.blend);
			#endif

				half4 res = 2 * tex * _TintColor;

			#ifdef Clip_ON
				res.a = step(_Cutout, tex.a) * res.a;
			#endif	

			#ifdef Clip_ON_Alpha
				res.a = step(1-i.color.a + _Cutout, tex.a);
				res.rgb *= i.color.rgb;
			#endif	

			#if !defined(Clip_ON_Alpha)
				res *= i.color;
			#endif	
			
				res.a = saturate(res.a);
				//res *= i.color;
			#ifdef FresnelFade_ON
				res.a *= i.fresnel;
			#endif
				

			#ifdef BlendAdd
				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, half4(0,0,0,0)); 
			#endif
			#ifdef BlendAlpha
				UNITY_APPLY_FOG(i.fogCoord, res);
			#endif
			#ifdef BlendMul
				res = lerp(half4(1,1,1,1), res, res.a);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, half4(1,1,1,1)); // fog towards white due to our blend mode
			#endif
			#ifdef BlendMul2
				res = lerp(half4(0.5,0.5,0.5,0.5), res, res.a);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, half4(0.5,0.5,0.5,0.5)); // fog towards gray due to our blend mode
			#endif
				return res;
			}
			ENDCG 
		}
	}	
}
 CustomEditor "RFX1_CustomMaterialInspectorParticle"
}