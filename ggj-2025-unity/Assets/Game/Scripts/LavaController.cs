using UnityEngine;

public class LavaController : MonoBehaviour
{
  [SerializeField]
  private float _riseRate = 0.01f;

  public enum LavaState
  {
    Idle,
    Rising
  }
  private LavaState _lavaState = LavaState.Idle;

  public void StartRising()
  {
    _lavaState = LavaState.Rising;
  }

  public void StopRising()
  {
    _lavaState = LavaState.Idle;
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
