Shader "PCGTerrain/TriplanarMultiMat" {
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

		_Specular("Specular Color", Color) = (0,0,0)
		_Smoothness("Smoothness", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 400
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

		uniform fixed3 _Specular;
		uniform fixed _Smoothness;

		struct Input 
		{
			float3 worldPos;
			fixed3 worldNormal;
			INTERNAL_DATA
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 splat_blend_weight = tex3D(_MatControl, (IN.worldPos - _Offset.xyz) * _Scale.xyz);
			splat_blend_weight /= (splat_blend_weight.r + splat_blend_weight.g + splat_blend_weight.b + splat_blend_weight.a);

			fixed3 triplanar_blend_weight = abs(WorldNormalVector(IN,fixed3(0,0,1))); //weird here, must use a flat float3(0,0,1)	
			triplanar_blend_weight /= (triplanar_blend_weight.x + triplanar_blend_weight.y + triplanar_blend_weight.z);			
			
			fixed4 colorXY = triplanar_blend_weight.z * (
								tex2D(_ColorTexR, IN.worldPos.xy) * splat_blend_weight.r + 
								tex2D(_ColorTexG, IN.worldPos.xy) * splat_blend_weight.g +
								tex2D(_ColorTexB, IN.worldPos.xy) * splat_blend_weight.b +
								tex2D(_ColorTexA, IN.worldPos.xy) * splat_blend_weight.a 
								);

			fixed4 colorYZ = triplanar_blend_weight.x * (
								tex2D(_ColorTexR, IN.worldPos.yz) * splat_blend_weight.r +
								tex2D(_ColorTexG, IN.worldPos.yz) * splat_blend_weight.g +
								tex2D(_ColorTexB, IN.worldPos.yz) * splat_blend_weight.b +
								tex2D(_ColorTexA, IN.worldPos.yz) * splat_blend_weight.a 
								);

			fixed4 colorXZ = triplanar_blend_weight.y * (
								tex2D(_ColorTexR, IN.worldPos.xz) * splat_blend_weight.r +
								tex2D(_ColorTexG, IN.worldPos.xz) * splat_blend_weight.g +
								tex2D(_ColorTexB, IN.worldPos.xz) * splat_blend_weight.b +
								tex2D(_ColorTexA, IN.worldPos.xz) * splat_blend_weight.a 
								);
			
			o.Albedo = colorXY + colorYZ + colorXZ;

			fixed3 normalXY = triplanar_blend_weight.z * (
								UnpackNormal(tex2D(_NormalMapR, IN.worldPos.xy)) * splat_blend_weight.r +
								UnpackNormal(tex2D(_NormalMapG, IN.worldPos.xy)) * splat_blend_weight.g +
								UnpackNormal(tex2D(_NormalMapB, IN.worldPos.xy)) * splat_blend_weight.b +
								UnpackNormal(tex2D(_NormalMapA, IN.worldPos.xy)) * splat_blend_weight.a
								);

			fixed3 normalYZ = triplanar_blend_weight.x * (
								UnpackNormal(tex2D(_NormalMapR, IN.worldPos.yz)) * splat_blend_weight.r +
								UnpackNormal(tex2D(_NormalMapG, IN.worldPos.yz)) * splat_blend_weight.g +
								UnpackNormal(tex2D(_NormalMapB, IN.worldPos.yz)) * splat_blend_weight.b +
								UnpackNormal(tex2D(_NormalMapA, IN.worldPos.yz)) * splat_blend_weight.a
								);

			fixed3 normalXZ = triplanar_blend_weight.y * (
								UnpackNormal(tex2D(_NormalMapR, IN.worldPos.xz)) * splat_blend_weight.r + 
								UnpackNormal(tex2D(_NormalMapG, IN.worldPos.xz)) * splat_blend_weight.g + 
								UnpackNormal(tex2D(_NormalMapB, IN.worldPos.xz)) * splat_blend_weight.b + 
								UnpackNormal(tex2D(_NormalMapA, IN.worldPos.xz)) * splat_blend_weight.a 
								);

			o.Normal = normalize(normalXY + normalYZ + normalXZ);	
	
			o.Specular = _Specular;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}
		ENDCG
	} 
	//comment FallBack when developingw
	//FallBack "Diffuse"
}
