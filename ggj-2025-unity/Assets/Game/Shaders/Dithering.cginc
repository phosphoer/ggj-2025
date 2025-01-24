sampler2D _GlobalDitherTex;
float2 _GlobalDitherTex_TexelSize;

float Dither(float inputVal, float2 screenPos)
{
    //value from the dither pattern
    float2 ditherCoordinate = screenPos * _ScreenParams.xy * _GlobalDitherTex_TexelSize.xy;
    float ditherValue = tex2D(_GlobalDitherTex, ditherCoordinate).r;

    //combine dither pattern with texture value to get final result
    float ditheredValue = step(ditherValue, inputVal);
    return ditheredValue;
}
