Shader "PCGTerrain/Triplanar" {
	Properties {
		_ColorXY ("Color XY-plane", 2D) = "white" {}
		_NormalXY ("Normal XY-plane", 2D) = "bump" {}

		_ColorYZ ("Color YZ-plane", 2D) = "white" {}
		_NormalYZ ("Normal YZ-plane", 2D) = "bump" {}

		_ColorXZ ("Color XZ-plane", 2D) = "white" {}
		_NormalXZ ("Normal XZ-plane", 2D) = "bump" {}

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

		uniform sampler2D _ColorXY;
		uniform sampler2D _NormalXY;

		uniform sampler2D _ColorYZ;
		uniform sampler2D _NormalYZ;

		uniform sampler2D _ColorXZ;
		uniform sampler2D _NormalXZ;

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
			float3 blend_weight = abs(WorldNormalVector(IN,float3(0,0,1)));		
			blend_weight /= (blend_weight.x + blend_weight.y + blend_weight.z);
			
			float4 colorXY = tex2D(_ColorXY, IN.worldPos.xy) * blend_weight.z;
			float4 colorYZ = tex2D(_ColorYZ, IN.worldPos.yz) * blend_weight.x;
			float4 colorXZ = tex2D(_ColorXZ, IN.worldPos.xz) * blend_weight.y;
			o.Albedo = colorXY + colorYZ + colorXZ;

			float3 normalXY = UnpackNormal(tex2D(_NormalXY, IN.worldPos.xy)) * blend_weight.z;
			float3 normalYZ = UnpackNormal(tex2D(_NormalYZ, IN.worldPos.yz)) * blend_weight.x;
			float3 normalXZ = UnpackNormal(tex2D(_NormalXZ, IN.worldPos.xz)) * blend_weight.y;
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
