Shader "Custom/CellShaded"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _MainTexAlpha ("Texture Alpha", float) = 1
    [NoScaleOffset]
    _LightRamp ("Light Ramp", 2D) = "white" {}
    _Glow ("Glow", float) = 0

    _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
    _FresnelPower ("Fresnel Power", float) = 1
    _FresnelOpacity ("Fresnel Opacity", float) = 0
    _FresnelBlend ("Fresnel Blend", float) = 0
  }
  SubShader
  {
    CGINCLUDE
    #include "UnityCG.cginc"
    #include "CellShading.cginc"

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
    };

    sampler2D _MainTex;
    float _MainTexAlpha;
    float4 _MainTex_ST;
    float4 _Color;
    float _Glow;
    float4 _FresnelColor;
    float _FresnelPower;
    float _FresnelOpacity;
    float _FresnelBlend;

    v2f vert(appdata v)
    {
      v2f o;
      o.pos = UnityObjectToClipPos(v.vertex);
      o.worldPos = mul(unity_ObjectToWorld, v.vertex);
      o.uv = TRANSFORM_TEX(v.uv, _MainTex);
      o.worldNormal = mul(unity_ObjectToWorld, fixed4(v.normal.xyz, 0));
      o.color = v.color;

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


      float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
      float viewDot = dot(viewDir, normalize(i.worldNormal));
      float fresnel = saturate(pow(1 - abs(viewDot), _FresnelPower));

      float3 fresnelAdditive = diffuse.rgb + _FresnelColor * fresnel * _FresnelOpacity;
      float3 fresnelMult = lerp(diffuse.rgb, _FresnelColor, _FresnelOpacity * fresnel);
      diffuse.rgb = lerp(fresnelAdditive, fresnelMult, _FresnelBlend);

      diffuse *= CalculateLighting(normalize(i.worldNormal), lightAtten, SHADOW_ATTENUATION(i)).rgb;
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