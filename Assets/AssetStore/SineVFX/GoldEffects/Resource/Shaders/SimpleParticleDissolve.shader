// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SineVFX/GoldEffects/SimpleParticleDissolve"
{
	Properties
	{
		_FinalColor("Final Color", Color) = (1,1,1,1)
		_FinalPower("Final Power", Float) = 4
		_MainTex("MainTex", 2D) = "white" {}
		_DissolveTexture("Dissolve Texture", 2D) = "white" {}
		[Toggle(_DISSOLVETEXTUREFLIP_ON)] _DissolveTextureFlip("Dissolve Texture Flip", Float) = 1
		_DissolveTextureScale("Dissolve Texture Scale", Float) = 1
		_DissolveExp("Dissolve Exp", Float) = 6.47
		[HideInInspector] _tex4coord2( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma shader_feature _DISSOLVETEXTUREFLIP_ON
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float4 uv_tex4coord;
			float4 uv2_tex4coord2;
		};

		uniform float4 _FinalColor;
		uniform float _FinalPower;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _DissolveTexture;
		uniform float _DissolveTextureScale;
		uniform float _DissolveExp;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Emission = ( _FinalColor * _FinalPower * i.vertexColor ).rgb;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode9 = tex2D( _DissolveTexture, (i.uv_texcoord*( _DissolveTextureScale * i.uv_tex4coord.w ) + i.uv_tex4coord.z) );
			#ifdef _DISSOLVETEXTUREFLIP_ON
				float staticSwitch28 = ( 1.0 - tex2DNode9.r );
			#else
				float staticSwitch28 = tex2DNode9.r;
			#endif
			float clampResult35 = clamp( ( pow( ( 0.0 + staticSwitch28 ) , _DissolveExp ) + (-1.0 + (i.uv2_tex4coord2.x - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) ) , 0.0 , 1.0 );
			float clampResult8 = clamp( ( tex2D( _MainTex, uv_MainTex ).r - clampResult35 ) , 0.0 , 1.0 );
			o.Alpha = ( clampResult8 * i.vertexColor.a );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17005
7;29;1906;1004;1905.953;450.5145;1.3;True;False
Node;AmplifyShaderEditor.RangedFloatNode;17;-3257.686,309.1931;Inherit;False;Property;_DissolveTextureScale;Dissolve Texture Scale;5;0;Create;True;0;0;False;0;1;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;29;-3204.404,526.384;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexCoordVertexDataNode;15;-3050.386,188.5925;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-2956.404,389.384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;16;-2777.386,244.4927;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;9;-2507.101,261.2002;Inherit;True;Property;_DissolveTexture;Dissolve Texture;3;0;Create;True;0;0;False;0;92c924ff21316b14ca15f758005b0cec;3399d33e2a6b89b489a73b2013223073;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;27;-2209.404,437.384;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;28;-2090.404,286.384;Inherit;False;Property;_DissolveTextureFlip;Dissolve Texture Flip;4;0;Create;True;0;0;False;0;0;1;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-1730.629,417.6297;Inherit;False;Property;_DissolveExp;Dissolve Exp;7;0;Create;True;0;0;False;0;6.47;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-1759.482,185.9925;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;40;-1601.759,563.4863;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;36;-1370.415,692.6248;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;23;-1428.629,222.6297;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;13;-1139.499,488.0003;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-974,132;Inherit;True;Property;_MainTex;MainTex;2;0;Create;True;0;0;False;0;None;552d593f2654b7841acade12036b4305;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;35;-993.3989,493.7206;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;7;-631,268;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-472.8999,-144.7;Inherit;False;Property;_FinalPower;Final Power;1;0;Create;True;0;0;False;0;4;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1;-510.8999,-321.6999;Inherit;False;Property;_FinalColor;Final Color;0;0;Create;True;0;0;False;0;1,1,1,1;1,0.6509804,0.2784306,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;39;-414.8594,488.0852;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;8;-461.9999,270.9001;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;41;-473.3526,-63.11443;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-238.9,-232.7001;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-2464.629,91.6297;Inherit;False;Property;_DissolveRadialAmount;Dissolve Radial Amount;6;0;Create;True;0;0;False;0;0.5;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-2142.629,-55.3703;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;20;-2375.282,-125.2074;Inherit;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;19;-2562.484,-126.5073;Inherit;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-1,-1;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;18;-2784.783,-127.8075;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-137.9596,337.2849;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;2;ASEMaterialInspector;0;0;Unlit;SineVFX/GoldEffects/SimpleParticleDissolve;False;False;False;False;True;True;True;True;True;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;30;0;17;0
WireConnection;30;1;29;4
WireConnection;16;0;15;0
WireConnection;16;1;30;0
WireConnection;16;2;29;3
WireConnection;9;1;16;0
WireConnection;27;0;9;1
WireConnection;28;1;9;1
WireConnection;28;0;27;0
WireConnection;21;1;28;0
WireConnection;36;0;40;1
WireConnection;23;0;21;0
WireConnection;23;1;24;0
WireConnection;13;0;23;0
WireConnection;13;1;36;0
WireConnection;35;0;13;0
WireConnection;7;0;4;1
WireConnection;7;1;35;0
WireConnection;8;0;7;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;3;2;41;0
WireConnection;25;0;20;0
WireConnection;25;1;26;0
WireConnection;20;0;19;0
WireConnection;19;0;18;0
WireConnection;38;0;8;0
WireConnection;38;1;39;4
WireConnection;0;2;3;0
WireConnection;0;9;38;0
ASEEND*/
//CHKSM=C69C2BE4B5804D6BA89304126BA3A3A5D2409A9B