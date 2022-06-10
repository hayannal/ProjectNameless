// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SineVFX/GoldEffects/CoinParticle"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_AlbedoTransparency("Albedo Transparency", 2D) = "white" {}
		_AlbedoColorTint("Albedo Color Tint", Color) = (1,1,1,1)
		_Normal("Normal", 2D) = "bump" {}
		_MetallicSmoothness("MetallicSmoothness", 2D) = "white" {}
		_Metallic("Metallic", Range( 0 , 1)) = 1
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Off
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha nolightmap noforwardadd interpolateview approxview vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			half ASEVFace : VFACE;
		};

		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform sampler2D _AlbedoTransparency;
		uniform float4 _AlbedoTransparency_ST;
		uniform float4 _AlbedoColorTint;
		uniform sampler2D _MetallicSmoothness;
		uniform float4 _MetallicSmoothness_ST;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			v.vertex.xyz += ( v.texcoord.z * float3(0,1,0) );
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			float3 tex2DNode11 = UnpackNormal( tex2D( _Normal, uv_Normal ) );
			float3 switchResult12 = (((i.ASEVFace>0)?(tex2DNode11):(( tex2DNode11 * float3(1,1,-1) ))));
			o.Normal = switchResult12;
			float2 uv_AlbedoTransparency = i.uv_texcoord * _AlbedoTransparency_ST.xy + _AlbedoTransparency_ST.zw;
			float4 tex2DNode1 = tex2D( _AlbedoTransparency, uv_AlbedoTransparency );
			o.Albedo = ( tex2DNode1 * _AlbedoColorTint ).rgb;
			float2 uv_MetallicSmoothness = i.uv_texcoord * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
			float4 tex2DNode6 = tex2D( _MetallicSmoothness, uv_MetallicSmoothness );
			o.Metallic = ( tex2DNode6.r * _Metallic );
			o.Smoothness = ( tex2DNode6.a * _Smoothness );
			o.Alpha = 1;
			clip( tex2DNode1.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17005
7;29;1906;1004;1959.866;417.9674;1.311528;True;False
Node;AmplifyShaderEditor.Vector3Node;14;-689.4544,-756.6972;Inherit;False;Constant;_Vector0;Vector 0;7;0;Create;True;0;0;False;0;1,1,-1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;11;-812.7459,-959.1484;Inherit;True;Property;_Normal;Normal;3;0;Create;True;0;0;False;0;None;79fe5b502b8bb5f4092615a23b26efde;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-692.2203,-400.368;Inherit;False;Property;_Metallic;Metallic;5;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-691.2203,-313.368;Inherit;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;15;-679.814,313.8652;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;17;-660.1412,585.3518;Inherit;False;Constant;_Vector1;Vector 1;7;0;Create;True;0;0;False;0;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-473.1809,-799.7695;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;4;-640.9451,-1200.579;Inherit;False;Property;_AlbedoColorTint;Albedo Color Tint;2;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-704.9451,-1393.58;Inherit;True;Property;_AlbedoTransparency;Albedo Transparency;1;0;Create;True;0;0;False;0;None;cc96607e06401ae4fb7747500be837e3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;-719.2203,-598.3677;Inherit;True;Property;_MetallicSmoothness;MetallicSmoothness;4;0;Create;True;0;0;False;0;None;22ccf805641de5b44a7365d605421d91;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-403.0819,458.1332;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-269.2204,-584.3679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;2;-278.945,-1250.579;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SwitchByFaceNode;12;-314.0211,-957.3549;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-268.2204,-480.3681;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;2;ASEMaterialInspector;0;0;Standard;SineVFX/GoldEffects/CoinParticle;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;13;0;11;0
WireConnection;13;1;14;0
WireConnection;16;0;15;3
WireConnection;16;1;17;0
WireConnection;7;0;6;1
WireConnection;7;1;9;0
WireConnection;2;0;1;0
WireConnection;2;1;4;0
WireConnection;12;0;11;0
WireConnection;12;1;13;0
WireConnection;8;0;6;4
WireConnection;8;1;10;0
WireConnection;0;0;2;0
WireConnection;0;1;12;0
WireConnection;0;3;7;0
WireConnection;0;4;8;0
WireConnection;0;10;1;4
WireConnection;0;11;16;0
ASEEND*/
//CHKSM=8AAA60C5A999BBA1160C5A741F8C0F78E1ABD12C