using UnityEngine;

public class UIRenderCompositor : MonoBehaviour
{
  [SerializeField] private Shader _compositeShader = null;

  private RenderTexture _uiRenderTex;
  private Material _compositeMaterial;
  private int _lastUIWidth;
  private int _lastUIHeight;

  private static readonly int kShaderUITex = Shader.PropertyToID("_UITex");

  private void Awake()
  {
    _compositeMaterial = new Material(_compositeShader);
  }

  private void Start()
  {
    UpdateRenderTexture();
  }

  private void OnDestroy()
  {
    Destroy(_uiRenderTex);
    _uiRenderTex = null;

    Destroy(_compositeMaterial);
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination)
  {
    Camera uiCamera = PlayerUI.Instance.Camera;
    if (_lastUIWidth != uiCamera.pixelWidth || _lastUIHeight != uiCamera.pixelHeight)
      UpdateRenderTexture();

    PlayerUI.Instance.Camera.Render();

    _compositeMaterial.mainTexture = source;
    _compositeMaterial.SetTexture(kShaderUITex, _uiRenderTex);
    Graphics.Blit(source, destination, _compositeMaterial);
  }

  private void UpdateRenderTexture()
  {
    if (_uiRenderTex != null)
    {
      Destroy(_uiRenderTex);
    }

    Camera uiCamera = PlayerUI.Instance.Camera;
    uiCamera.enabled = false;

    _uiRenderTex = new(uiCamera.pixelWidth, uiCamera.pixelHeight, 24, RenderTextureFormat.Default);
    _uiRenderTex.Create();
    _lastUIWidth = uiCamera.pixelWidth;
    _lastUIHeight = uiCamera.pixelHeight;
    uiCamera.targetTexture = _uiRenderTex;
  }
}