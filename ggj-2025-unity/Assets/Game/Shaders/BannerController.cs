using UnityEngine;

public class BannerController : MonoBehaviour
{
  [SerializeField] private Transform _bannerHeightRoot = null;
  [SerializeField] private ParticleSystem _fxBannerBreak = null;
  [SerializeField] private GameObject _bannerVisualRoot = null;

  public void SetHeight(float height)
  {
    _bannerHeightRoot.position = _bannerHeightRoot.position.WithY(height);
  }

  public void Break()
  {
    _bannerVisualRoot.SetActive(false);
    _fxBannerBreak.Play();
  }

  private void Start()
  {
    SetHeight(GameController.Instance.StartMatchHeight);
    GameController.Instance.MatchStarted += OnMatchStarted;
  }

  private void OnDestroy()
  {
    if (GameController.Instance)
      GameController.Instance.MatchStarted -= OnMatchStarted;
  }

  private void OnMatchStarted()
  {
    Break();
  }
}