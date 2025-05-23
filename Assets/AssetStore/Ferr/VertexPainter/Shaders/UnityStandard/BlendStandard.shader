Shader "Ferr/Blend/Standard" {
	Properties {
		_BlendStrength("Blend Strength", Range(0.001,0.2)) = 0.1

		_Smoothness ("Smoothness",   Range(0,1)) = 1
		_Metallic   ("Metallic",     Range(0,1)) = 1
		
		_MainTex ("Red   Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap ("Red   Normal Map",               2D   ) = "bump"  {}
		_SpecTex ("Red   Metal (R) Smooth (A)",     2D   ) = "white" {}
		
		_MainTex2("Green Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap2("Green Normal Map",               2D   ) = "bump"  {}
		_SpecTex2("Green Metal (R) Smooth (A)",     2D   ) = "white" {}
		
		_MainTex3("Blue  Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap3("Blue  Normal Map",               2D   ) = "bump"  {}
		_SpecTex3("Blue  Metal (R) Smooth (A)",     2D   ) = "white" {}

		_MainTex4("Alpha Texture (RGB) Height (A)", 2D) = "white" {}
		_BumpMap4("Alpha Normal Map",               2D) = "bump"  {}
		_SpecTex4("Alpha Metal (R) Smooth (A)",     2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		
		#pragma shader_feature BLEND_TEX_2 BLEND_TEX_3 BLEND_TEX_4
		#pragma shader_feature BLEND_HEIGHT BLEND_HARD BLEND_SOFT
		#pragma shader_feature BLEND_WORLDUV_OFF BLEND_WORLDUV
		
		#define BLEND_NORMAL
		#define BLEND_SURFACE
		#define BLEND_METALLIC
		#define BLEND_SPECULAR
		#define BLEND_SEPARATEOFFSETS
		#define BLEND_METAL_MULTICOMPONENT
		
		#include "../BlendCommon.cginc"
		#include "BlendStandardCommon.cginc"

		ENDCG
	}
	CustomEditor "Ferr.BlendShaderStandardGUI"
	FallBack "Diffuse"
}
