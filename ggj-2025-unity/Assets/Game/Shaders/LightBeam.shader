Shader "Custom/Light Beam"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _GlowRadius ("Glow Radius", float) = 5.0
    _BeamAngle ("Beam Angle", float) = 30
    _BeamLength ("Beam Length", float) = 10
    _BeamInnerRadius ("Beam Inner Radius", float) = 1

    [Enum(Off,0,On,1)] 
    _ZWrite ("ZWrite", Float) = 1
    
    [Enum(Always, 0, Less, 2, Equal, 3, LEqual, 4, GEqual, 5)] 
    _ZTest ("ZTest", Float) = 4
  }
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "CellShading.cginc"

    struct appdata
    {
      float4 vertex : POSITION;
      fixed4 color : COLOR;
      float2 uv : TEXCOORD0;
    };

    struct v2f
    {
      float4 vertex : SV_POSITION;
      fixed4 color : COLOR;
      float2 uv : TEXCOORD0;
      float3 worldPos : TEXCOORD1;
    };

    sampler2D _MainTex;
    float4 _Color;
    float _GlowRadius;
    float _BeamAngle;
    float _BeamLength;
    float _BeamInnerRadius;

    float3 NearestPointLine(float3 lineStart, float3 lineEnd, float3 pointPos)
    {
      float3 lineDirection = normalize(lineEnd - lineStart);
      float closestPoint = dot((pointPos - lineStart), lineDirection) / dot(lineDirection, lineDirection);
      return lineStart + (closestPoint * lineDirection);
    }

    float2 NearestPointLine(float2 lineStart, float2 lineEnd, float2 pointPos)
    {
      float2 lineDirection = normalize(lineEnd - lineStart);
      float closestPoint = dot((pointPos - lineStart), lineDirection) / dot(lineDirection, lineDirection);
      return lineStart + (closestPoint * lineDirection);
    }

    v2f vert (appdata v)
    {
      v2f o;
      
      float coneEndRadius = tan(radians(_BeamAngle)) * _BeamLength;
      v.vertex.xy *= lerp(_BeamInnerRadius, coneEndRadius * 2, v.vertex.z);
      v.vertex.z *= lerp(1, _BeamLength, v.vertex.z);

      o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
      o.vertex = UnityObjectToClipPos(v.vertex);
      o.uv = v.uv;
      o.color = v.color;
      o.color.a *= saturate(1 - v.uv.y);
      return o;
    }

    fixed4 frag (v2f i) : SV_Target
    {
      fixed4 color = _Color * tex2D(_MainTex, i.uv) * i.color;
      fixed4 finalColor = color * color.a * i.color.a + _Color * _GlowRadius;
      finalColor.rgb = FadeToHorizon(color.rgb, i.worldPos, -0.25) * color.a * i.color.a;
      return finalColor;
    }
    ENDCG

    Pass
    {
      Tags { "RenderType"="Transparent" "Queue"="Transparent" }
      BlendOp Add
      Blend One One
      ZWrite [_ZWrite]
      ZTest [_ZTest]

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
  }
}
