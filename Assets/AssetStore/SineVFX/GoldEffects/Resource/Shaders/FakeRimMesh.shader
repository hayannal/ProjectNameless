// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SineVFX/GoldEffects/FakeRimLightMesh"
{
	Properties
	{
		_Mask("Mask", 2D) = "white" {}
		_FinalColor("Final Color", Color) = (1,1,1,1)
		_FinalPower("Final Power", Float) = 4
		[Toggle(_FRESNELENABLED_ON)] _FresnelEnabled("Fresnel Enabled", Float) = 0
		_FresnelPower("Fresnel Power", Range( 0.1 , 10)) = 2
		_FresnelPowerAfterOneMinus("Fresnel Power After One Minus", Range( 0.1 , 10)) = 2
		_Float0("Float 0", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#pragma target 3.0
		#pragma shader_feature _FRESNELENABLED_ON
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
		};

		uniform float4 _FinalColor;
		uniform float _FinalPower;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float _Float0;
		uniform float _FresnelPower;
		uniform float _FresnelPowerAfterOneMinus;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Emission = ( _FinalColor * _FinalPower ).rgb;
			float2 uv0_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 tex2DNode1 = tex2D( _Mask, uv0_Mask );
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV6 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode6 = ( 0.0 + _Float0 * pow( 1.0 - fresnelNdotV6, _FresnelPower ) );
			float clampResult9 = clamp( ( tex2DNode1.r * pow( ( 1.0 - fresnelNode6 ) , _FresnelPowerAfterOneMinus ) ) , 0.0 , 1.0 );
			#ifdef _FRESNELENABLED_ON
				float staticSwitch10 = clampResult9;
			#else
				float staticSwitch10 = tex2DNode1.r;
			#endif
			o.Alpha = staticSwitch10;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17005
7;29;1906;1004;1469.217;40.64404;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;12;-1188.848,458.8107;Inherit;False;Property;_FresnelPower;Fresnel Power;4;0;Create;True;0;0;False;0;2;1;0.1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-1068.217,347.356;Inherit;False;Property;_Float0;Float 0;6;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;6;-876,372;Inherit;True;Standard;WorldNormal;ViewDir;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;2;-1134,171;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-719.2173,597.356;Inherit;False;Property;_FresnelPowerAfterOneMinus;Fresnel Power After One Minus;5;0;Create;True;0;0;False;0;2;1;0.1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;7;-586,372;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;14;-315.2173,484.356;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-870,147;Inherit;True;Property;_Mask;Mask;0;0;Create;True;0;0;False;0;2b2f1db59193bda4389fdbe055cf7fa4;009f3097051622846a801019b3fa9e3b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-342,266;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-560,-206;Inherit;False;Property;_FinalPower;Final Power;2;0;Create;True;0;0;False;0;4;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;9;-197,267;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-597,-381;Inherit;False;Property;_FinalColor;Final Color;1;0;Create;True;0;0;False;0;1,1,1,1;1,0.6509804,0.2784312,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-269,-275;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;10;66,216;Inherit;False;Property;_FresnelEnabled;Fresnel Enabled;3;0;Create;True;0;0;False;0;0;0;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;545,-66;Float;False;True;2;ASEMaterialInspector;0;0;Unlit;SineVFX/GoldEffects/FakeRimLightMesh;False;False;False;False;True;True;True;True;True;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;2;15;0
WireConnection;6;3;12;0
WireConnection;7;0;6;0
WireConnection;14;0;7;0
WireConnection;14;1;13;0
WireConnection;1;1;2;0
WireConnection;8;0;1;1
WireConnection;8;1;14;0
WireConnection;9;0;8;0
WireConnection;3;0;4;0
WireConnection;3;1;5;0
WireConnection;10;1;1;1
WireConnection;10;0;9;0
WireConnection;0;2;3;0
WireConnection;0;9;10;0
ASEEND*/
//CHKSM=7BB11912DECAFE9107913F43881656F5CB3C2136