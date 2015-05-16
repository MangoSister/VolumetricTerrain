Shader "PCGTerrain/Triplanar" {
	Properties {
		_ColorTex ("Color Tex", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}

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
			//weird here, must use a flat float3(0,0,1)
			fixed3 blend_weight = abs(WorldNormalVector(IN,fixed3(0,0,1)));		
			blend_weight /= (blend_weight.x + blend_weight.y + blend_weight.z);
			
			fixed4 colorXY = tex2D(_ColorTex, IN.worldPos.xy) * blend_weight.z;
			fixed4 colorYZ = tex2D(_ColorTex, IN.worldPos.yz) * blend_weight.x;
			fixed4 colorXZ = tex2D(_ColorTex, IN.worldPos.xz) * blend_weight.y;
			o.Albedo = colorXY + colorYZ + colorXZ;

			fixed3 normalXY = UnpackNormal(tex2D(_NormalMap, IN.worldPos.xy)) * blend_weight.z;
			fixed3 normalYZ = UnpackNormal(tex2D(_NormalMap, IN.worldPos.yz)) * blend_weight.x;
			fixed3 normalXZ = UnpackNormal(tex2D(_NormalMap, IN.worldPos.xz)) * blend_weight.y;
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
