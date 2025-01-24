Shader "Custom/Skybox"
{
  Properties
  {
    [Enum(Off,0,On,1)] 
    _ZWrite ("ZWrite", Float) = 1
    
    [Enum(Always, 0, Less, 2, Equal, 3, LEqual, 4, GEqual, 5)] 
    _ZTest ("ZTest", Float) = 4
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    ZWrite [_ZWrite]
    ZTest [_ZTest]
    
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

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
        float3 vertLocal : TEXCOORD1;
        float3 worldPos : TEXCOORD2;
      };

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.uv = v.uv;
        o.color = v.color;
        o.vertLocal = v.vertex;
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        return fixed4(FadeToHorizon(fixed3(0, 0, 0), i.worldPos, 1), 1);
      }
      ENDCG
    }
  }
}
