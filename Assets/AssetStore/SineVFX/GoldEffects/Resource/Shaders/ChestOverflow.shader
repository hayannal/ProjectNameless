// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SineVFX/GoldEffects/ChestOverflow"
{
	Properties
	{
		_FinalPower("Final Power", Range( 0 , 40)) = 4
		_SingleColor("Single Color", Color) = (1,0.3103448,0,1)
		[Toggle(_RAMPENABLED_ON)] _RampEnabled("Ramp Enabled", Float) = 1
		[Toggle(_RAMPNOISEORMASK_ON)] _RampNoiseOrMask("Ramp Noise Or Mask", Float) = 1
		_Ramp("Ramp", 2D) = "white" {}
		_RampColorTint("Ramp Color Tint", Color) = (1,1,1,1)
		_Mask("Mask", 2D) = "white" {}
		_MaskPower("Mask Power", Range( 0 , 1)) = 1
		_Noise01("Noise 01", 2D) = "white" {}
		_Noise01Negate("Noise 01 Negate", Range( 0 , 1)) = 0
		_Noise01Power("Noise 01 Power", Range( 0 , 1)) = 1
		_Noise01ScrollSpeed("Noise 01 Scroll Speed", Float) = 0.25
		_Noise01DistortionPower("Noise 01 Distortion Power", Range( 0 , 0.6)) = 0.1
		[Toggle(_MULTIPLYNOISES_ON)] _MultiplyNoises("Multiply Noises", Float) = 0
		[Toggle(_NOISE02ENABLED_ON)] _Noise02Enabled("Noise 02 Enabled", Float) = 0
		_Noise02("Noise 02", 2D) = "white" {}
		_Noise02Negate("Noise 02 Negate", Range( 0 , 1)) = 0
		_Noise02Power("Noise 02 Power", Range( 0 , 1)) = 1
		_Noise02ScrollSpeed("Noise 02 Scroll Speed", Float) = 0.25
		_Noise02DistortionPower("Noise 02 Distortion Power", Range( 0 , 0.6)) = 0
		_NoiseDistortion("Noise Distortion", 2D) = "white" {}
		_NoiseDistortionScrollSpeed("Noise Distortion Scroll Speed", Float) = 0.25
		[Toggle(_WAVEENABLED_ON)] _WaveEnabled("Wave Enabled", Float) = 0
		_Wave("Wave", 2D) = "white" {}
		_WavePower("Wave Power", Range( 0 , 1)) = 0.25
		_WaveDistortionPower("Wave Distortion Power", Range( 0 , 1)) = 0.25
		_AppearMask("Appear Mask", 2D) = "white" {}
		_AppearMaskEmissionPower("Appear Mask Emission Power", Range( 0 , 100)) = 1
		_AppearMaskDistortion("Appear Mask Distortion", 2D) = "white" {}
		_AppearMaskDistortionPower("Appear Mask Distortion Power", Range( 0 , 1)) = 0.4
		_AppearMaskDistortionScrollSpeed("Appear Mask Distortion Scroll Speed", Float) = 0.25
		_MaskAppearProgress("Mask Appear Progress", Range( -4 , 4)) = -0.04
		[Toggle(_ADDITIONALMASKENABLED_ON)] _AdditionalMaskEnabled("Additional Mask Enabled", Float) = 0
		_AdditionalMask("Additional Mask", 2D) = "white" {}
		_AdditionalMaskAdd("Additional Mask Add", Float) = -0.25
		_AdditionalMaskMultiply("Additional Mask Multiply", Float) = 1
		_AdditionalMaskScrollSpeed("Additional Mask Scroll Speed", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature _RAMPENABLED_ON
		#pragma shader_feature _RAMPNOISEORMASK_ON
		#pragma shader_feature _MULTIPLYNOISES_ON
		#pragma shader_feature _NOISE02ENABLED_ON
		#pragma shader_feature _WAVEENABLED_ON
		#pragma shader_feature _ADDITIONALMASKENABLED_ON
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _SingleColor;
		uniform sampler2D _Ramp;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float _MaskPower;
		uniform sampler2D _Noise01;
		uniform float4 _Noise01_ST;
		uniform float _Noise01ScrollSpeed;
		uniform sampler2D _NoiseDistortion;
		uniform float4 _NoiseDistortion_ST;
		uniform float _NoiseDistortionScrollSpeed;
		uniform float _Noise01DistortionPower;
		uniform float _Noise01Negate;
		uniform float _Noise01Power;
		uniform sampler2D _Noise02;
		uniform float4 _Noise02_ST;
		uniform float _Noise02ScrollSpeed;
		uniform float _Noise02DistortionPower;
		uniform float _Noise02Negate;
		uniform float _Noise02Power;
		uniform float _AppearMaskEmissionPower;
		uniform sampler2D _AppearMask;
		uniform sampler2D _AppearMaskDistortion;
		uniform float4 _AppearMaskDistortion_ST;
		uniform float _AppearMaskDistortionScrollSpeed;
		uniform float _AppearMaskDistortionPower;
		uniform float4 _AppearMask_ST;
		uniform float _MaskAppearProgress;
		uniform sampler2D _Wave;
		uniform float _WaveDistortionPower;
		uniform float4 _Wave_ST;
		uniform float _WavePower;
		uniform sampler2D _AdditionalMask;
		uniform float4 _AdditionalMask_ST;
		uniform float _AdditionalMaskScrollSpeed;
		uniform float _AdditionalMaskAdd;
		uniform float _AdditionalMaskMultiply;
		uniform float _FinalPower;
		uniform float4 _RampColorTint;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float lerpResult132 = lerp( 1.0 , tex2D( _Mask, uv_Mask ).r , _MaskPower);
			float2 uv0_Noise01 = i.uv_texcoord * _Noise01_ST.xy + _Noise01_ST.zw;
			float2 appendResult15 = (float2(uv0_Noise01.x , ( uv0_Noise01.y + ( _Time.y * _Noise01ScrollSpeed ) )));
			float2 uv0_NoiseDistortion = i.uv_texcoord * _NoiseDistortion_ST.xy + _NoiseDistortion_ST.zw;
			float2 appendResult28 = (float2(uv0_NoiseDistortion.x , ( uv0_NoiseDistortion.y + ( _Time.y * _NoiseDistortionScrollSpeed ) )));
			float Distortion122 = tex2D( _NoiseDistortion, appendResult28 ).r;
			float temp_output_20_0 = ( Distortion122 * _Noise01DistortionPower );
			float clampResult114 = clamp( ( tex2D( _Noise01, ( appendResult15 + temp_output_20_0 ) ).r + _Noise01Negate ) , 0.0 , 1.0 );
			float temp_output_109_0 = ( clampResult114 * _Noise01Power );
			float2 uv0_Noise02 = i.uv_texcoord * _Noise02_ST.xy + _Noise02_ST.zw;
			float2 appendResult98 = (float2(uv0_Noise02.x , ( uv0_Noise02.y + ( _Time.y * _Noise02ScrollSpeed ) )));
			float temp_output_102_0 = ( Distortion122 * _Noise02DistortionPower );
			float clampResult118 = clamp( ( tex2D( _Noise02, ( appendResult98 + temp_output_102_0 ) ).r + _Noise02Negate ) , 0.0 , 1.0 );
			#ifdef _NOISE02ENABLED_ON
				float staticSwitch105 = ( clampResult118 * _Noise02Power );
			#else
				float staticSwitch105 = 0.0;
			#endif
			#ifdef _MULTIPLYNOISES_ON
				float staticSwitch104 = ( temp_output_109_0 * staticSwitch105 );
			#else
				float staticSwitch104 = ( temp_output_109_0 + staticSwitch105 );
			#endif
			float2 uv0_AppearMaskDistortion = i.uv_texcoord * _AppearMaskDistortion_ST.xy + _AppearMaskDistortion_ST.zw;
			float2 appendResult68 = (float2(uv0_AppearMaskDistortion.x , ( uv0_AppearMaskDistortion.y + ( _Time.y * _AppearMaskDistortionScrollSpeed ) )));
			float2 uv0_AppearMask = i.uv_texcoord * _AppearMask_ST.xy + _AppearMask_ST.zw;
			float2 appendResult54 = (float2(( uv0_AppearMask.y + _MaskAppearProgress ) , uv0_AppearMask.x));
			float4 tex2DNode50 = tex2D( _AppearMask, ( ( tex2D( _AppearMaskDistortion, appendResult68 ).r * _AppearMaskDistortionPower ) + appendResult54 ) );
			float2 uv0_Wave = i.uv_texcoord * _Wave_ST.xy + _Wave_ST.zw;
			float2 appendResult39 = (float2(( uv0_Wave.y + _Time.y ) , uv0_Wave.x));
			#ifdef _WAVEENABLED_ON
				float staticSwitch120 = ( tex2D( _Wave, ( ( Distortion122 * _WaveDistortionPower ) + appendResult39 ) ).r * _WavePower );
			#else
				float staticSwitch120 = 0.0;
			#endif
			float2 uv0_AdditionalMask = i.uv_texcoord * _AdditionalMask_ST.xy + _AdditionalMask_ST.zw;
			float2 appendResult77 = (float2(uv0_AdditionalMask.x , ( uv0_AdditionalMask.y + ( _Time.y * _AdditionalMaskScrollSpeed ) )));
			float clampResult72 = clamp( ( ( tex2D( _AdditionalMask, appendResult77 ).r + _AdditionalMaskAdd ) * _AdditionalMaskMultiply ) , 0.0 , 1.0 );
			#ifdef _ADDITIONALMASKENABLED_ON
				float staticSwitch81 = clampResult72;
			#else
				float staticSwitch81 = 1.0;
			#endif
			float temp_output_16_0 = ( ( staticSwitch104 + ( _AppearMaskEmissionPower * tex2DNode50.r ) + staticSwitch120 ) * lerpResult132 * tex2DNode50.g * staticSwitch81 );
			#ifdef _RAMPNOISEORMASK_ON
				float staticSwitch84 = temp_output_16_0;
			#else
				float staticSwitch84 = lerpResult132;
			#endif
			float2 appendResult29 = (float2(staticSwitch84 , 0.0));
			#ifdef _RAMPENABLED_ON
				float4 staticSwitch33 = tex2D( _Ramp, appendResult29 );
			#else
				float4 staticSwitch33 = _SingleColor;
			#endif
			o.Emission = ( staticSwitch33 * _FinalPower * _RampColorTint ).rgb;
			float clampResult43 = clamp( temp_output_16_0 , 0.0 , 1.0 );
			o.Alpha = clampResult43;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17005
7;29;1906;1004;1391.132;84.2746;1;True;False
Node;AmplifyShaderEditor.TimeNode;24;-4774.112,-573.2161;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;27;-4862.914,-427.849;Inherit;False;Property;_NoiseDistortionScrollSpeed;Noise Distortion Scroll Speed;21;0;Create;True;0;0;False;0;0.25;0.0625;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-4619.815,-772.3497;Inherit;False;0;19;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-4521.111,-503.2159;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;23;-4345.509,-622.8495;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;28;-4229.812,-702.1497;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;19;-4085.798,-728.071;Inherit;True;Property;_NoiseDistortion;Noise Distortion;20;0;Create;True;0;0;False;0;None;be9b4b580ae423f45ad26f55da3b2187;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;90;-3163.422,-66.86253;Inherit;False;Property;_Noise02ScrollSpeed;Noise 02 Scroll Speed;18;0;Create;True;0;0;False;0;0.25;0.025;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;92;-3135.422,-202.8629;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;122;-3791.313,-708.3348;Inherit;False;Distortion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;11;-3163.879,-1059.057;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-3191.879,-923.0569;Inherit;False;Property;_Noise01ScrollSpeed;Noise 01 Scroll Speed;11;0;Create;True;0;0;False;0;0.25;0.075;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;-2882.423,-132.8626;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;101;-3090.423,-389.8631;Inherit;False;0;85;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-2692.423,-217.8628;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-3118.879,-1246.057;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-2910.879,-989.0569;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-2902.925,-653.8842;Inherit;False;122;Distortion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-2980.807,-568.2561;Inherit;False;Property;_Noise02DistortionPower;Noise 02 Distortion Power;19;0;Create;True;0;0;False;0;0;0;0;0.6;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;98;-2582.423,-337.863;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;123;-2909.036,-839.0817;Inherit;False;122;Distortion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-2989.499,-760.9009;Inherit;False;Property;_Noise01DistortionPower;Noise 01 Distortion Power;12;0;Create;True;0;0;False;0;0.1;0.172;0;0.6;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;8;-2720.878,-1074.057;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-2124.129,1381.399;Inherit;False;Property;_AppearMaskDistortionScrollSpeed;Appear Mask Distortion Scroll Speed;30;0;Create;True;0;0;False;0;0.25;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;-2576.906,-616.7012;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;63;-2035.327,1236.032;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-2574.867,-827.3772;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-15.91922,1549.733;Inherit;False;Property;_AdditionalMaskScrollSpeed;Additional Mask Scroll Speed;36;0;Create;True;0;0;False;0;0;0.025;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;-1782.33,1306.032;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;100;-2338.508,-251.5344;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TimeNode;78;74.14554,1399.714;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;66;-1881.034,1036.9;Inherit;False;0;62;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;15;-2610.878,-1194.057;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;75;207.7263,1229.991;Inherit;False;0;69;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;321.1456,1475.714;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;67;-1606.729,1186.398;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2174.132,-72.51593;Inherit;False;Property;_Noise02Negate;Noise 02 Negate;16;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;44;-1845.84,2200.974;Inherit;False;0;36;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;49;-1819.826,2394.179;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;85;-2190.408,-274.2695;Inherit;True;Property;_Noise02;Noise 02;15;0;Create;True;0;0;False;0;None;7a9be9e0e7c4fcd4696d2486d10fea1a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-2366.963,-1107.728;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;117;-1873.83,-170.0159;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;-1570.01,1889.688;Inherit;False;122;Distortion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-1312.836,1634.217;Inherit;False;Property;_MaskAppearProgress;Mask Appear Progress;31;0;Create;True;0;0;False;0;-0.04;-0.5;-4;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-2186.567,-922.3667;Inherit;False;Property;_Noise01Negate;Noise 01 Negate;9;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;40;-1551.859,2333.193;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;76;487.0607,1396.474;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-1325.717,1460.957;Inherit;False;0;50;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;127;-1655.01,2019.688;Inherit;False;Property;_WaveDistortionPower;Wave Distortion Power;25;0;Create;True;0;0;False;0;0.25;0.6;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-2215.301,-1137.349;Inherit;True;Property;_Noise01;Noise 01;8;0;Create;True;0;0;False;0;None;853386e2b0a0e35459f9cab919a402e1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;68;-1491.032,1107.099;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;59;-1242.638,1345.059;Inherit;False;Property;_AppearMaskDistortionPower;Appear Mask Distortion Power;29;0;Create;True;0;0;False;0;0.4;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;118;-1751.631,-171.316;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;-1031.736,1593.177;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;113;-1837.868,-1027.367;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;39;-1384.474,2209.412;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;62;-1259.362,1059.552;Inherit;True;Property;_AppearMaskDistortion;Appear Mask Distortion;28;0;Create;True;0;0;False;0;None;be9b4b580ae423f45ad26f55da3b2187;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;77;630.064,1259.687;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-1349.01,1944.688;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-1876.43,-54.3159;Inherit;False;Property;_Noise02Power;Noise 02 Power;17;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;47;-1139.724,2089.851;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;69;770.4161,1232.341;Inherit;True;Property;_AdditionalMask;Additional Mask;33;0;Create;True;0;0;False;0;None;c24d1638ee9e50a45a3a84ddefd020cc;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;71;829.6265,1427.794;Inherit;False;Property;_AdditionalMaskAdd;Additional Mask Add;34;0;Create;True;0;0;False;0;-0.25;-0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;-864.3515,1469.396;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-1847.979,-902.3278;Inherit;False;Property;_Noise01Power;Noise 01 Power;10;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;114;-1714.868,-1028.367;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;110;-1571.115,-120.7569;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-901.0053,1209.642;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1103.128,-28.08077;Inherit;False;Constant;_Float2;Float 2;30;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;-1515.66,-968.7227;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;57;-663.748,1369.709;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;105;-898.8578,-123.3267;Inherit;False;Property;_Noise02Enabled;Noise 02 Enabled;14;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;36;-937.1718,2060.313;Inherit;True;Property;_Wave;Wave;23;0;Create;True;0;0;False;0;None;014dca088ec93b54486621ab84890b5d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;46;-914.6676,2255.83;Inherit;False;Property;_WavePower;Wave Power;24;0;Create;True;0;0;False;0;0.25;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;1050.752,1563.434;Inherit;False;Property;_AdditionalMaskMultiply;Additional Mask Multiply;35;0;Create;True;0;0;False;0;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;70;1118.31,1331.171;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;108;-513.3102,-405.0321;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-538.233,2035.332;Inherit;False;Constant;_Float3;Float 3;35;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-533.4778,2117.983;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;1385.233,1370.574;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;107;-539.2408,-233.8894;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;50;-528.5922,1343.577;Inherit;True;Property;_AppearMask;Appear Mask;26;0;Create;True;0;0;False;0;None;77774541f347fd84397b003878ac0815;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;61;-664.3767,617.8666;Inherit;False;Property;_AppearMaskEmissionPower;Appear Mask Emission Power;27;0;Create;True;0;0;False;0;1;2;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;120;-355.2747,2067.295;Inherit;False;Property;_WaveEnabled;Wave Enabled;22;0;Create;True;0;0;False;0;0;0;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;131;-893.1322,469.7253;Inherit;False;Property;_MaskPower;Mask Power;7;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;17;-913.3826,274.8285;Inherit;True;Property;_Mask;Mask;6;0;Create;True;0;0;False;0;None;071ef51bc75245747ad2e564ac4f3152;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;82;1510.679,1165.206;Inherit;False;Constant;_Float1;Float 1;24;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-316.5767,620.0667;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;72;1532.864,1371.682;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;104;-360.3774,-326.4424;Inherit;False;Property;_MultiplyNoises;Multiply Noises;13;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;133;-760.132,197.7254;Inherit;False;Constant;_Float4;Float 4;37;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;81;1717.422,1260.592;Inherit;False;Property;_AdditionalMaskEnabled;Additional Mask Enabled;32;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;56;-135.5446,247.2913;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;132;-514.132,345.7254;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;99.46303,314.7783;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;179.3372,-68.67076;Inherit;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;84;230.5757,139.7577;Inherit;False;Property;_RampNoiseOrMask;Ramp Noise Or Mask;3;0;Create;True;0;0;False;0;0;1;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;402.3229,-87.65938;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;35;657.0137,-374.1371;Inherit;False;Property;_SingleColor;Single Color;1;0;Create;True;0;0;False;0;1,0.3103448,0,1;1,0.6521298,0.279411,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;31;555.9224,-114.8591;Inherit;True;Property;_Ramp;Ramp;4;0;Create;True;0;0;False;0;None;ed17826310cbfb74384b61ba285b5fad;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;32;1055.63,141.8537;Inherit;False;Property;_RampColorTint;Ramp Color Tint;5;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;33;939.1133,-231.137;Inherit;False;Property;_RampEnabled;Ramp Enabled;2;0;Create;True;0;0;False;0;0;1;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;7;982.6689,55.97536;Inherit;False;Property;_FinalPower;Final Power;0;0;Create;True;0;0;False;0;4;8;0;40;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;129;-2350.918,-846.1205;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;128;-2351.375,-603.095;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;43;1601.543,134.5119;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;1387.469,-48.62471;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1800.869,-96.42478;Float;False;True;2;ASEMaterialInspector;0;0;Unlit;SineVFX/GoldEffects/ChestOverflow;False;False;False;False;True;True;True;True;True;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;26;0;24;2
WireConnection;26;1;27;0
WireConnection;23;0;22;2
WireConnection;23;1;26;0
WireConnection;28;0;22;1
WireConnection;28;1;23;0
WireConnection;19;1;28;0
WireConnection;122;0;19;1
WireConnection;93;0;92;2
WireConnection;93;1;90;0
WireConnection;96;0;101;2
WireConnection;96;1;93;0
WireConnection;13;0;11;2
WireConnection;13;1;14;0
WireConnection;98;0;101;1
WireConnection;98;1;96;0
WireConnection;8;0;4;2
WireConnection;8;1;13;0
WireConnection;102;0;124;0
WireConnection;102;1;103;0
WireConnection;20;0;123;0
WireConnection;20;1;21;0
WireConnection;65;0;63;2
WireConnection;65;1;64;0
WireConnection;100;0;98;0
WireConnection;100;1;102;0
WireConnection;15;0;4;1
WireConnection;15;1;8;0
WireConnection;79;0;78;2
WireConnection;79;1;80;0
WireConnection;67;0;66;2
WireConnection;67;1;65;0
WireConnection;85;1;100;0
WireConnection;18;0;15;0
WireConnection;18;1;20;0
WireConnection;117;0;85;1
WireConnection;117;1;116;0
WireConnection;40;0;44;2
WireConnection;40;1;49;2
WireConnection;76;0;75;2
WireConnection;76;1;79;0
WireConnection;1;1;18;0
WireConnection;68;0;66;1
WireConnection;68;1;67;0
WireConnection;118;0;117;0
WireConnection;53;0;52;2
WireConnection;53;1;55;0
WireConnection;113;0;1;1
WireConnection;113;1;112;0
WireConnection;39;0;40;0
WireConnection;39;1;44;1
WireConnection;62;1;68;0
WireConnection;77;0;75;1
WireConnection;77;1;76;0
WireConnection;126;0;125;0
WireConnection;126;1;127;0
WireConnection;47;0;126;0
WireConnection;47;1;39;0
WireConnection;69;1;77;0
WireConnection;54;0;53;0
WireConnection;54;1;52;1
WireConnection;114;0;113;0
WireConnection;110;0;118;0
WireConnection;110;1;119;0
WireConnection;58;0;62;1
WireConnection;58;1;59;0
WireConnection;109;0;114;0
WireConnection;109;1;111;0
WireConnection;57;0;58;0
WireConnection;57;1;54;0
WireConnection;105;1;106;0
WireConnection;105;0;110;0
WireConnection;36;1;47;0
WireConnection;70;0;69;1
WireConnection;70;1;71;0
WireConnection;108;0;109;0
WireConnection;108;1;105;0
WireConnection;45;0;36;1
WireConnection;45;1;46;0
WireConnection;73;0;70;0
WireConnection;73;1;74;0
WireConnection;107;0;109;0
WireConnection;107;1;105;0
WireConnection;50;1;57;0
WireConnection;120;1;121;0
WireConnection;120;0;45;0
WireConnection;60;0;61;0
WireConnection;60;1;50;1
WireConnection;72;0;73;0
WireConnection;104;1;108;0
WireConnection;104;0;107;0
WireConnection;81;1;82;0
WireConnection;81;0;72;0
WireConnection;56;0;104;0
WireConnection;56;1;60;0
WireConnection;56;2;120;0
WireConnection;132;0;133;0
WireConnection;132;1;17;1
WireConnection;132;2;131;0
WireConnection;16;0;56;0
WireConnection;16;1;132;0
WireConnection;16;2;50;2
WireConnection;16;3;81;0
WireConnection;84;1;132;0
WireConnection;84;0;16;0
WireConnection;29;0;84;0
WireConnection;29;1;30;0
WireConnection;31;1;29;0
WireConnection;33;1;35;0
WireConnection;33;0;31;0
WireConnection;129;0;20;0
WireConnection;128;0;102;0
WireConnection;43;0;16;0
WireConnection;6;0;33;0
WireConnection;6;1;7;0
WireConnection;6;2;32;0
WireConnection;0;2;6;0
WireConnection;0;9;43;0
ASEEND*/
//CHKSM=457DE5AC28085F43E4BC8AF9BDCE2D2795C1DC01