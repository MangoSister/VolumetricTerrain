//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//Triplanar.shader
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

//Basic Triplanar texturing with bump mapping
// * use world position scaled by tiling factor as uvw coordinate
// * project the normal vector of each fragment onto x/y/z axises to get weight
// * blend three samples (using xy, xz, yz components) based on weights

Shader "PCGTerrain/Triplanar" {
	Properties {
		_ColorTex ("Color Tex", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_Tile ("Tile Factor", Range(0.05,1)) = 0.2
		_Specular("Specular Color", Color) = (0,0,0)
		_Smoothness("Smoothness", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 300
		Cull Off
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma target 3.0

		uniform sampler2D _ColorTex;
		uniform sampler2D _NormalMap;
		uniform half _Tile;

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
			//subtle notification: must use a flat float3(0,0,1) here
			//get world normal vector as blending weight
			fixed3 blend_weight = abs(WorldNormalVector(IN,fixed3(0,0,1)));		
			//normalize the weight
			blend_weight /= (blend_weight.x + blend_weight.y + blend_weight.z);
			float3 scaledPos =  _Tile * IN.worldPos.xyz;
			fixed4 colorXY = tex2D(_ColorTex, scaledPos.xy) * blend_weight.z;
			fixed4 colorYZ = tex2D(_ColorTex, scaledPos.yz) * blend_weight.x;
			fixed4 colorXZ = tex2D(_ColorTex, scaledPos.xz) * blend_weight.y;
			o.Albedo = colorXY + colorYZ + colorXZ;
			//apply normal map
			fixed3 normalXY = UnpackNormal(tex2D(_NormalMap, scaledPos.xy)) * blend_weight.z;
			fixed3 normalYZ = UnpackNormal(tex2D(_NormalMap, scaledPos.yz)) * blend_weight.x;
			fixed3 normalXZ = UnpackNormal(tex2D(_NormalMap, scaledPos.xz)) * blend_weight.y;
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
