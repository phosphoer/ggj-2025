using UnityEngine;

[System.Serializable]
public struct SkyboxColors
{
  public Color SkyColor;
  public Color HorizonColor;
  public Color GroundColor;

  public static SkyboxColors Lerp(SkyboxColors a, SkyboxColors b, float t)
  {
    SkyboxColors v = a;
    v.SkyColor = Color.Lerp(a.SkyColor, b.SkyColor, t);
    v.HorizonColor = Color.Lerp(a.HorizonColor, b.HorizonColor, t);
    v.GroundColor = Color.Lerp(a.GroundColor, b.GroundColor, t);
    return v;
  }
}

[ExecuteAlways]
public class SkyboxManager : Singleton<SkyboxManager>
{
  public SkyboxColors Colors;
  public Texture SkyboxNoise = null;
  public Texture CausticsNoise = null;

  [Range(0, 1000)] public float FogNearScale = 100;
  [Range(0, 1000)] public float FogFarScale = 500;
  [Range(0, 1)] public float SkyboxNoiseScale = 0.25f;

  [SerializeField] private Transform _skyboxRoot = null;

  private static readonly int kShaderSkyColor = Shader.PropertyToID("_GlobalSkyColor");
  private static readonly int kShaderHorizonColor = Shader.PropertyToID("_GlobalHorizonColor");
  private static readonly int kShaderGroundColor = Shader.PropertyToID("_GlobalGroundColor");
  private static readonly int kShaderFogNearScale = Shader.PropertyToID("_GlobalFogNearScale");
  private static readonly int kShaderFogFarScale = Shader.PropertyToID("_GlobalFogFarScale");
  private static readonly int kShaderSkyboxNoise = Shader.PropertyToID("_GlobalSkyboxNoiseTex");
  private static readonly int kShaderCausticsNoise = Shader.PropertyToID("_GlobalCausticsNoiseTex");
  private static readonly int kShaderSkyboxNoiseScale = Shader.PropertyToID("_GlobalSkyboxNoiseScale");

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    Shader.SetGlobalColor(kShaderSkyColor, Colors.SkyColor.linear);
    Shader.SetGlobalColor(kShaderHorizonColor, Colors.HorizonColor.linear);
    Shader.SetGlobalColor(kShaderGroundColor, Colors.GroundColor.linear);

    Shader.SetGlobalFloat(kShaderFogNearScale, FogNearScale);
    Shader.SetGlobalFloat(kShaderFogFarScale, FogFarScale);
    Shader.SetGlobalFloat(kShaderSkyboxNoiseScale, SkyboxNoiseScale);

    Shader.SetGlobalTexture(kShaderSkyboxNoise, SkyboxNoise);
    Shader.SetGlobalTexture(kShaderCausticsNoise, CausticsNoise);

    if (Application.isPlaying)
    {
      Vector3 cameraPos = MainCamera.Instance.CachedTransform.position;
      _skyboxRoot.position = cameraPos;
      _skyboxRoot.localScale = Vector3.one * (MainCamera.Instance.Camera.farClipPlane * 2 - 100);

      BiomeDefinition currentBiome = BiomeManager.Instance.CurrentBiome;
      if (currentBiome != null)
      {
        FogFarScale = Mathfx.Damp(FogFarScale, currentBiome.FogFar, 0.25f, Time.deltaTime * 0.1f);
        FogNearScale = Mathfx.Damp(FogNearScale, currentBiome.FogNear, 0.25f, Time.deltaTime * 0.1f);
      }
    }
  }
}