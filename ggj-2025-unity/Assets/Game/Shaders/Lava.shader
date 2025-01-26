Shader "Custom/Lava"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _MainTexAlpha ("Texture Alpha", float) = 1
    [NoScaleOffset]
    _LightRamp ("Light Ramp", 2D) = "white" {}
    _Glow ("Glow", float) = 0

    _NoiseFrequency ("Noise Frequency", float) = 2
    _NoiseScale ("Noise Scale", float) = 0.05
    _NoiseSpeed ("Noise Speed", float) = 0.2

    _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
    _FresnelPower ("Fresnel Power", float) = 1
    _FresnelOpacity ("Fresnel Opacity", float) = 0
  }
  SubShader
  {
    CGINCLUDE
    #include "UnityCG.cginc"
    #include "CellShading.cginc"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

    struct appdata
    {
      float4 vertex : POSITION;
      fixed4 normal : NORMAL;
      fixed4 color : COLOR;
      float2 uv : TEXCOORD0;
    };

    struct v2f
    {
      float4 pos : SV_POSITION;
      fixed4 color : COLOR;
      float2 uv : TEXCOORD0;
      SHADOW_COORDS(1)
      float3 worldNormal : TEXCOORD2;
      float3 worldPos : TEXCOORD3;
      UNITY_FOG_COORDS(4)
      float3 heights : TEXCOORD5;
    };

    sampler2D _MainTex;
    float _MainTexAlpha;
    float4 _MainTex_ST;
    float4 _Color;
    float _Glow;
    float4 _FresnelColor;
    float _FresnelPower;
    float _FresnelOpacity;
    float _NoiseFrequency;
    float _NoiseScale;
    float _NoiseSpeed;

    float3 GetNoiseVertex(float3 inVert, float3 normal)
    {
      float3 noisePos = inVert * _NoiseFrequency + _Time.y * _NoiseSpeed;
      inVert += normal * SimplexNoise(noisePos) * _NoiseScale;
      return inVert;
    }

    v2f vert(appdata v)
    {
      v2f o;

      float3 v0 = GetNoiseVertex(v.vertex.xyz, v.normal);
      float3 v1 = GetNoiseVertex(v.vertex.xyz + float3(0.1, 0, 0), v.normal);
      float3 v2 = GetNoiseVertex(v.vertex.xyz + float3(0, 0, 0.1), v.normal);

      v.vertex.xyz = v0;
      v.normal.xyz = normalize(cross(v1 - v0, v2 - v0));

      o.pos = UnityObjectToClipPos(v.vertex);
      o.worldPos = mul(unity_ObjectToWorld, v.vertex);
      o.uv = TRANSFORM_TEX(v.uv, _MainTex);
      o.worldNormal = mul(unity_ObjectToWorld, fixed4(v.normal.xyz, 0));
      o.color = v.color;
      o.heights = float3(v0.y, v1.y, v2.y);

      TRANSFER_SHADOW(o)
      UNITY_TRANSFER_FOG(o, o.pos);

      return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
      // Get base diffuse color
      fixed3 texColor = tex2D(_MainTex, i.uv).rgb;
      fixed3 diffuse = _Color.rgb * i.color * lerp(fixed3(1, 1, 1), texColor, _MainTexAlpha);

      fixed lightAtten = 1;
      #ifdef POINT
        unityShadowCoord3 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(i.worldPos, 1)).xyz;
        fixed shadow = UNITY_SHADOW_ATTENUATION(i, i.worldPos);
        lightAtten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).r * shadow;
      #endif


      float height = i.heights.x;
      float height1 = i.heights.y;
      float height2 = i.heights.z;

      float3 tangent = float3(2, height1 - height, 0);
      float3 bitangent = float3(0, height2 - height, 2);
      float3 worldNormal = normalize(cross(bitangent, tangent));

      float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
      float viewDot = dot(viewDir, normalize(worldNormal));
      float fresnel = saturate(pow(1 - abs(viewDot), _FresnelPower));
      diffuse.rgb += _FresnelColor * fresnel * _FresnelOpacity;

      diffuse *= CalculateLighting(normalize(worldNormal), lightAtten, SHADOW_ATTENUATION(i)).rgb;
      diffuse.rgb += diffuse.rgb * _Glow;

      UNITY_APPLY_FOG(i.fogCoord, diffuse);

      return fixed4(diffuse, 1);
    }
    ENDCG

    Pass
    {
      Tags
      {
        "RenderType"="Opaque" "LightMode" = "ForwardBase"
      }

      ZWrite On

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_fog
      #pragma multi_compile_fwdbase
      ENDCG
    }

    Pass
    {
      Tags
      {
        "RenderType"="Opaque" "LightMode" = "ForwardAdd"
      }
      Blend One One
      ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_fog
      #pragma multi_compile_fwdadd
      ENDCG
    }
  }

  Fallback "Diffuse"
}