using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class LightController : MonoBehaviour
{
  public static List<LightController> Instances => _instances;

  public enum LightTypeEnum
  {
    Point = 0,
    Spot,
    Directional,
  }

  public LightTypeEnum LightType;
  public Color Color = Color.white;
  public float Strength = 1;
  public float Falloff = 250;
  [Range(0, 180)] public float SpotlightAngle = 30;
  public float MaxBeamLength = 20;
  [Range(0, 1)] public float BeamOpacity = 0.5f;
  public bool EnableDistanceCast = false;
  public LayerMask BeamRaycastMask = default;

  [SerializeField] private Renderer _lightBeamRenderer = null;
  [SerializeField] private Renderer _lightBulbRenderer = null;
  [SerializeField] private ParticleSystem _lightParticles = null;

  private Material _lightMaterial;
  private Material _bulbMaterial;
  private float _currentBeamLength;
  private bool _isFading;
  private float _fadeTargetStrength;
  private float _fadeStartStrength;
  private float _fadeTimer;
  private float _fadeDuration;

  private static List<LightController> _instances = new();

  private static readonly int kShaderBeamAngle = Shader.PropertyToID("_BeamAngle");
  private static readonly int kShaderBeamLength = Shader.PropertyToID("_BeamLength");
  private static readonly int kShaderBeamColor = Shader.PropertyToID("_Color");
  private static readonly int kShaderGlow = Shader.PropertyToID("_Glow");

  public void StartFade(float targetStrength, float duration)
  {
    _isFading = true;
    _fadeTimer = 0;
    _fadeTargetStrength = targetStrength;
    _fadeStartStrength = Strength;
    _fadeDuration = duration;
  }

  private void Awake()
  {
    _currentBeamLength = MaxBeamLength;

#if UNITY_EDITOR
    if (!UnityEditor.EditorApplication.isPlaying)
      return;
#endif

    if (_lightBeamRenderer != null)
    {
      _lightMaterial = _lightBeamRenderer.material;
    }

    if (_lightBulbRenderer != null)
    {
      _bulbMaterial = _lightBulbRenderer.material;
    }

    if (_lightParticles != null)
    {
      var particleShape = _lightParticles.shape;
      if (LightType == LightTypeEnum.Point)
      {
        particleShape.shapeType = ParticleSystemShapeType.Sphere;
        particleShape.radius = Falloff / 2;
      }
      else if (LightType == LightTypeEnum.Spot)
      {
        particleShape.shapeType = ParticleSystemShapeType.ConeVolume;
        particleShape.radius = 1;
        particleShape.length = Falloff / 2;
      }
    }
  }

  private void OnDestroy()
  {
    if (_lightMaterial != null)
      Destroy(_lightMaterial);

    if (_bulbMaterial != null)
      Destroy(_bulbMaterial);
  }

  private void OnEnable()
  {
    _instances.Add(this);

#if UNITY_EDITOR
    if (!UnityEditor.EditorApplication.isPlaying)
      return;
#endif

    _currentBeamLength = 0;

    if (_bulbMaterial != null)
    {
      _bulbMaterial.SetFloat(kShaderGlow, 1);
    }
  }

  private void OnDisable()
  {
    _instances.Remove(this);

    if (_bulbMaterial != null)
    {
      _bulbMaterial.SetFloat(kShaderGlow, 0);
    }
  }

  private void Update()
  {
#if UNITY_EDITOR
    if (!UnityEditor.EditorApplication.isPlaying)
      return;
#endif

    if (_isFading)
    {
      _fadeTimer += Time.unscaledDeltaTime;
      float fadeT = Mathf.Clamp01(_fadeTimer / _fadeDuration);
      Strength = Mathf.Lerp(_fadeStartStrength, _fadeTargetStrength, fadeT);
      if (fadeT >= 1)
        _isFading = false;
    }

    if (_lightMaterial != null)
    {
      _lightMaterial.SetFloat(kShaderBeamAngle, SpotlightAngle * 0.5f);
      _lightMaterial.SetFloat(kShaderBeamLength, _currentBeamLength);
      _lightMaterial.SetColor(kShaderBeamColor, Color.WithA(BeamOpacity));
    }

    if (_lightBeamRenderer != null)
    {
      Bounds beamBounds = new Bounds(_lightBeamRenderer.transform.position, Vector3.zero);
      Vector3 beamStart = _lightBeamRenderer.transform.position;
      Vector3 beamEnd = beamStart + _lightBeamRenderer.transform.forward * _currentBeamLength;
      float beamEndRadius = Mathf.Tan(Mathf.Deg2Rad * SpotlightAngle) * _currentBeamLength;
      beamBounds.Encapsulate(beamEnd);
      beamBounds.Encapsulate(beamEnd + _lightBeamRenderer.transform.up * beamEndRadius);
      beamBounds.Encapsulate(beamEnd - _lightBeamRenderer.transform.up * beamEndRadius);
      _lightBeamRenderer.bounds = beamBounds;
    }

    float beamLength = MaxBeamLength;
    if (EnableDistanceCast)
    {
      RaycastHit hitInfo;
      if (Physics.SphereCast(transform.position, 2, transform.forward, out hitInfo, MaxBeamLength, BeamRaycastMask))
        beamLength = hitInfo.distance;
    }

    _currentBeamLength = Mathfx.Damp(_currentBeamLength, beamLength, 0.25f, Time.deltaTime * 10);
  }

#if UNITY_EDITOR
  private void OnDrawGizmos()
  {
    Vector3 lightPos = transform.position;
    Vector3 lightDir = transform.forward;

    Gizmos.color = Color;
    switch (LightType)
    {
      case LightTypeEnum.Point:
        Gizmos.DrawWireSphere(lightPos, Falloff);
        break;
      case LightTypeEnum.Spot:
        float lightRadius = Mathf.Tan(Mathf.Deg2Rad * SpotlightAngle) * Falloff;
        // Gizmos.DrawWireSphere(lightPos + lightDir * Falloff, lightRadius);
        GizmosEx.DrawCircle(lightPos + lightDir * Falloff, lightDir, lightRadius);
        Gizmos.DrawLine(lightPos, lightPos + lightDir * Falloff + transform.up * lightRadius);
        Gizmos.DrawLine(lightPos, lightPos + lightDir * Falloff - transform.up * lightRadius);

        Gizmos.DrawLine(lightPos, lightPos + lightDir * Falloff + transform.right * lightRadius);
        Gizmos.DrawLine(lightPos, lightPos + lightDir * Falloff - transform.right * lightRadius);
        break;
      case LightTypeEnum.Directional:
        Gizmos.DrawRay(lightPos, lightDir * 10);
        break;
    }
  }
#endif
}