using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class LightingManager : MonoBehaviour
{
  [SerializeField] private Transform _mainLightTransform = null;

  [SerializeField] private Color _mainLightColor = Color.white;

  private Vector4[] _lightPositionBuffer = new Vector4[kMaxLights];
  private Vector4[] _lightDirectionBuffer = new Vector4[kMaxLights];
  private Vector4[] _lightColorBuffer = new Vector4[kMaxLights];
  private Vector4[] _lightParamsBuffer = new Vector4[kMaxLights];

  private List<LightController> _sortedLights = new();

  private const int kMaxLights = 8;

  private static readonly int kShaderGlobalLightDirection = Shader.PropertyToID("_GlobalLightDirection");
  private static readonly int kShaderGlobalLightColor = Shader.PropertyToID("_GlobalLightColor");

  private static readonly int kShaderLightPositions = Shader.PropertyToID("_GlobalLightPositions");
  private static readonly int kShaderLightDirections = Shader.PropertyToID("_GlobalLightDirections");
  private static readonly int kShaderLightColors = Shader.PropertyToID("_GlobalLightColors");
  private static readonly int kShaderLightParams = Shader.PropertyToID("_GlobalLightParams");

  private void LateUpdate()
  {
    Shader.SetGlobalColor(kShaderGlobalLightColor, _mainLightColor);
    Shader.SetGlobalVector(kShaderGlobalLightDirection, -_mainLightTransform.forward);

    Transform cameraTransform = MainCamera.Instance != null ? MainCamera.Instance.CachedTransform : null;
#if UNITY_EDITOR
    if (!UnityEditor.EditorApplication.isPlaying)
      cameraTransform = UnityEditor.SceneView.GetAllSceneCameras()[0].transform;
#endif

    _sortedLights.Clear();
    _sortedLights.AddRange(LightController.Instances);
    _sortedLights.Sort((a, b) =>
    {
      float distA = Vector3.SqrMagnitude(a.transform.position - cameraTransform.position);
      float distB = Vector3.SqrMagnitude(b.transform.position - cameraTransform.position);
      return Mathf.RoundToInt(distA - distB);
    });

    for (int i = 0; i < kMaxLights; ++i)
    {
      if (i < _sortedLights.Count)
      {
        LightController light = _sortedLights[i];

        int lightType = (int)light.LightType;
        _lightPositionBuffer[i] = light.transform.position;
        _lightDirectionBuffer[i] = light.transform.forward.WithW(lightType);
        _lightColorBuffer[i] = light.Color;
        _lightParamsBuffer[i] = new Vector4(light.Strength, light.Falloff, light.SpotlightAngle, 0);
      }
      else
      {
        _lightPositionBuffer[i] = Vector4.zero;
        _lightDirectionBuffer[i] = Vector4.zero;
        _lightColorBuffer[i] = Vector4.zero;
        _lightParamsBuffer[i] = Vector4.zero;
      }
    }

    Shader.SetGlobalVectorArray(kShaderLightPositions, _lightPositionBuffer);
    Shader.SetGlobalVectorArray(kShaderLightDirections, _lightDirectionBuffer);
    Shader.SetGlobalVectorArray(kShaderLightColors, _lightColorBuffer);
    Shader.SetGlobalVectorArray(kShaderLightParams, _lightParamsBuffer);
  }
}