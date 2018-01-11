Shader "Custom/HeightDependentTint"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_HeightMin("Height Min", Float) = -1
		_HeightMax("Height Max", Float) = 1
		_ColorMin("Tint Color At Min", Color) = (0,0,0,1)
		_ColorMax("Tint Color At Max", Color) = (1,1,1,1)
		_MinX("Min X",Float) = -1
		_MaxX("Max X",Float) = -1
		_MinZ("Min Z",Float) = -1
		_MaxZ("Max Z",Float) = -1
		_ScaleFactor("Scale Factor",Float) = 1
	}

		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		CGPROGRAM
#pragma surface surf Lambert

		sampler2D _MainTex;
	fixed4 _ColorMin;
	fixed4 _ColorMax;
	float _HeightMin;
	float _HeightMax;

	float _MinX;
	float _MaxX;
	float _MinZ;
	float _MaxZ;
	float _ScaleFactor;


	struct Input
	{
		float2 uv_MainTex;
		float3 worldPos;
	};

	void surf(Input IN, inout SurfaceOutput o)
	{
		if (IN.worldPos.x < _MinX || IN.worldPos.x > _MaxX
			|| IN.worldPos.z < _MinZ || IN.worldPos.z > _MaxZ
			|| IN.worldPos.y < _HeightMin || IN.worldPos.y> _HeightMax
			)
		discard;

		float h = (_HeightMax - IN.worldPos.y) / (_HeightMax - _HeightMin);
		
		half4 c = tex2D(_MainTex, float2(h*_ScaleFactor,.5f));
		if (h < 0 || h > 1) c = half4(0, 0, 0, 0);
		o.Albedo = c.rgb;// *tintColor.rgb;
		o.Alpha = c.a;// *tintColor.a;
	}
	ENDCG
	}
		Fallback "Diffuse"
}