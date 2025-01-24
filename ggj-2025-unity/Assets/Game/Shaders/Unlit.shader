Shader "Custom/Unlit"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _GlowRadius ("Glow Radius", float) = 0
    _GlowTexFactor ("Glow Texture Factor", float) = 0
    _VertexColorAlpha ("Vertex Color Alpha", float) = 1
    _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
    _FresnelPower ("Fresnel Power", float) = 0
    _FresnelOpacity ("Fresnel Opacity", float) = 0

    _GlobalDitherTex("Dither Texture", 2D) = "white" {}
    _DitherClipDistance ("Dither Clip Distance", float) = 1

    [Enum(Off,0,On,1)]
    _ZWrite ("ZWrite", Float) = 1

    [Enum(Always, 0, Less, 2, Equal, 3, LEqual, 4, GEqual, 5)]
    _ZTest ("ZTest", Float) = 4

    [Enum(UnityEngine.Rendering.BlendOp)]
    _BlendOperation ("Blend Op", Float) = 0

    [Enum(UnityEngine.Rendering.BlendMode)]
    _BlendSource ("Blend Src", Float) = 5

    [Enum(UnityEngine.Rendering.BlendMode)]
    _BlendDest ("Blend Dest", Float) = 10

    [Enum(UnityEngine.Rendering.CullMode)]
    _CullMode ("Cull Mode", Float) = 2
  }
  SubShader
  {
    Blend [_BlendSource] [_BlendDest]
    BlendOp [_BlendOperation]
    ZWrite [_ZWrite]
    ZTest [_ZTest]
    Cull [_CullMode]

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile _ _ENABLE_SOFT_PARTICLE

      #include "UnityCG.cginc"
      #include "Dithering.cginc"

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
        float3 worldNormal : TEXCOORD1;
        float3 worldPos : TEXCOORD2;
        float4 screenPos : TEXCOORD3;
        float4 clipPos : TEXCOORD4;
      };

      sampler2D _MainTex;
      sampler2D _CameraDepthTexture;
      float4 _Color;
      float _GlowRadius;
      float _GlowTexFactor;
      float _VertexColorAlpha;
      float4 _FresnelColor;
      float _FresnelPower;
      float _FresnelOpacity;
      float _DitherClipDistance;

      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.clipPos = o.pos;
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.worldNormal = mul(unity_ObjectToWorld, fixed4(v.normal.xyz, 0));
        o.uv = v.uv;
        o.color = v.color;
        o.screenPos = ComputeScreenPos(o.pos);

        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        float4 tex = tex2D(_MainTex, i.uv);
        float4 texGlow = lerp(float4(1, 1, 1, 1), tex, _GlowTexFactor);
        float4 vColor = lerp(float4(1, 1, 1, 1), i.color, _VertexColorAlpha);
        float4 diffuse = _Color * tex * vColor;
        float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
        float fresnel = saturate(pow(1 - abs(dot(viewDir, normalize(i.worldNormal))), _FresnelPower));

        diffuse.rgb += diffuse.rgb * _GlowRadius * texGlow.rgb;
        diffuse.rgb += _FresnelColor * fresnel * _FresnelOpacity;

        #if _ENABLE_SOFT_PARTICLE
        float2 screenUV = i.screenPos.xy / i.screenPos.w;
        float rawSceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
        float sceneDepth = rawSceneDepth * _ProjectionParams.z;
        float rawCurrentDepth = (i.clipPos.z / i.clipPos.w);
        float currentDepth = rawCurrentDepth * _ProjectionParams.z;
        diffuse.a *= saturate(abs(sceneDepth - currentDepth) * 1);
        #endif

        float clipDepth = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
        float ditheredAlpha = Dither(saturate((clipDepth - _DitherClipDistance) / _DitherClipDistance), i.screenPos.xy / i.screenPos.w);
        clip(ditheredAlpha - 0.01);

        return diffuse;
      }
      ENDCG
    }
  }

  Fallback "VertexLit"
}