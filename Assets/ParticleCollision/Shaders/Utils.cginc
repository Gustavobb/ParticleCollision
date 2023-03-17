float2 Random(float2 p)
{
	float3 a = frac(p.xyx * float3(123.34, 234.34, 345.65));
	a += dot(a, a + 34.45);
	return frac(float2(a.x * a.y, a.y * a.z));
}

//  function from Iñigo Quiles
//  https://www.shadertoy.com/view/MsS3Wc
//  via: https://thebookofshaders.com/06/
float4 hsb2rgb(float3 c) 
{
	float3 rgb = clamp(abs(((c.x * 6.0 + float3(0.0, 4.0, 2.0)) % 6.0) - 3.0) - 1.0, 0.0, 1.0);
	rgb *= rgb * (3.0 - 2.0 * rgb);
	float3 o = c.z * lerp(float3(1.0, 1.0, 1.0), rgb, c.y);
	return float4(o.r, o.g, o.b, 1);
}

// http://www.chilliant.com/rgb2hsv.html
float4 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float4(R, G, B, 1));
}

float random (float2 st)
{
    return frac(sin(dot(st.xy, float2(12.9898,78.233)))* 43758.5453123);
}