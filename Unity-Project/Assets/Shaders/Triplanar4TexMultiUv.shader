//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//Triplanar4TexMultiUv.shader
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

// Triplanar texturing with bump mapping (up to 4 textures with their normal maps)
// * use world position scaled by tiling factor as uvw coordinate
// * project the normal vector of each fragment onto x/y/z axises to get weight
// * blend three samples (using xy, xz, yz components) based on weights
// * use a 3D splatmap to control blending of different textures
// * apply multi-UV mixing to reduce tiling artifact

Shader "PCGTerrain/Triplanar4TexMultiUv" {
	Properties {
		_ColorTexR ("Color Tex (R)", 2D) = "white" {}
		_NormalMapR ("Normal Map (R)", 2D) = "bump" {}

		_ColorTexG ("Color Tex (G)", 2D) = "white" {}
		_NormalMapG ("Normal Map (G)", 2D) = "bump" {}

		_ColorTexB ("Color Tex (B)", 2D) = "white" {}
		_NormalMapB ("Normal Map (B)", 2D) = "bump" {}

		_ColorTexA ("Color Tex (A)", 2D) = "white" {}
		_NormalMapA ("Normal Map (A)", 2D) = "bump" {}

		_MatControl("Mat Blend Weight",3D) = "red" {}
		_Offset("Terrain Origin Offset",Vector) = (0,0,0,1)
		_Scale("Terrain Scale (inv real world size)",Vector) = (1,1,1,1) //_Scale = 1 / (RealWorld Size)

		_UvOctave("UV Scale Octave",Vector) = (0.25,0.25,0.25,0.25)
		_BrightnessComp("UV Scale Brightness Compensate",Vector) = (1.5,1.5,1.5,1.5)
		_Desat("Saturation after modulation",Vector) = (0.9,0.9,0.9,0.9)

		_Specular("Specular Color", Color) = (0,0,0)
		_Smoothness("Smoothness", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 450
		Cull Off
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma target 3.0

		uniform sampler2D _ColorTexR;
		uniform sampler2D _NormalMapR;
		uniform sampler2D _ColorTexG;
		uniform sampler2D _NormalMapG;
		uniform sampler2D _ColorTexB;
		uniform sampler2D _NormalMapB;
		uniform sampler2D _ColorTexA;
		uniform sampler2D _NormalMapA;

		uniform sampler3D _MatControl;
		uniform half4 _Offset;
		uniform half4 _Scale;

		uniform half4 _UvOctave;
		uniform half4 _BrightnessComp;
		uniform fixed4 _Desat;

		uniform fixed3 _Specular;
		uniform fixed _Smoothness;

		struct Input 
		{
			float3 worldPos;
			fixed3 worldNormal;
			INTERNAL_DATA
		};

		inline fixed3 desaturate(fixed3 color, fixed amount)
		{
			//Desaturate the final color a little bit
			//grayscale coeffcient = 0.3, 0.59, 0.11
			fixed intensity = dot(color, fixed3(0.3, 0.59, 0.11));
			return lerp(fixed3(intensity, intensity, intensity), color, amount);
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			//the weight for blending different textures
			fixed4 splat_blend_weight = tex3D(_MatControl, (IN.worldPos - _Offset.xyz) * _Scale.xyz);
			splat_blend_weight /= (splat_blend_weight.r + splat_blend_weight.g + splat_blend_weight.b + splat_blend_weight.a);
			
			//the weight for blending three samples from the same texture (triplanar)
			//subtle notification: must use a flat float3(0,0,1) here
			//get world normal vector as blending weight
			fixed3 triplanar_blend_weight = abs(WorldNormalVector(IN,fixed3(0,0,1)));
			triplanar_blend_weight /= (triplanar_blend_weight.x + triplanar_blend_weight.y + triplanar_blend_weight.z);			
			
			//clamp the uv scale
			_UvOctave = clamp(_UvOctave, half4(0.125,0.125,0.125,0.125), half4(1,1,1,1));

			o.Albedo = fixed4(0,0,0,0);

			//multiply two samples sampled by different uv scales -> desaturate the result color -> compensate the brightness  
			//Color XY plane
			o.Albedo += triplanar_blend_weight.z * (
								splat_blend_weight.r * desaturate( tex2D(_ColorTexR, IN.worldPos.xy) * tex2D(_ColorTexR, IN.worldPos.xy * -_UvOctave.r), _Desat.r ) * _BrightnessComp.r + 
								splat_blend_weight.g * desaturate( tex2D(_ColorTexG, IN.worldPos.xy) * tex2D(_ColorTexG, IN.worldPos.xy * -_UvOctave.g), _Desat.g ) * _BrightnessComp.g +
								splat_blend_weight.b * desaturate( tex2D(_ColorTexB, IN.worldPos.xy) * tex2D(_ColorTexB, IN.worldPos.xy * -_UvOctave.b), _Desat.b ) * _BrightnessComp.b +
								splat_blend_weight.a * desaturate( tex2D(_ColorTexA, IN.worldPos.xy) * tex2D(_ColorTexA, IN.worldPos.xy * -_UvOctave.a), _Desat.a ) * _BrightnessComp.a 
								);
								
			//Color YZ plane
			o.Albedo += triplanar_blend_weight.x * (
								splat_blend_weight.r * desaturate( tex2D(_ColorTexR, IN.worldPos.yz) * tex2D(_ColorTexR, IN.worldPos.yz * -_UvOctave.r), _Desat.r ) * _BrightnessComp.r +
								splat_blend_weight.g * desaturate( tex2D(_ColorTexG, IN.worldPos.yz) * tex2D(_ColorTexB, IN.worldPos.yz * -_UvOctave.g), _Desat.g ) * _BrightnessComp.g +
								splat_blend_weight.b * desaturate( tex2D(_ColorTexB, IN.worldPos.yz) * tex2D(_ColorTexG, IN.worldPos.yz * -_UvOctave.b), _Desat.b ) * _BrightnessComp.b +
								splat_blend_weight.a * desaturate( tex2D(_ColorTexA, IN.worldPos.yz) * tex2D(_ColorTexA, IN.worldPos.yz * -_UvOctave.a), _Desat.a ) * _BrightnessComp.a 
								);
			//Color XZ plane
			o.Albedo += triplanar_blend_weight.y * (
								splat_blend_weight.r * desaturate( tex2D(_ColorTexR, IN.worldPos.xz) * tex2D(_ColorTexR, IN.worldPos.xz * -_UvOctave.r), _Desat.r ) * _BrightnessComp.r +
								splat_blend_weight.g * desaturate( tex2D(_ColorTexG, IN.worldPos.xz) * tex2D(_ColorTexG, IN.worldPos.xz * -_UvOctave.g), _Desat.g ) * _BrightnessComp.g +
								splat_blend_weight.b * desaturate( tex2D(_ColorTexB, IN.worldPos.xz) * tex2D(_ColorTexB, IN.worldPos.xz * -_UvOctave.b), _Desat.b ) * _BrightnessComp.b +
								splat_blend_weight.a * desaturate( tex2D(_ColorTexA, IN.worldPos.xz) * tex2D(_ColorTexA, IN.worldPos.xz * -_UvOctave.a), _Desat.a ) * _BrightnessComp.a
								);


			fixed4 nrm = fixed4(0,0,0,0);
			//Normal XY plane
			nrm += triplanar_blend_weight.z * (
								splat_blend_weight.r * tex2D(_NormalMapR, IN.worldPos.xy) +
								splat_blend_weight.g * tex2D(_NormalMapG, IN.worldPos.xy) +
								splat_blend_weight.b * tex2D(_NormalMapB, IN.worldPos.xy) +
								splat_blend_weight.a * tex2D(_NormalMapA, IN.worldPos.xy)
								);
			//Normal YZ plane
			nrm += triplanar_blend_weight.x * (
								splat_blend_weight.r * tex2D(_NormalMapR, IN.worldPos.yz) +
								splat_blend_weight.g * tex2D(_NormalMapG, IN.worldPos.yz) +
								splat_blend_weight.b * tex2D(_NormalMapB, IN.worldPos.yz) +
								splat_blend_weight.a * tex2D(_NormalMapA, IN.worldPos.yz)
								);
			//Normal XZ plane
			nrm += triplanar_blend_weight.y * (
								splat_blend_weight.r * tex2D(_NormalMapR, IN.worldPos.xz) + 
								splat_blend_weight.g * tex2D(_NormalMapG, IN.worldPos.xz) + 
								splat_blend_weight.b * tex2D(_NormalMapB, IN.worldPos.xz) + 
								splat_blend_weight.a * tex2D(_NormalMapA, IN.worldPos.xz)
								);

			o.Normal = normalize(UnpackNormal(nrm));	
	
			o.Specular = _Specular;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}
		ENDCG
	} 
	//comment FallBack when developing
	//FallBack "Diffuse"
}
