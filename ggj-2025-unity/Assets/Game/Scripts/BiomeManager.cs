using UnityEngine;
using System.Collections.Generic;

public class BiomeManager : Singleton<BiomeManager>
{
  public BiomeDefinition CurrentBiome => _currentBiome;

  private static List<BiomeVolume> _volumeStack = new();

  private BiomeDefinition _currentBiome;

  private AudioManager.AudioInstance _audioAmbient;

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    float dt = Time.deltaTime;
    Vector3 cameraPos = MainCamera.Instance.CachedTransform.position;

    for (int i = 0; i < BiomeVolume.Instances.Count; ++i)
    {
      BiomeVolume volume = BiomeVolume.Instances[i];
      if (volume.ContainsPoint(cameraPos) && !_volumeStack.Contains(volume))
        _volumeStack.Add(volume);
    }

    for (int i = 0; i < _volumeStack.Count; ++i)
    {
      if (!_volumeStack[i].ContainsPoint(cameraPos))
      {
        _volumeStack.RemoveAt(i);
        --i;
      }
    }

    _volumeStack.Sort((a, b) => b.Priority - a.Priority);

    if (_volumeStack.Count > 0)
    {
      BiomeVolume currentVolume = _volumeStack[0];
      if (_currentBiome != currentVolume.Biome)
      {
        if (_currentBiome == null || _currentBiome.SfxAmbient != currentVolume.Biome.SfxAmbient)
        {
          if (_currentBiome != null)
            AudioManager.Instance.FadeOutSound(gameObject, _currentBiome.SfxAmbient, 5);

          AudioManager.Instance.FadeInSound(gameObject, currentVolume.Biome.SfxAmbient, 5);
        }

        _currentBiome = currentVolume.Biome;
      }
    }

    if (_currentBiome != null)
    {
      float dampDt = 1 - Mathf.Pow(0.5f, dt * 0.3f);
      SkyboxManager.Instance.Colors = SkyboxColors.Lerp(SkyboxManager.Instance.Colors, _currentBiome.SkyboxColors, dampDt);
    }
  }
}