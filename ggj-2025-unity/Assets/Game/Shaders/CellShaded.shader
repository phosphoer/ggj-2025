Shader "Custom/CellShaded"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _MainTexAlpha ("Texture Alpha", float) = 1

    _VertexColorAlpha ("Vertex Color Alpha", float) = 1
    _VertexColorGlow ("Vertex Color Glow", float) = 0

    [NoScaleOffset]
    _LightRamp ("Light Ramp", 2D) = "white" {}
    _Glow ("Glow", float) = 0

    _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
    _FresnelPower ("Fresnel Power", float) = 1
    _FresnelOpacity ("Fresnel Opacity", float) = 0

    _CausticsAlpha ("Caustics Alpha", float) = 1

    _GlobalDitherTex("Dither Texture", 2D) = "white" {}
    _DitherClipDistance ("Dither Clip Distance", float) = 1

    _ClipRect ("Clip Rect", Vector) = (0,0,0,0)
    _UIMaskSoftness ("UI Mask Softness", Vector) = (0,0,0,0)
  }
  SubShader
  {
    CGINCLUDE
    #include "UnityCG.cginc"
    #include "CellShading.cginc"
    #include "Dithering.cginc"

    #ifdef UNITY_UI_CLIP_RECT
    #include "UnityUI.cginc"
    #endif

    #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

    struct appdata
    {
      float4 vertex : POSITION;
      float4 normal : NORMAL;
      float4 color : COLOR;
      float2 uv : TEXCOORD0;
    };

    struct v2f
    {
      float4 pos : SV_POSITION;
      float4 color : COLOR;
      float2 uv : TEXCOORD0;
      SHADOW_COORDS(1)
      float3 worldNormal : TEXCOORD2;
      float3 worldPos : TEXCOORD3;
      float4 screenPos : TEXCOORD4;

      #ifdef UNITY_UI_CLIP_RECT
      float4 mask : TEXCOORD5;
      #endif
    };

    sampler2D _MainTex;
    float _MainTexAlpha;
    float4 _MainTex_ST;
    float4 _Color;
    float _VertexColorAlpha;
    float _VertexColorGlow;
    float4 _FresnelColor;
    float _FresnelPower;
    float _FresnelOpacity;
    float _Glow;
    float _DitherClipDistance;

    float4 _ClipRect;
    float4 _UIMaskSoftness;

    v2f vert(appdata v)
    {
      v2f o;
      o.pos = UnityObjectToClipPos(v.vertex);
      o.worldPos = mul(unity_ObjectToWorld, v.vertex);
      o.uv = TRANSFORM_TEX(v.uv, _MainTex);
      o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
      o.color = v.color;
      o.screenPos = ComputeScreenPos(o.pos);

      // Calculate mask value used for rectmask softness in UnityUI
      #ifdef UNITY_UI_CLIP_RECT
      float2 pixelSize = o.pos.w;
      pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

      float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
      o.mask = float4(o.worldPos.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftness.x, _UIMaskSoftness.y) + abs(pixelSize.xy)));
      #endif

      TRANSFER_SHADOW(o)

      return o;
    }

    float4 frag(v2f i) : SV_Target
    {
      // Clip to a RectMask, if one is defined
      #ifdef UNITY_UI_CLIP_RECT
      float clip2d = UnityGet2DClipping(i.worldPos.xy, _ClipRect);
      clip(clip2d - 0.01);
      #endif

      // Get base diffuse color
      float3 texColor = tex2D(_MainTex, i.uv).rgb;
      float3 vColor = lerp(float3(1, 1, 1), i.color, _VertexColorAlpha);
      float3 diffuse = _Color.rgb * vColor * lerp(float3(1, 1, 1), texColor, _MainTexAlpha);

      float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
      float viewDot = dot(viewDir, normalize(i.worldNormal));
      float fresnel = saturate(pow(1 - abs(viewDot), _FresnelPower));
      diffuse.rgb += _FresnelColor * fresnel * _FresnelOpacity;

      float3 diffuseLit = LightingAndAmbience(diffuse, i.worldPos, i.worldNormal);
      diffuseLit.rgb += diffuse.rgb * _Glow * lerp(1, i.color.r, _VertexColorGlow);

      // Handle softness parameter from UnityUI RectMask
      float alpha = 1;
      #ifdef UNITY_UI_CLIP_RECT
      float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
      alpha *= m.x * m.y;
      #endif

      float clipDepth = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
      float ditheredAlpha = Dither(saturate((clipDepth - _DitherClipDistance) / _DitherClipDistance), i.screenPos.xy / i.screenPos.w);
      clip(ditheredAlpha - 0.01);

      return float4(diffuseLit, alpha * _Color.a);
    }
    ENDCG

    Pass
    {
      Tags
      {
        "RenderType"="Opaque"
      }

      ZWrite On
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
  }

  Fallback "Diffuse"
}