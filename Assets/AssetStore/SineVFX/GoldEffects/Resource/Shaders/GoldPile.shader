// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SineVFX/GoldEffects/GoldPile"
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
		_YAxisMask("Y Axis Mask", Float) = 0
		_YAxisMove("Y Axis Move", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha nolightmap noforwardadd interpolateview approxview vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform float _YAxisMove;
		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform sampler2D _AlbedoTransparency;
		uniform float4 _AlbedoTransparency_ST;
		uniform float4 _AlbedoColorTint;
		uniform sampler2D _MetallicSmoothness;
		uniform float4 _MetallicSmoothness_ST;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _YAxisMask;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			v.vertex.xyz += ( float3(0,1,0) * _YAxisMove );
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			o.Normal = UnpackNormal( tex2D( _Normal, uv_Normal ) );
			float2 uv_AlbedoTransparency = i.uv_texcoord * _AlbedoTransparency_ST.xy + _AlbedoTransparency_ST.zw;
			float4 tex2DNode1 = tex2D( _AlbedoTransparency, uv_AlbedoTransparency );
			o.Albedo = ( tex2DNode1 * _AlbedoColorTint ).rgb;
			float2 uv_MetallicSmoothness = i.uv_texcoord * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
			float4 tex2DNode6 = tex2D( _MetallicSmoothness, uv_MetallicSmoothness );
			o.Metallic = ( tex2DNode6.r * _Metallic );
			o.Smoothness = ( tex2DNode6.a * _Smoothness );
			o.Alpha = 1;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float ifLocalVar14 = 0;
			if( ( ase_vertex3Pos.y + _YAxisMask ) >= 0.0 )
				ifLocalVar14 = 1.0;
			else
				ifLocalVar14 = 0.0;
			float clampResult20 = clamp( ( tex2DNode1.a * ifLocalVar14 ) , 0.0 , 1.0 );
			clip( clampResult20 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17005
86;279;1378;754;1335.63;12.83811;1.150706;True;False
Node;AmplifyShaderEditor.RangedFloatNode;19;-1095.132,126.3976;Inherit;False;Property;_YAxisMask;Y Axis Mask;7;0;Create;True;0;0;False;0;0;0.44;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;13;-1119.295,-10.53655;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-899.5114,347.3337;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-898.3607,425.5818;Inherit;False;Constant;_Float2;Float 2;7;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-861.5382,72.31453;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-902.9635,271.387;Inherit;False;Constant;_Float0;Float 0;7;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-704.9451,-1393.58;Inherit;True;Property;_AlbedoTransparency;Albedo Transparency;1;0;Create;True;0;0;False;0;None;220b717d0cd6a494796482555f6f6145;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ConditionalIfNode;14;-632.5479,229.9613;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-691.2203,-313.368;Inherit;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;24;-556.6016,422.1289;Inherit;False;Constant;_Vector0;Vector 0;9;0;Create;True;0;0;False;0;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;22;-562.3553,571.7211;Inherit;False;Property;_YAxisMove;Y Axis Move;8;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;-719.2203,-598.3677;Inherit;True;Property;_MetallicSmoothness;MetallicSmoothness;4;0;Create;True;0;0;False;0;None;21617f59b0f917f478e9fc1498bb3ecf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;4;-640.9451,-1200.579;Inherit;False;Property;_AlbedoColorTint;Albedo Color Tint;2;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-692.2203,-400.368;Inherit;False;Property;_Metallic;Metallic;5;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-445.2192,185.0913;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;2;-278.945,-1250.579;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-310.3506,531.4457;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-268.2204,-480.3681;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-709.804,-797.8727;Inherit;True;Property;_Normal;Normal;3;0;Create;True;0;0;False;0;None;e191700781839b442a713636c271424d;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-269.2204,-584.3679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;20;-302.2962,189.6862;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;2;ASEMaterialInspector;0;0;Standard;SineVFX/GoldEffects/GoldPile;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;18;0;13;2
WireConnection;18;1;19;0
WireConnection;14;0;18;0
WireConnection;14;1;15;0
WireConnection;14;2;16;0
WireConnection;14;3;16;0
WireConnection;14;4;17;0
WireConnection;12;0;1;4
WireConnection;12;1;14;0
WireConnection;2;0;1;0
WireConnection;2;1;4;0
WireConnection;23;0;24;0
WireConnection;23;1;22;0
WireConnection;8;0;6;4
WireConnection;8;1;10;0
WireConnection;7;0;6;1
WireConnection;7;1;9;0
WireConnection;20;0;12;0
WireConnection;0;0;2;0
WireConnection;0;1;11;0
WireConnection;0;3;7;0
WireConnection;0;4;8;0
WireConnection;0;10;20;0
WireConnection;0;11;23;0
ASEEND*/
//CHKSM=135413E9F7FCB9D0F3B99CB3CBED8D66E69D97F6