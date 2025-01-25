using UnityEngine;

public class LavaController : MonoBehaviour
{
  [SerializeField]
  private float _riseRate = 0.01f;


  private Vector3 _initialPosition = Vector3.zero;

  public enum LavaState
  {
    Idle,
    Rising
  }
  private LavaState _lavaState = LavaState.Idle;

  public void Awake()
  {
    _initialPosition= gameObject.transform.position;
  }

  public void StartRising()
  {
    _lavaState = LavaState.Rising;
  }

  public void StopRising()
  {
    _lavaState = LavaState.Idle;
  }

  public void Reset()
  {
    StopRising();
    gameObject.transform.position = _initialPosition;
  }

  void Update()
  {
    UpdateRising();
    CheckLavaCollision();
  }

  private void UpdateRising()
  {
    if (_lavaState == LavaState.Rising)
    {
      var newLavaTransform = gameObject.transform.position;
      newLavaTransform.y += _riseRate * Time.deltaTime;

      gameObject.transform.position = newLavaTransform;
    }
  }

  private void CheckLavaCollision()
  {
    if (GameController.Instance != null)
    {
      var playerListCopy= GameController.Instance.SpawnedPlayers.ToArray();

      foreach (PlayerActorController playerController in playerListCopy)
      {
        if (playerController != null &&
            playerController.transform.position.y < gameObject.transform.position.y)
        {
          playerController.Kill();
        }
      }
    }
  }
}
