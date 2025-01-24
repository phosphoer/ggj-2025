#include "AutoLight.cginc"

sampler2D _LightRamp;
float _CausticsAlpha;

float4 _GlobalLightDirection;
float4 _GlobalLightColor;

float _GlobalFogNearScale;
float _GlobalFogFarScale;
float _GlobalSkyboxNoiseScale;

float4 _GlobalSkyColor;
float4 _GlobalHorizonColor;
float4 _GlobalGroundColor;
sampler2D _GlobalSkyboxNoiseTex;
sampler2D _GlobalCausticsNoiseTex;

#define kMaxLights 8
float4 _GlobalLightPositions[kMaxLights];
float4 _GlobalLightDirections[kMaxLights];
float4 _GlobalLightColors[kMaxLights];
float4 _GlobalLightParams[kMaxLights]; // x = strength, y = falloff, z = spotlight angle

float InverseLerp(float a, float b, float v)
{
    return saturate((v - a) / (b - a));
}

float NDotL(float3 worldNormal, float3 lightDir)
{
    // Calculate lighting 
    float nl = max(0, dot(worldNormal, -lightDir));
    return nl;
}

float GlitterSpecular(sampler2D glitterTex, float3 worldPos)
{
    float2 uv1 = worldPos.xz * 0.03 + _Time.x * 0.03;
    float2 uv2 = worldPos.xz * 0.03 - _Time.x * 0.03;
    float3 glitterDirection1 = (tex2D(glitterTex, uv1) * 2 - 1).rgb;
    float3 glitterDirection2 = (tex2D(glitterTex, uv2) * 2 - 1).rgb;
    float3 glitterDirection = glitterDirection1 * glitterDirection2;

    if (glitterDirection.r < 0.98)
        return 0;

    return saturate(glitterDirection.r);
}

float GetLightingAmount(float3 worldPos, float3 worldNormal, out float3 totalColor)
{
    float totalLightContribution = 0;
    for (int i = 0; i < kMaxLights; ++i)
    {
        float lightStrength = _GlobalLightParams[i].x;
        float lightFalloff = _GlobalLightParams[i].y;
        float lightAngle = _GlobalLightParams[i].z;
        float4 lightColor = _GlobalLightColors[i];

        if (lightColor.a > 0)
        {
            float3 lightDir = _GlobalLightDirections[i].xyz;
            float3 lightToPoint = worldPos.xyz - _GlobalLightPositions[i].xyz;
            float3 lightToPointNormalized = normalize(lightToPoint);

            int lightType = (int)_GlobalLightDirections[i].w;
            float lightAtten = lightStrength * lightColor.a;
            switch (lightType)
            {
            // Point light
            case 0:
                lightAtten *= NDotL(worldNormal, lightToPointNormalized);
                lightAtten *= 1 - saturate(length(lightToPoint) / lightFalloff);
                break;
            // Spot light
            case 1:
                float lightDot = dot(normalize(lightDir), lightToPointNormalized);
                float cosLightAngle = cos(radians(lightAngle));
                lightAtten *= NDotL(worldNormal, lightDir);
                lightAtten *= 1 - saturate(length(lightToPoint) / lightFalloff);
                lightAtten *= max(0, lightDot - cosLightAngle);

                float spotRadiusAtten = NDotL(worldNormal, lightToPointNormalized);
                float spotRadiusFalloff = lightFalloff * 0.2;
                spotRadiusAtten *= 1 - saturate(length(lightToPoint) / spotRadiusFalloff);
                lightAtten += spotRadiusAtten * 0.1;
                break;
            // Directional light
            case 2:
                lightAtten *= NDotL(worldNormal, lightDir);
                break;
            }

            totalColor += lightColor * lightAtten;
            totalLightContribution += lightAtten;
        }
    }

    return totalLightContribution;
}

float GetFloatySpeckLighting(float3 worldPos, out float3 totalColor)
{
    float totalLightContribution = 0;
    for (int i = 0; i < kMaxLights; ++i)
    {
        float lightStrength = _GlobalLightParams[i].x;
        float lightFalloff = _GlobalLightParams[i].y;
        float lightAngle = _GlobalLightParams[i].z;
        float4 lightColor = _GlobalLightColors[i];

        if (lightColor.a > 0)
        {
            float3 lightDir = _GlobalLightDirections[i].xyz;
            float3 lightToPoint = worldPos.xyz - _GlobalLightPositions[i].xyz;
            float3 lightToPointNormalized = normalize(lightToPoint);

            int lightType = (int)_GlobalLightDirections[i].w;
            float lightAtten = lightStrength * lightColor.a;
            switch (lightType)
            {
            // Point light
            case 0:
                lightAtten *= 1 - saturate(length(lightToPoint) / lightFalloff);
                break;
            // Spot light
            case 1:
                float lightDot = dot(normalize(lightDir), lightToPointNormalized);
                float cosLightAngle = cos(radians(lightAngle));
                lightAtten *= 1 - saturate(length(lightToPoint) / lightFalloff);
                lightAtten *= max(0, lightDot - cosLightAngle);
                break;
            }

            totalColor += lightColor * lightAtten;
            totalLightContribution += lightAtten;
        }
    }

    return totalLightContribution;
}

float3 LightingAndAmbience(float3 diffuse, float3 worldPos, float3 worldNormal, float extraFade = 0)
{
    worldNormal = normalize(worldNormal);

    // Calculate global light and ambient light
    float ambient = saturate(0.75 - worldPos.y / -1200);
    float globalLight = NDotL(worldNormal, -_GlobalLightDirection.xyz) * ambient;
    float3 totalLightColor = globalLight * _GlobalLightColor;

    // Calculate lighting from all other lights and sum it up
    float lighting = GetLightingAmount(worldPos, worldNormal, totalLightColor);
    float totalLight = lighting + globalLight;
    float nlRamp = tex2D(_LightRamp, float2(0.5, totalLight)).r;
    totalLight = lerp(totalLight, nlRamp, 0.75) + ambient;

    // Get camera distance for fog calculations
    float3 camToWorldVec = (worldPos - _WorldSpaceCameraPos.xyz);
    float3 camToWorldDir = normalize(camToWorldVec);
    float totalDistance = length(camToWorldVec);
    float distanceT = saturate((totalDistance - _GlobalFogNearScale) / (_GlobalFogFarScale - _GlobalFogNearScale));
    distanceT = saturate(distanceT + extraFade);

    // Skybox noise 
    float2 skyUV1 = worldPos.xz * 0.0002 + _Time.x * 0.1;
    float2 skyUV2 = worldPos.xz * 0.0002 - _Time.x * 0.1;
    float3 noiseColor1 = tex2D(_GlobalSkyboxNoiseTex, skyUV1).rgb - 0.5;
    float3 noiseColor2 = tex2D(_GlobalSkyboxNoiseTex, skyUV2).rgb - 0.5;

    // Calculate skybox color from gradient
    float noiseOffset = (noiseColor1.r + noiseColor2.r);
    float skyboxPos = (camToWorldDir * 500).y + _WorldSpaceCameraPos.y + noiseOffset * 100;
    float gradientT1 = InverseLerp(-1000, 0, skyboxPos);
    float gradientT2 = InverseLerp(0, 1000, skyboxPos);
    float3 gradientColor1 = lerp(_GlobalGroundColor.rgb, _GlobalHorizonColor.rgb, gradientT1);
    float3 gradientColor2 = lerp(_GlobalHorizonColor.rgb, _GlobalSkyColor.rgb, gradientT2);
    float3 skyColor = lerp(gradientColor1, gradientColor2, ceil(saturate(skyboxPos)));
    skyColor += skyColor * noiseColor1 * _GlobalSkyboxNoiseScale;
    skyColor += skyColor * noiseColor2 * _GlobalSkyboxNoiseScale;

    // Caustics
    float2 offset = float2(sin(worldPos.x * 0.2 + _Time.x * 30) * 0.2, sin(worldPos.z * 0.3 + _Time.x * 22) * 0.1);
    float2 causticsUV = worldPos.xz * 0.05;
    causticsUV.xy += _Time.xx * 1.6;
    causticsUV.xy += offset * 0.3;

    float2 causticsUV2 = worldPos.xz * 0.04;
    causticsUV2.xy += _Time.xx * -1.5;
    causticsUV2.xy += offset * 0.3;

    float3 causticsColor1 = tex2D(_GlobalCausticsNoiseTex, causticsUV).rgb;
    float3 causticsColor2 = tex2D(_GlobalCausticsNoiseTex, causticsUV2).rgb;
    float3 causticsColor = causticsColor1 * causticsColor2;
    float causticsAmount = saturate((worldPos.y + 100) / 300) * saturate(worldNormal.y);
    float3 caustics = lerp(skyColor, float3(1, 1, 1), 0.1) * causticsColor * causticsAmount * 5;
    caustics += ddx(caustics) * float3(1, 0, -1) * 10;

    float depth = max(0, abs(worldPos.y - 1000));
    float3 transmission = exp(-(totalDistance * (1 - _GlobalHorizonColor)) / _GlobalFogFarScale);
    float3 transmissionSurface = exp(-(depth * (1 - _GlobalHorizonColor)) / 1000);

    diffuse += caustics * _CausticsAlpha;
    diffuse *= transmission * transmissionSurface;
    diffuse += totalLightColor;

    diffuse = lerp(_GlobalGroundColor, diffuse, totalLight);
    diffuse = lerp(diffuse, skyColor, distanceT - lighting);

    return diffuse;
}

float3 FadeToHorizon(float3 color, float3 worldPos, float extraFade = 0)
{
    float3 camToWorldVec = (worldPos - _WorldSpaceCameraPos.xyz);
    float3 camToWorldDir = normalize(camToWorldVec);
    float totalDistance = length(camToWorldVec);
    float distanceT = saturate((totalDistance - _GlobalFogNearScale) / (_GlobalFogFarScale - _GlobalFogNearScale));
    distanceT = saturate(distanceT + extraFade);

    float2 skyUV1 = worldPos.xz * 0.0002 + _Time.x * 0.1;
    float2 skyUV2 = worldPos.xz * 0.0002 - _Time.x * 0.1;
    float3 noiseColor1 = tex2D(_GlobalSkyboxNoiseTex, skyUV1).rgb - 0.5;
    float3 noiseColor2 = tex2D(_GlobalSkyboxNoiseTex, skyUV2).rgb - 0.5;

    float noiseOffset = (noiseColor1.r + noiseColor2.r) * 100;
    float skyboxPos = (camToWorldDir * 500).y + _WorldSpaceCameraPos.y + noiseOffset;
    float gradientT1 = InverseLerp(-1000, 0, skyboxPos);
    float gradientT2 = InverseLerp(0, 1000, skyboxPos);
    float3 gradientColor1 = lerp(_GlobalGroundColor.rgb, _GlobalHorizonColor.rgb, gradientT1);
    float3 gradientColor2 = lerp(_GlobalHorizonColor.rgb, _GlobalSkyColor.rgb, gradientT2);
    float3 skyColor = lerp(gradientColor1, gradientColor2, ceil(saturate(skyboxPos)));
    skyColor += skyColor * noiseColor1 * _GlobalSkyboxNoiseScale;
    skyColor += skyColor * noiseColor2 * _GlobalSkyboxNoiseScale;

    return lerp(color, skyColor, distanceT);
}
