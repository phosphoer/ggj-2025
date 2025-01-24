using UnityEngine;
using System.Collections.Generic;

public class SimplePostFX : MonoBehaviour
{
  public Shader Shader = null;
  public SimplePostFXDefinition InitialSettings = null;

  [Range(0, 8)] public int BloomIterations = 4;

  private List<PostFXLayer> _layers = new List<PostFXLayer>();
  private List<LayerFade> _fadeInLayers = new List<LayerFade>();
  private List<LayerFade> _fadeOutLayers = new List<LayerFade>();
  private List<RenderTexture> _blurStepsBloom = new List<RenderTexture>();
  private RenderTexture _blurTempTarget;
  private Material _postFxMaterial;
  private int _lastWidthBloom;
  private int _lastHeightBloom;
  private int _lastBloomIterationCount;
  private int _lastWidthBlur;
  private int _lastHeightBlur;

  [System.Serializable]
  public class PostFXLayer
  {
    public PostFXLayer(SimplePostFXDefinition settings, float weight)
    {
      Settings = settings;
      Weight = weight;
    }

    public SimplePostFXDefinition Settings = null;
    public float Weight = 1;
  }

  [System.Serializable]
  public struct LayerFade
  {
    public PostFXLayer Layer;
    public float Duration;
    public float Timer;
    public float StartWeight;
    public float EndWeight;
  }

  private static readonly int kMatSourceTex = Shader.PropertyToID("_SourceTex");
  private static readonly int kMatBloomParams = Shader.PropertyToID("_BloomParams");
  private static readonly int kMatBlurParams = Shader.PropertyToID("_BlurParams");
  private static readonly int kMatColorParams = Shader.PropertyToID("_ColorParams");
  private static readonly int kMatColorBrightness = Shader.PropertyToID("_ColorBrightness");
  private static readonly int kMatChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
  private static readonly int kMatChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
  private static readonly int kMatChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");

  private const int kPassBloomPrefilter = 0;
  private const int kPassBloomDown = 1;
  private const int kPassBloomUp = 2;
  private const int kPassBlurDown = 3;
  private const int kPassBlurUp = 4;
  private const int kPassFinalBlur = 5;
  private const int kPassFinal = 6;

  public void AddLayer(PostFXLayer layer)
  {
    _layers.Add(layer);
    _layers.Sort((a, b) => { return a.Settings.Priority - b.Settings.Priority; });
  }

  public void RemoveLayer(PostFXLayer layer)
  {
    _layers.Remove(layer);
  }

  public void FadeInLayer(PostFXLayer layer, float duration, float targetWeight = 1)
  {
    LayerFade layerFade = new()
    {
      Layer = layer,
      Duration = duration,
      Timer = 0,
      StartWeight = 0,
      EndWeight = targetWeight,
    };

    layer.Weight = 0;
    _fadeInLayers.Add(layerFade);
    AddLayer(layer);
  }

  public void FadeOutLayer(PostFXLayer layer, float duration)
  {
    LayerFade layerFade = new()
    {
      Layer = layer,
      Duration = duration,
      Timer = 0,
      StartWeight = layer.Weight,
      EndWeight = 0,
    };

    _fadeOutLayers.Add(layerFade);
  }

  private void Start()
  {
    if (InitialSettings != null)
    {
      AddInitialLayer();
    }
    else
    {
      enabled = false;
    }
  }

  private void OnDestroy()
  {
    FreeRenderTextures();
    Destroy(_postFxMaterial);
  }

  private void Update()
  {
    for (int i = 0; i < _fadeInLayers.Count; ++i)
    {
      LayerFade fadeInfo = _fadeInLayers[i];
      fadeInfo.Timer += Time.unscaledDeltaTime;
      fadeInfo.Layer.Weight = Mathf.SmoothStep(fadeInfo.StartWeight, fadeInfo.EndWeight, fadeInfo.Timer / fadeInfo.Duration);
      _fadeInLayers[i] = fadeInfo;

      if (fadeInfo.Timer >= fadeInfo.Duration)
      {
        _fadeInLayers.RemoveAt(i);
        --i;
      }
    }

    for (int i = 0; i < _fadeOutLayers.Count; ++i)
    {
      LayerFade fadeInfo = _fadeOutLayers[i];
      fadeInfo.Timer += Time.unscaledDeltaTime;
      fadeInfo.Layer.Weight = Mathf.SmoothStep(fadeInfo.StartWeight, fadeInfo.EndWeight, fadeInfo.Timer / fadeInfo.Duration);
      _fadeOutLayers[i] = fadeInfo;

      if (fadeInfo.Timer >= fadeInfo.Duration)
      {
        RemoveLayer(fadeInfo.Layer);
        _fadeOutLayers.RemoveAt(i);
        --i;
      }
    }
  }

  [ContextMenu("Add Initial Layer")]
  private void AddInitialLayer()
  {
    AddLayer(new PostFXLayer(InitialSettings, 1));
  }

  private void FreeRenderTextures()
  {
    for (int i = 0; i < _blurStepsBloom.Count; ++i)
    {
      if (_blurStepsBloom[i] != null)
      {
        _blurStepsBloom[i].Release();
        Destroy(_blurStepsBloom[i]);
      }

      _blurStepsBloom.Clear();
    }

    if (_blurTempTarget != null)
    {
      _blurTempTarget.Release();
      Destroy(_blurTempTarget);
    }
  }

  private void UpdateRenderTextures(RenderTexture source)
  {
    if (_lastWidthBloom != source.width || _lastHeightBloom != source.height || _lastBloomIterationCount != BloomIterations)
    {
      FreeRenderTextures();

      RenderTexture originalRT = RenderTexture.active;

      _lastWidthBloom = source.width;
      _lastHeightBloom = source.height;
      _lastBloomIterationCount = BloomIterations;
      int blurWidth = _lastWidthBloom;
      int blurHeight = _lastHeightBloom;
      const int divisor = 2;
      for (int i = 0; i < BloomIterations && blurWidth >= divisor && blurHeight >= divisor; ++i)
      {
        blurWidth /= divisor;
        blurHeight /= divisor;
        RenderTexture blurStep = new RenderTexture(blurWidth, blurHeight, 0, source.format);
        blurStep.Create();
        _blurStepsBloom.Add(blurStep);

        RenderTexture.active = blurStep;
        GL.Clear(true, true, Color.white);
      }

      RenderTexture.active = originalRT;
    }
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination)
  {
    // Create material
    if (_postFxMaterial == null && Shader != null)
    {
      _postFxMaterial = new Material(Shader);
      _postFxMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    if (_postFxMaterial == null || _layers.Count == 0)
    {
      Graphics.Blit(source, destination);
      return;
    }

    SimplePostFXDefinition.BloomSettings bloom = SimplePostFXDefinition.BloomDefault;
    SimplePostFXDefinition.SaturationContrastSettings saturationContrast = SimplePostFXDefinition.SaturationContrastDefault;
    SimplePostFXDefinition.WhiteBalanceSettings whiteBalance = SimplePostFXDefinition.WhiteBalanceDefault;
    SimplePostFXDefinition.ChannelMixerSettings channelMixers = SimplePostFXDefinition.ChannelMixerDefault;
    SimplePostFXDefinition.BlurSettings blur = SimplePostFXDefinition.BlurDefault;
    for (int i = 0; i < _layers.Count; ++i)
    {
      PostFXLayer layer = _layers[i];
      if (layer.Settings.Bloom.Enabled)
      {
        bloom = SimplePostFXDefinition.Lerp(bloom, layer.Settings.Bloom, layer.Weight);
      }

      if (layer.Settings.SaturationContrast.Enabled)
      {
        saturationContrast = SimplePostFXDefinition.Lerp(saturationContrast, layer.Settings.SaturationContrast, layer.Weight);
      }

      if (layer.Settings.WhiteBalance.Enabled)
      {
        whiteBalance = SimplePostFXDefinition.Lerp(whiteBalance, layer.Settings.WhiteBalance, layer.Weight);
      }

      if (layer.Settings.ChannelMixers.Enabled)
      {
        channelMixers = SimplePostFXDefinition.Lerp(channelMixers, layer.Settings.ChannelMixers, layer.Weight);
      }

      if (layer.Settings.Blur.Enabled)
      {
        blur = SimplePostFXDefinition.Lerp(blur, layer.Settings.Blur, layer.Weight);
      }
    }

    // Update material params
    Vector4 bloomParams = new Vector4(bloom.BloomFilterSize, bloom.BloomThreshold, bloom.BloomIntensity, bloom.BloomThresholdSoft);
    Vector4 blurParams = new Vector4(blur.FilterSize, 0, 0, blur.Opacity);
    Vector4 colorParams = new Vector4(saturationContrast.ColorSaturation, saturationContrast.ColorContrast, whiteBalance.ColorTemperature, whiteBalance.ColorTint);
    _postFxMaterial.SetVector(kMatBloomParams, bloomParams);
    _postFxMaterial.SetVector(kMatBlurParams, blurParams);
    _postFxMaterial.SetVector(kMatColorParams, colorParams);
    _postFxMaterial.SetFloat(kMatColorBrightness, saturationContrast.ColorBrightness);
    _postFxMaterial.SetVector(kMatChannelMixerRed, channelMixers.ChannelMixerRed);
    _postFxMaterial.SetVector(kMatChannelMixerGreen, channelMixers.ChannelMixerGreen);
    _postFxMaterial.SetVector(kMatChannelMixerBlue, channelMixers.ChannelMixerBlue);

    RenderTexture currentSource = source;
    RenderTexture currentDestination;

    // Do bloom
    if (bloom.Enabled)
    {
      // Initialize render textures if necessary
      UpdateRenderTextures(source);

      // Downsample
      int iteration = 0;
      for (; iteration < _blurStepsBloom.Count && iteration < BloomIterations; ++iteration)
      {
        currentDestination = _blurStepsBloom[iteration];
        Graphics.Blit(currentSource, currentDestination, _postFxMaterial, iteration == 0 ? kPassBloomPrefilter : kPassBloomDown);
        currentSource = currentDestination;
      }

      // Upsample 
      for (iteration -= 2; iteration >= 0; iteration--)
      {
        currentDestination = _blurStepsBloom[iteration];
        Graphics.Blit(currentSource, currentDestination, _postFxMaterial, kPassBloomUp);
        currentSource = currentDestination;
      }
    }

    // Do blur
    if (blur.Enabled)
    {
      // Initialize render textures if necessary
      UpdateRenderTextures(source);
      if (_lastWidthBlur != source.width || _lastHeightBlur != source.height)
      {
        if (_blurTempTarget != null)
        {
          _blurTempTarget.Release();
          Destroy(_blurTempTarget);
        }

        _blurTempTarget = new RenderTexture(source.width, source.height, 0, source.format);
        _blurTempTarget.Create();
        _lastWidthBlur = source.width;
        _lastHeightBlur = source.height;
      }

      // Blit 'final' image so far to blur temp target
      _postFxMaterial.SetTexture(kMatSourceTex, source);
      Graphics.Blit(currentSource, _blurTempTarget, _postFxMaterial, kPassFinal);

      // Downsample
      currentSource = _blurTempTarget;
      int iteration = 0;
      for (; iteration < _blurStepsBloom.Count && iteration < BloomIterations; ++iteration)
      {
        currentDestination = _blurStepsBloom[iteration];
        Graphics.Blit(currentSource, currentDestination, _postFxMaterial, kPassBlurDown);
        currentSource = currentDestination;
      }

      // Upsample 
      for (iteration -= 2; iteration >= 0; iteration--)
      {
        currentDestination = _blurStepsBloom[iteration];
        Graphics.Blit(currentSource, currentDestination, _postFxMaterial, kPassBlurUp);
        currentSource = currentDestination;
      }

      // Final blit
      _postFxMaterial.SetTexture(kMatSourceTex, _blurTempTarget);
      Graphics.Blit(currentSource, destination, _postFxMaterial, kPassFinalBlur);
    }
    else
    {
      // Final blit
      _postFxMaterial.SetTexture(kMatSourceTex, source);
      Graphics.Blit(currentSource, destination, _postFxMaterial, kPassFinal);
    }
  }
}